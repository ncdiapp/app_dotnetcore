using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Transaction Message Template (AppMessage MessgaeScopeType=MessageTemplate) CRUD for Form Config Agent.
    /// Used with Print From Message Template (74) via upsert_command optionsJson.messageTemplateId.
    /// </summary>
    public static class FormConfigMessageTemplateService
    {
        const int MessageTemplateScope = (int)EmAppMessgaeScopeType.MessageTemplate;

        public static string GetMessageTemplates(int transactionId, string nameContains, int? take)
        {
            try
            {
                var limit = take.HasValue && take.Value > 0 ? Math.Min(take.Value, 200) : 100;
                var q = (nameContains ?? "").Trim();
                var list = AppMessageBL.RetrieveTransactionMessageTemplates(transactionId)
                    .Where(m => string.IsNullOrEmpty(q)
                        || ContainsIgnore(m.Bcclist, q)
                        || ContainsIgnore(m.Subject, q)
                        || ContainsIgnore(m.Message, q))
                    .OrderByDescending(m => Convert.ToInt32(m.Id ?? 0))
                    .Take(limit)
                    .Select(Summarize)
                    .ToList();
                return Ok(new
                {
                    transactionId,
                    count = list.Count,
                    messageTemplates = list,
                    NextStep = "Use messageTemplateId with upsert_command actionType 74: optionsJson.messageTemplateId."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetMessageTemplateDetail(int transactionId, int messageTemplateId)
        {
            try
            {
                var dto = AppMessageBL.RetrieveOneAppMessageExDto(messageTemplateId);
                if (dto == null || dto.Id == null)
                    return Fail($"Message template {messageTemplateId} not found.");
                if (dto.MessgaeScopeType != MessageTemplateScope)
                    return Fail($"Message {messageTemplateId} is not a MessageTemplate (MessgaeScopeType={dto.MessgaeScopeType}).");
                if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId)
                    return Fail($"Message template {messageTemplateId} does not belong to transaction {transactionId}.");

                return Ok(new
                {
                    transactionId,
                    messageTemplate = Detail(dto),
                    tokenHints =
                        "Body/subject may include message tokens from find_fields → messageToken (e.g. [TF_{fieldId}_{dbName}]). " +
                        "UI picker display prefers Bcclist (name), then Subject."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string UpsertMessageTemplate(
            int transactionId,
            int? messageTemplateId,
            string name,
            string subject,
            string body,
            string toList,
            string ccList,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                var isCreate = !(messageTemplateId.HasValue && messageTemplateId.Value > 0);
                AppMessageDto dto;
                if (!isCreate)
                {
                    var existing = AppMessageBL.RetrieveOneAppMessageExDto(messageTemplateId.Value);
                    if (existing == null || existing.Id == null)
                        return Fail($"Message template {messageTemplateId} not found.");
                    if (existing.MessgaeScopeType != MessageTemplateScope)
                        return Fail($"Message {messageTemplateId} is not a MessageTemplate scope.");
                    if (existing.TransactionId.HasValue && Convert.ToInt32(existing.TransactionId) != transactionId)
                        return Fail($"Message template {messageTemplateId} does not belong to transaction {transactionId}.");
                    dto = existing;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(name))
                        return Fail("subject or name is required for create (UI picker shows Bcclist/name, then Subject).");
                    dto = new AppMessageDto
                    {
                        MessgaeScopeType = MessageTemplateScope,
                        TransactionId = transactionId,
                        IsPredefinedTemplate = true,
                        IsDraft = false,
                        Subject = "",
                        Message = "",
                        ToList = "",
                        Cclist = "",
                        Bcclist = "",
                        FromEmail = ""
                    };
                }

                dto.MessgaeScopeType = MessageTemplateScope;
                dto.TransactionId = transactionId;
                if (dto.IsPredefinedTemplate == null) dto.IsPredefinedTemplate = true;

                // name → Bcclist (picker display); subject stays email subject / fallback display
                if (name != null) dto.Bcclist = name;
                if (subject != null) dto.Subject = subject;
                else if (isCreate && string.IsNullOrWhiteSpace(dto.Subject) && !string.IsNullOrWhiteSpace(dto.Bcclist))
                    dto.Subject = dto.Bcclist;

                if (body != null) dto.Message = body;
                if (toList != null) dto.ToList = toList;
                if (ccList != null) dto.Cclist = ccList;

                if (string.IsNullOrWhiteSpace(dto.Subject))
                    return Fail("Subject is required (DTO mandatory). Pass subject or name.");
                dto.ToList ??= "";
                dto.Cclist ??= "";
                dto.Bcclist ??= "";
                dto.Message ??= "";
                dto.IsModified = true;

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = isCreate || dto.IsNew ? "create" : "update",
                            messageTemplateId = dto.Id,
                            name = dto.Bcclist,
                            subject = dto.Subject,
                            bodyPreview = Truncate(dto.Message, 240),
                            toList = dto.ToList,
                            ccList = dto.Cclist,
                            transactionId = dto.TransactionId,
                            messgaeScopeType = dto.MessgaeScopeType
                        },
                        NextStep = "After save: upsert_command(..., optionsJson:{messageTemplateId:<id>}) for actionType 74."
                    });
                }

                var result = AppMessageBL.SaveOneAppMessageDto(dto);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save message template failed.");
                }

                int? savedId = null;
                if (result?.Object != null)
                {
                    try { savedId = Convert.ToInt32(result.Object); } catch { /* ignore */ }
                }
                if (!savedId.HasValue && dto.Id != null)
                    savedId = Convert.ToInt32(dto.Id);

                return Ok(new
                {
                    saved = true,
                    messageTemplateId = savedId,
                    changed = new[] { new { type = "messageTemplate", id = savedId } },
                    NextStep = savedId.HasValue
                        ? $"Call upsert_command with optionsJson.messageTemplateId={savedId.Value} for Print From Message Template (74), then request_form_reload."
                        : "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string DeleteMessageTemplate(int transactionId, int messageTemplateId, bool? confirmed, bool? dryRun)
        {
            try
            {
                var dto = AppMessageBL.RetrieveOneAppMessageExDto(messageTemplateId);
                if (dto == null || dto.Id == null)
                    return Fail($"Message template {messageTemplateId} not found.");
                if (dto.MessgaeScopeType != MessageTemplateScope)
                    return Fail($"Message {messageTemplateId} is not a MessageTemplate scope.");
                if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId)
                    return Fail($"Message template {messageTemplateId} does not belong to transaction {transactionId}.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = "delete",
                            messageTemplateId,
                            name = dto.Bcclist,
                            subject = dto.Subject
                        }
                    });
                }

                var result = AppMessageBL.DeleteMessagesByIdList(new List<int> { messageTemplateId });
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete message template failed.");
                }

                return Ok(new
                {
                    deleted = true,
                    messageTemplateId,
                    changed = new[] { new { type = "messageTemplate", id = messageTemplateId } },
                    NextStep = "If a command 74 still points at this id, clear or rebind optionsJson.messageTemplateId, then request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static object Summarize(AppMessageDto m) => new
        {
            messageTemplateId = m.Id,
            name = string.IsNullOrWhiteSpace(m.Bcclist) ? m.Subject : m.Bcclist,
            subject = m.Subject,
            bcclist = m.Bcclist,
            transactionId = m.TransactionId,
            isPredefinedTemplate = m.IsPredefinedTemplate,
            bodyPreview = Truncate(m.Message, 120)
        };

        static object Detail(AppMessageDto m) => new
        {
            messageTemplateId = m.Id,
            name = string.IsNullOrWhiteSpace(m.Bcclist) ? m.Subject : m.Bcclist,
            subject = m.Subject,
            body = m.Message,
            toList = m.ToList,
            ccList = m.Cclist,
            bcclist = m.Bcclist,
            fromEmail = m.FromEmail,
            transactionId = m.TransactionId,
            messgaeScopeType = m.MessgaeScopeType,
            isPredefinedTemplate = m.IsPredefinedTemplate,
            isDraft = m.IsDraft
        };

        static bool ContainsIgnore(string hay, string needle) =>
            !string.IsNullOrEmpty(hay) && hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        static string Ok(object payload)
        {
            var jo = JObject.FromObject(payload);
            jo["ok"] = true;
            jo["IsSuccess"] = true;
            return jo.ToString(Formatting.Indented);
        }

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });
    }
}
