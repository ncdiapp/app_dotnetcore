using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// ask_user — structured HITL question gate for Interactive GenericAgent runs.
    ///
    /// Reads <see cref="GenericAgentCallbacks.OnAskUser"/> via <see cref="AgentHitlBridge"/>
    /// (set by GenericAgentEngine). Waits until the UI POSTs ConfirmAskUser.
    /// Deterministic mode returns an error JSON (no blocking).
    /// </summary>
    public class AgentAskUserPlugin
    {
        public async Task<string> AskUser(
            string prompt,
            AgentToolContext context,
            string mode = "text",
            string fieldsJson = null,
            string optionsJson = null,
            string contextKey = null,
            string ui = "radio",
            string layout = "vertical")
        {
            if (context != null && context.IsDeterministic)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ask_user is not available in Deterministic mode. Collect required inputs before calling the agent, or use Interactive ExecutionMode."
                });
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "prompt is required." });
            }

            var promptError = ValidateAskUserPrompt(prompt);
            if (promptError != null)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = promptError });
            }

            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "text" : mode.Trim().ToLowerInvariant();
            if (normalizedMode != "text" && normalizedMode != "single_choice" && normalizedMode != "multi_choice")
                normalizedMode = "text";

            var options = ParseOptions(optionsJson);
            if (options.Count == 0)
                options = TryParseOptionsFromPrompt(prompt);

            // ask_user Prompt is often cleaned to "Please select…" — also read Skill SystemPrompt
            // for phrases like: let user choose from ddl / radio buttons / button group.
            var skillUiHint = InferChoiceUiFromSkillPrompt(TryLoadSkillSystemPrompt(context));
            var promptUiHint = InferChoiceUiFromPrompt(prompt);
            var inferredUi = promptUiHint ?? skillUiHint;

            // Model often uses mode=text + free text when SystemPrompt says "choose from ddl …".
            // Coerce to single_choice + dropdown when prompt lists choices.
            if (options.Count >= 2
                && normalizedMode == "text"
                && (inferredUi != null || PromptLooksLikeChoiceList(prompt)))
            {
                normalizedMode = "single_choice";
                if (string.IsNullOrWhiteSpace(ui))
                    ui = inferredUi ?? "dropdown";
            }

            var normalizedUi = ResolveChoiceUi(prompt, ui, normalizedMode, skillUiHint);

            var normalizedLayout = string.IsNullOrWhiteSpace(layout) ? "vertical" : layout.Trim().ToLowerInvariant();
            if (normalizedLayout != "vertical" && normalizedLayout != "horizontal")
                normalizedLayout = "vertical";

            // Choice UIs only apply to single_choice / multi_choice.
            if (normalizedMode == "text")
                normalizedUi = "radio";
            else if (normalizedMode == "multi_choice" && normalizedUi == "button_group")
                normalizedUi = "radio";
            else if (normalizedMode == "multi_choice" && normalizedUi == "dropdown")
                normalizedUi = "radio";

            if ((normalizedMode == "single_choice" || normalizedMode == "multi_choice") && options.Count == 0)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "mode=" + normalizedMode
                        + " requires non-empty optionsJson as [{id,display}]. "
                        + "Do not put numbered choices in prompt text — retry ask_user with optionsJson "
                        + "(ui=button_group | radio | dropdown)."
                });
            }

            var askEvent = new AgentAskUserEvent
            {
                Prompt = SanitizeAskUserPrompt(prompt),
                Mode = normalizedMode,
                Ui = normalizedUi,
                Layout = normalizedLayout,
                Fields = ParseFields(fieldsJson),
                Options = options,
                ContextKey = string.IsNullOrWhiteSpace(contextKey) ? null : contextKey.Trim()
            };

            var onAsk = AgentHitlBridge.Current?.OnAskUser;
            if (onAsk == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ask_user callback is not wired. Use GenericAgent Interactive chat (SSE) so the UI can answer."
                });
            }

            AgentAskUserResponse response;
            try
            {
                response = await onAsk(askEvent).ConfigureAwait(false)
                    ?? new AgentAskUserResponse { Cancelled = true };
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    cancelled = true,
                    error = "ask_user callback error: " + ex.Message
                });
            }

            if (!response.Cancelled
                && !string.IsNullOrWhiteSpace(askEvent.ContextKey)
                && context != null
                && HasAnswers(response))
            {
                try
                {
                    MergeAnswersToSharedContext(context, askEvent.ContextKey, response);
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                        "ask_user contextKey merge failed for key={0}", askEvent.ContextKey);
                }
            }

            return JsonConvert.SerializeObject(new
            {
                ok = !response.Cancelled,
                cancelled = response.Cancelled,
                answers = response.Answers ?? new Dictionary<string, string>(),
                selectedIds = response.SelectedIds ?? new List<string>(),
                freeText = response.FreeText ?? ""
            });
        }

        /// <summary>
        /// ROOT sometimes titles a successful Phase A as "Cancelled" and pastes child JSON.
        /// Reject that so the model retries the real checklist (A/B1/B2/C).
        /// </summary>
        private static string ValidateAskUserPrompt(string prompt)
        {
            var trimmed = SanitizeAskUserPrompt(prompt);
            if (string.IsNullOrWhiteSpace(trimmed))
                return null;

            var nl = trimmed.IndexOfAny(new[] { '\r', '\n' });
            var firstLine = nl < 0 ? trimmed : trimmed.Substring(0, nl);

            if (firstLine.IndexOf("Phase A Cancelled", StringComparison.OrdinalIgnoreCase) >= 0
                || firstLine.IndexOf("Phase A Canceled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Invalid ask_user title. A child Phase A JSON with ok=true is success — empty files/executionPlan is correct. "
                    + "Retry ask_user with first line [massupdate] Phase A checklist (or [search] / [import-dw] Phase A checklist). "
                    + "MassUpdate fieldsJson: recommendedOption select A|B1|B2|C plus proceed approve|revise|cancel. "
                    + "Do not paste child JSON into Prompt. If the user actually cancelled, use [Menu] Repeatable imports.";
            }

            if (trimmed.IndexOf("How would you like to proceed", StringComparison.OrdinalIgnoreCase) >= 0
                || trimmed.IndexOf("setup was cancelled", StringComparison.OrdinalIgnoreCase) >= 0
                || trimmed.IndexOf("setup was canceled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Invalid ask_user Prompt. Do not write 'setup was cancelled' or 'How would you like to proceed'. "
                    + "After Phase A ok=true use [massupdate] Phase A checklist with recommendedOption A|B1|B2|C. "
                    + "If the user cancelled, use [Menu] Repeatable imports with optionsJson buttons.";
            }

            return null;
        }

        /// <summary>
        /// Map ui aliases + infer from ask_user prompt text (ddl / dropdown / radio / button).
        /// </summary>
        private static bool PromptLooksLikeChoiceList(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return false;
            // At least two quoted tokens → treat as an option list.
            return System.Text.RegularExpressions.Regex.Matches(prompt, "\"([^\"]+)\"").Count >= 2;
        }

        /// <summary>
        /// Recover options when the model lists them in prompt text instead of optionsJson
        /// (e.g. choose from ddl "A1", "B2", "C3").
        /// </summary>
        private static List<LookupItemDto> TryParseOptionsFromPrompt(string prompt)
        {
            var list = new List<LookupItemDto>();
            if (string.IsNullOrWhiteSpace(prompt)) return list;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(prompt, "\"([^\"]+)\""))
            {
                var raw = (m.Groups[1].Value ?? "").Trim().TrimEnd(',').Trim();
                if (string.IsNullOrWhiteSpace(raw)) continue;
                // Skip long narrative quotes
                if (raw.Length > 80) continue;
                if (!seen.Add(raw)) continue;

                // Prefer first token / letter as id when display is like "A1" or "A — Apple"
                var id = raw;
                var dash = raw.IndexOfAny(new[] { '—', '-' });
                if (dash > 0 && dash <= 8)
                    id = raw.Substring(0, dash).Trim();
                else if (raw.Length >= 2 && char.IsLetter(raw[0]) && char.IsDigit(raw[1]))
                    id = raw.Substring(0, 1); // A1 → A (matches user's "If user selects A")

                list.Add(new LookupItemDto { Id = id, Display = raw });
            }

            // Fallback: after ddl/dropdown keyword, split remaining tokens by comma
            if (list.Count < 2)
            {
                list.Clear();
                seen.Clear();
                var m = System.Text.RegularExpressions.Regex.Match(
                    prompt,
                    @"(?:ddl|drop[\s-]*down|dropdown|combobox|combo\s*box|option\s*lists?|radio\s*buttons?|button\s*groups?)\s*(.+)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    foreach (var part in m.Groups[1].Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var raw = part.Trim().Trim('"', '\'', '.', ' ');
                        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 80) continue;
                        if (!seen.Add(raw)) continue;
                        var id = raw.Length >= 1 && char.IsLetter(raw[0]) ? raw.Substring(0, 1) : raw;
                        list.Add(new LookupItemDto { Id = id, Display = raw });
                    }
                }
            }

            return list;
        }

        private static string ResolveChoiceUi(string prompt, string requestedUi, string normalizedMode, string skillUiHint = null)
        {
            var fromAlias = NormalizeUiAlias(requestedUi);
            var inferred = InferChoiceUiFromPrompt(prompt) ?? skillUiHint;

            // Skill/SystemPrompt "choose from ddl" (and ask_user prompt keywords) win over
            // omitted ui or model default button_group/radio.
            string chosen;
            if (inferred == "dropdown"
                && (string.IsNullOrWhiteSpace(fromAlias) || fromAlias == "radio" || fromAlias == "button_group"))
                chosen = "dropdown";
            else if (inferred == "radio"
                && (string.IsNullOrWhiteSpace(fromAlias) || fromAlias == "button_group"))
                chosen = "radio";
            else if (!string.IsNullOrWhiteSpace(fromAlias))
                chosen = fromAlias;
            else if (!string.IsNullOrWhiteSpace(inferred))
                chosen = inferred;
            else
                chosen = normalizedMode == "single_choice" ? "button_group" : "radio";

            if (chosen != "radio" && chosen != "button_group" && chosen != "dropdown")
                chosen = normalizedMode == "single_choice" ? "button_group" : "radio";
            return chosen;
        }

        /// <summary>
        /// Detect explicit "choose from ddl/radio/buttons" instructions in the agent SystemPrompt.
        /// Avoids scanning the whole PLM prompt for the word "dropdown" (too noisy).
        /// </summary>
        private static string InferChoiceUiFromSkillPrompt(string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt)) return null;
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    systemPrompt,
                    @"choose\s+from\s+(ddl|drop[\s-]*downs?|dropdowns?|combobox|combo\s*box|option\s*lists?|select\s*lists?)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return "dropdown";
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    systemPrompt,
                    @"choose\s+from\s+radio(\s*buttons?)?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return "radio";
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    systemPrompt,
                    @"choose\s+from\s+(buttons?|button\s*groups?)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return "button_group";
            return null;
        }

        private static string TryLoadSkillSystemPrompt(AgentToolContext context)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.SkillKey))
                    return null;
                var dto = context.DataSourceId > 0
                    ? App.BL.AIAgent.AiSkill.AppAgentSkillSetBL.GetByKey(context.SkillKey, context.DataSourceId)
                    : App.BL.AIAgent.AiSkill.AppAgentSkillSetBL.GetByKey(context.SkillKey);
                return dto?.SystemPrompt;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeUiAlias(string ui)
        {
            if (string.IsNullOrWhiteSpace(ui)) return null;
            var u = ui.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
            while (u.Contains("  ")) u = u.Replace("  ", " ");

            if (u == "radio" || u == "radios" || u == "radio button" || u == "radio buttons")
                return "radio";
            if (u == "button group" || u == "buttongroup" || u == "buttons" || u == "button")
                return "button_group";
            if (u == "dropdown" || u == "drop down" || u == "ddl" || u == "combobox" || u == "combo box"
                || u == "select" || u == "option list" || u == "options list" || u == "list box" || u == "listbox")
                return "dropdown";
            if (u == "button_group")
                return "button_group";
            return null;
        }

        private static string InferChoiceUiFromPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return null;
            var p = prompt.ToLowerInvariant();

            // Prefer more specific phrases first.
            if (ContainsAny(p,
                    "dropdown", "drop down", "drop-down", "ddl", "combobox", "combo box",
                    "option list", "options list", "select list", "listbox", "list box"))
                return "dropdown";

            if (ContainsAny(p, "radio button", "radio buttons", "radiobutton", " as radio", "from radio"))
                return "radio";

            if (ContainsAny(p, "button group", "buttongroup", "from buttons", "as buttons"))
                return "button_group";

            return null;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var n in needles)
            {
                if (!string.IsNullOrEmpty(n) && haystack.IndexOf(n, StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Drop leading model garbage before the first <c>[StepTitle]</c> (e.g. "巧妙 eyes0123…###[Linear] …").
        /// Does not strip content that has newlines before a later <c>[x]</c> checklist marker.
        /// </summary>
        private static string SanitizeAskUserPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return prompt?.Trim() ?? "";
            var trimmed = prompt.Trim();
            // ###[Title] → [Title]
            while (trimmed.StartsWith("#", StringComparison.Ordinal))
                trimmed = trimmed.TrimStart('#').TrimStart();

            var nl = trimmed.IndexOfAny(new[] { '\r', '\n' });
            var line = nl < 0 ? trimmed : trimmed.Substring(0, nl);
            var rest = nl < 0 ? "" : trimmed.Substring(nl);

            var idx = line.IndexOf('[');
            if (idx > 0)
            {
                var after = line.Substring(idx);
                var close = after.IndexOf(']');
                if (close > 1 && close < 120)
                    return (after + rest).Trim();
            }
            return trimmed;
        }

        private static bool HasAnswers(AgentAskUserResponse response)
        {
            if (response?.Answers != null && response.Answers.Count > 0) return true;
            if (response?.SelectedIds != null && response.SelectedIds.Count > 0) return true;
            if (!string.IsNullOrWhiteSpace(response?.FreeText)) return true;
            return false;
        }

        private static void MergeAnswersToSharedContext(
            AgentToolContext context, string contextKey, AgentAskUserResponse response)
        {
            JObject merged;
            var existing = AppAgentSharedContextBL.ReadContext(
                context.WorkflowId, contextKey, context.DataSourceId);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                try { merged = JObject.Parse(existing); }
                catch (Exception swallowed) { SwallowLog.Write(swallowed); merged = new JObject(); }
            }
            else
            {
                merged = new JObject();
            }

            if (response.Answers != null)
            {
                foreach (var kv in response.Answers)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    merged[kv.Key] = kv.Value ?? "";
                }
            }

            if (response.SelectedIds != null && response.SelectedIds.Count > 0)
                merged["selectedIds"] = new JArray(response.SelectedIds);

            if (!string.IsNullOrWhiteSpace(response.FreeText))
                merged["freeText"] = response.FreeText;

            AppAgentSharedContextBL.WriteContext(
                context.WorkflowId, contextKey, merged.ToString(Formatting.None), context.DataSourceId);
        }

        private static List<AgentAskUserField> ParseFields(string fieldsJson)
        {
            var list = new List<AgentAskUserField>();
            if (string.IsNullOrWhiteSpace(fieldsJson)) return list;
            try
            {
                var arr = JArray.Parse(fieldsJson);
                foreach (var token in arr)
                {
                    if (token is not JObject o) continue;
                    var name = o.Value<string>("name") ?? o.Value<string>("Name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var type = (o.Value<string>("type") ?? o.Value<string>("Type") ?? "text").Trim().ToLowerInvariant();
                    if (type == "ddl" || type == "dropdown" || type == "combobox")
                        type = "select";
                    else if (type == "radiobutton" || type == "radio_button" || type == "radio-buttons" || type == "radios")
                        type = "radio";
                    if (type != "select" && type != "text" && type != "radio")
                        type = "text";
                    var field = new AgentAskUserField
                    {
                        Name = name.Trim(),
                        Label = (o.Value<string>("label") ?? o.Value<string>("Label") ?? name).Trim(),
                        Required = o.Value<bool?>("required") ?? o.Value<bool?>("Required") ?? false,
                        Type = type,
                        Options = new List<LookupItemDto>()
                    };
                    var optsToken = o["options"] ?? o["Options"];
                    if (optsToken is JArray optsArr)
                    {
                        foreach (var optTok in optsArr)
                        {
                            var item = ParseLookupItem(optTok);
                            if (item != null)
                                field.Options.Add(item);
                        }
                    }
                    // Options without an explicit choice type → dropdown. Keep type=radio as radio.
                    if (field.Options.Count > 0
                        && !string.Equals(field.Type, "select", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(field.Type, "radio", StringComparison.OrdinalIgnoreCase))
                        field.Type = "select";
                    EnsureDefaultSelectOptions(field);
                    list.Add(field);
                }
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed, "ignore bad JSON — LLM gets empty fields"); }
            return list;
        }

        private static List<LookupItemDto> ParseOptions(string optionsJson)
        {
            var list = new List<LookupItemDto>();
            if (string.IsNullOrWhiteSpace(optionsJson)) return list;
            try
            {
                var arr = JArray.Parse(optionsJson);
                foreach (var token in arr)
                {
                    var item = ParseLookupItem(token);
                    if (item != null)
                        list.Add(item);
                }
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed, "ignore bad JSON"); }
            return list;
        }

        /// <summary>
        /// Phase A *Ok / proceed / importMode often arrive as type=text with no options.
        /// Empty options render as a text box — fill the standard dropdowns.
        /// </summary>
        private static void EnsureDefaultSelectOptions(AgentAskUserField field)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.Name))
                return;
            field.Options ??= new List<LookupItemDto>();
            if (field.Options.Count > 0)
                return;
            string name = field.Name.Trim();
            if (name.EndsWith("Ok", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("_ok", StringComparison.OrdinalIgnoreCase))
            {
                field.Type = "select";
                field.Options.Add(new LookupItemDto { Id = "ok", Display = "OK" });
                field.Options.Add(new LookupItemDto { Id = "revise", Display = "Revise" });
                return;
            }
            if (name.Equals("proceed", StringComparison.OrdinalIgnoreCase)
                || name.Equals("action", StringComparison.OrdinalIgnoreCase))
            {
                field.Type = "select";
                field.Options.Add(new LookupItemDto { Id = "approve", Display = "Approve — proceed to Phase B" });
                field.Options.Add(new LookupItemDto { Id = "revise", Display = "Revise" });
                field.Options.Add(new LookupItemDto { Id = "cancel", Display = "Cancel" });
                return;
            }
            if (name.Equals("importMode", StringComparison.OrdinalIgnoreCase)
                || name.Equals("import_mode", StringComparison.OrdinalIgnoreCase))
            {
                field.Type = "select";
                field.Options.Add(new LookupItemDto { Id = "APPEND", Display = "APPEND" });
                field.Options.Add(new LookupItemDto { Id = "REPLACE", Display = "REPLACE" });
                return;
            }
            if (name.Equals("recommendedOption", StringComparison.OrdinalIgnoreCase)
                || name.Equals("attachOption", StringComparison.OrdinalIgnoreCase)
                || name.Equals("muOption", StringComparison.OrdinalIgnoreCase)
                || name.Equals("massUpdateOption", StringComparison.OrdinalIgnoreCase))
            {
                field.Type = "select";
                field.Options.Add(new LookupItemDto { Id = "A", Display = "A — Single table update" });
                field.Options.Add(new LookupItemDto { Id = "B1", Display = "B1 — Hierarchical, use existing ListEdit" });
                field.Options.Add(new LookupItemDto { Id = "B2", Display = "B2 — Hierarchical, create new ListEdit" });
                field.Options.Add(new LookupItemDto { Id = "C", Display = "C — Do not attach this Mass Update View" });
            }
        }

        /// <summary>
        /// Accepts LookupItemDto shape {id,display} and legacy ask_user {id,label}.
        /// </summary>
        private static LookupItemDto ParseLookupItem(JToken token)
        {
            if (token is not JObject o) return null;
            var idTok = o["id"] ?? o["Id"];
            if (idTok == null || idTok.Type == JTokenType.Null) return null;
            var id = idTok.Type == JTokenType.String ? (object)idTok.Value<string>()?.Trim() : idTok.ToObject<object>();
            if (id == null || (id is string s && string.IsNullOrWhiteSpace(s))) return null;
            var display = o.Value<string>("display") ?? o.Value<string>("Display")
                ?? o.Value<string>("label") ?? o.Value<string>("Label")
                ?? id.ToString();
            return new LookupItemDto { Id = id, Display = display?.Trim() ?? id.ToString() };
        }
    }
}
