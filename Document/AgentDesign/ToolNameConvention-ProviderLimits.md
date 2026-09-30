# Tool Name Convention & Provider Limits

Rules for naming tools, plugins and MCP servers so that Generic Agent tool calls resolve on every LLM provider.
Applies to `GenericAgentEngine` (Semantic Kernel plugins) and to MCP servers registered under Tool Libraries.

## 1. Provider limits

| Provider | Max length | Allowed characters | Must start with | Status |
|---|---|---|---|---|
| Google Gemini | **64** | `a-z A-Z 0-9 _ . -` | letter or underscore | Verified in Google docs |
| OpenAI | **64** | `a-z A-Z 0-9 _ -` | — | Verified in OpenAI docs / forum |
| Anthropic Claude | **64** | `a-z A-Z 0-9 _ -` | — | Length verified; character set from the API's documented pattern `^[a-zA-Z0-9_-]{1,64}$`, not re-checked |

Design to the strictest of all three: **max 64 characters, letters/digits/underscore only, start with a letter**.
(One Claude Code issue mentions a 128-character error; do not rely on that.)

Sources:
- Gemini: https://docs.cloud.google.com/gemini-enterprise-agent-platform/reference/models/function-calling
- Gemini FunctionDeclaration: https://docs.cloud.google.com/gemini-enterprise-agent-platform/reference/rest/Shared.Types/FunctionDeclaration
- OpenAI: https://developers.openai.com/api/docs/guides/function-calling
- Claude: https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools

## 2. The name the model actually sees

The model does not see the tool name alone. Semantic Kernel combines plugin and function:

| Connector | Name sent to the model | Separator |
|---|---|---|
| Gemini (`AddGoogleAIGeminiChatCompletion`) | `plugin_function` | `_` |
| OpenAI (`AddOpenAIChatCompletion`) | `plugin-function` | `-` |
| Anthropic (`AnthropicChatCompletionService`) | `function` only (plugin dropped) | — |

The combined name is what the 64-character limit applies to.

## 3. Rules

1. **Plugin names contain no underscore.** Gemini splits `plugin_function` at the *first* `_`. A plugin named `mcp_plm` turns `mcp_plm_Plm_Style_Published` into plugin `mcp` + function `plm_Plm_Style_Published`, which does not exist, so the call fails with `Error: Requested function could not be found.`
2. **Combined name ≤ 64 characters:** `len(plugin) + 1 + len(function) ≤ 64`.
3. **Names start with a letter.** `SanitizeName` prefixes `_` if the first character is a digit (Gemini accepts a leading underscore).
4. **Only letters, digits and `_`.** `SanitizeName` replaces everything else with `_`.
5. **Function names may contain underscores** (only the plugin name is split on the first one).
6. **Names are unique** across all plugins in one agent run, or the second registration is dropped or fails.

## 4. How the engine applies this

`APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs`

| Item | Plugin name | Function name |
|---|---|---|
| Registered tools (`AppAgentToolRegister`, library tools) | `tools` | `SanitizeName(ToolName)` |
| Ask-user | `hitl` | `ask_user` |
| MCP server | `McpPluginName(ServerName)` = `mcp` + letters/digits of the server name, cut to 16 | `SanitizeName(mcpToolName)` |

`McpPluginName` examples:

| Server name | Plugin | Gemini tool name (example tool `Plm_Style_Published`) |
|---|---|---|
| `plm` | `mcpplm` | `mcpplm_Plm_Style_Published` |
| `http://localhost:41498/webapi/McpServer` | `mcphttplocalhost` | `mcphttplocalhost_Plm_Style_Published` |

The 16-character cap exists so the longest known PLM tool still fits:
`mcp` (3) + 16 + `_` (1) + `DataExchange_Analyze_production_order_detail` (44) = 64.
It is a safeguard, not a guarantee: a server with longer tool names can still exceed 64.

## 5. Guidance when registering things

- **MCP server name:** use a short lowercase word (`plm`, `erp`), not the URL. Server name is what the model sees, and long names eat the 64-character budget.
- **Two servers whose names share the same first 16 letters/digits get the same plugin name**, and the second is skipped (logged as `MCP server … skipped`). Keep server names distinct within the first 16 characters.
- **Tool names on your own MCP server:** keep them under ~40 characters so they fit behind any plugin prefix. Prefer `Plm_Style_Published` over `DataExchange_Analyze_production_order_detail`.
- **Library tool names** (`AppAgentLibraryTool.ToolName`) follow the same rules; prefix is always `tools`.

## 6. Symptoms and diagnosis

| Symptom | Likely cause |
|---|---|
| `Requested function could not be found` on MCP tools only, while normal tools work; MCP Inspector works | Underscore in plugin name (Gemini split), rule 1 |
| Provider returns 400 `INVALID_ARGUMENT` / `string too long` on `function_declarations[n].name` | Combined name over 64 characters, rule 2 |
| Provider returns 400 `Invalid function name` | Bad first character or illegal characters, rules 3–4 |
| Tools missing entirely, backend log has `MCP server … skipped` | Connection or auth failure, or plugin-name collision, rule 6 |

## 7. History

- Fixed in commit `ad707e8` (`fix: MCP plugin names must not contain underscores`). Before that the plugin was `mcp_<ServerName>`, which broke every MCP tool under Gemini.
- Not yet verified: the Semantic Kernel separator behaviour in section 2 is taken from the SK connector design, not re-read from source in this repo. If a provider or SK version changes it, re-check section 3 rule 1.
