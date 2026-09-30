# Generic Agent — Safe Mode Security Plan

**Project:** App-netore  
**Status:** Planned — not started  
**Date:** 2026-09-30  
**Audience:** Engineers who will implement it; chairman for go/no-go on the phases  

---

## 1. Why

Agents can run code: PowerShell scripts, C# snippets, arbitrary .NET methods, external DLLs. The LLM can also write the files it then runs. Today the only controls are "which file" and "how long", not "what it can do". We need a **safe mode** that is the default, with code execution as an explicit, approved, sandboxed exception.

**Goal:** a tenant admin or an LLM cannot read or change anything outside the agent's own data, cannot reach internal services it was not given, and cannot turn text into running code without an approval that is tied to the exact content.

**Non-goals:** a general-purpose malware sandbox; protection against a platform admin who deliberately installs a malicious DLL.

---

## 2. Findings (code review, 2026-09-30)

Reviewed by reading code only; none of this has been exploited or tested. Re-verify each item when fixing it.

| ID | Severity | Finding | Where |
|---|---|---|---|
| F1 | High | No role check on creating tools. Any logged-in user who reaches the endpoints can create tools of type PowerShell, DynamicCSharp, ExternalDll or BuiltIn. The docs say "super-admin only", but nothing enforces it. | `AgentSkillSetController.UpsertTool`, `UpsertLibraryTool` |
| F2 | High | Scripts run as the web server's OS account with `-ExecutionPolicy Bypass`. The policy checks path prefix (`source/`), extension (`.ps1`) and timeout only, not script behaviour. | `GenericAgentProcessBL.StartPowerShellFileAsync`, `ValidatePathPolicy` |
| F3 | High | The agent can write a `.ps1` into its file area (agent-files tools) and run it (`run_agent_script`). Text becomes code with no approval tied to its content. | `AgentFilePlugin`, `AgentScriptPlugin.Run` |
| F4 | High | The child PowerShell process inherits the web process's whole environment (connection strings, `MCP_*` tokens, API keys). | `StartPowerShellFileAsync` (`ProcessStartInfo.Environment` is not cleared) |
| F5 | High | `DynamicCSharp`: "allowed imports" are only default `using`s; fully-qualified calls such as `System.IO.File` or `System.Diagnostics.Process` still work. `TimeoutSeconds` from the docs is not implemented. **The architecture doc claims IO/Net/Reflection are blocked — that claim is wrong.** | `DynamicCSharpToolExecutor` |
| F6 | High | In-process `PowerShell` tool type: full trust in the web process, no timeout or cancellation (`Invoke()` is synchronous), no language restriction. | `PowerShellToolExecutor` |
| F7 | High | `BuiltIn` resolves `TypeName` across **every loaded assembly** and binds a public method by name, so a tool author can point it at framework or third-party methods. | `BuiltInToolExecutor.FindType` |
| F8 | High | `ExternalDll`: `Path.Combine(repo, AssemblyName)` with no validation. `..\` or an absolute path loads any DLL on disk. | `ExternalDllToolExecutor.ExecuteAsync` |
| F9 | Medium | `SqlQuery` runs whatever SQL is in `ToolConfig` with the tenant database login; nothing forces read-only. | `SqlQueryToolExecutor` |
| F10 | Medium | `HttpRest` has no host allow-list or redirect control (same SSRF class already closed for MCP). | `HttpRestToolExecutor` |
| F11 | Medium | No audit trail of tool calls (who, which agent, which tool, arguments, result). No per-run call limits. No global kill switch. | `AppAgentToolEngine`, `AgentStepFilter` |

---

## 3. Design

### 3.1 Execution modes (per agent)

| Mode | Allowed | Who can set it |
|---|---|---|
| **Safe** (default) | Read-only SQL, REST to allow-listed hosts, MCP (existing policy), allow-listed built-ins, `ask_user`, session files (write, **not execute**) | Everyone (default) |
| **Standard** | Safe + scripts that an admin has approved by content hash, run **only in the sandbox** (§3.4) | Tenant admin with the `AgentExecute` permission |
| **Privileged** | Everything incl. ExternalDll, in-process code, unapproved scripts; fully audited | Platform admin only; off by default in tenants |

`ExecutionMode` (Interactive/Deterministic) is unrelated and stays as is.

### 3.2 One enforcement point

`AppAgentToolEngine.Dispatch` (the single choke point for every tool type) asks `AgentExecutionPolicy.Evaluate(agent, toolType, toolConfig, context)` and **fails closed** with a clear error string for the LLM ("This tool is not allowed in Safe mode"). The same policy is checked again when a tool is saved, so problems show up in the UI, not at run time.

### 3.3 Authoring control

- Creating or editing tools of type `PowerShell`, `DynamicCSharp`, `ExternalDll`, `BuiltIn` requires platform admin (`RequireSysAdmin()` pattern already used in `TenantProvisioningController`).
- Tenant admins can create `SqlQuery`, `HttpRest` and MCP servers only.
- Changing an agent's mode above Safe requires the matching permission and is audited.

### 3.4 Script sandbox (the real boundary)

Validation in code cannot stop a determined script. The boundary must be the operating system. Target design for Windows; Linux equivalent is one container per run.

1. Dedicated low-privilege local account (for example `agentrun`), not the app-pool identity and not an administrator.
2. NTFS: modify rights only on `AgentOutput\{session}`; read-only on what a script needs; no other access.
3. Outbound firewall rule for that account: deny all except an allow-list (PLM/ERP hosts).
4. Job object: memory, CPU time, process count limits; kill-on-close; existing timeout kept.
5. Clean environment: clear the inherited variables, pass only declared ones (today `PLM_DW_SQL_USER/PASSWORD`).
6. PowerShell in **ConstrainedLanguage** with an allowed-cmdlet list (JEA session configuration); no `Add-Type`, no arbitrary .NET member calls.
7. Launch through a small runner service/process so the web app never holds the low-privilege credentials in memory longer than needed.

### 3.5 Script integrity and approval

- The approver sees the **script content**; approval stores its SHA-256 (table `AppAgentScriptApproval`: hash, approver, approved at, scope agent/library, optional expiry).
- `run_agent_script` runs a file only when its current hash is approved. If the LLM edits the file, the hash changes and approval is required again.
- Interactive agents request approval through the existing `ask_user` flow; Deterministic agents run **only** pre-approved scripts from the library.
- An admin-maintained **script library** (approved, versioned scripts) is the preferred path: the LLM chooses a script and passes parameters, it does not author code.

### 3.6 Audit and brakes

- Table `AppAgentToolAudit`: time, tenant, user, agent, session, tool, tool type, arguments hash (and arguments for write/exec types), outcome, duration, blocked reason.
- Per-run limits: max tool calls (already `MaxIterations`), max script runs per run and per hour, max output size.
- Global switch `Agents:AllowExecutableTools` (default `false`): off = PowerShell, DynamicCSharp, ExternalDll, unapproved BuiltIn and script execution return "disabled".
- Log every blocked attempt at Warn with agent and tool.

---

## 4. Work plan

Effort is a rough estimate for one developer including tests.

### Phase A — Stop the obvious holes (about 1–2 days; no new infrastructure)

| Task | Change | Files | Done when |
|---|---|---|---|
| A1 | Require platform admin to create/edit risky tool types (F1) | `AgentSkillSetController`, `AgentToolPolicy` (new helper) | A tenant admin gets a validation error saving a PowerShell tool; SQL/REST still save |
| A2 | Config flag `Agents:AllowExecutableTools` (default false); executors for PowerShell, DynamicCSharp, ExternalDll and script run return a disabled error when off (F5, F6, F8) | `AppAgentToolEngine.Dispatch`, `AgentScriptPlugin` | Flag off: those tool types return the error and log a warning |
| A3 | ExternalDll: accept a bare file name only, resolve and check the full path stays inside the repo folder (F8) | `ExternalDllToolExecutor` | `..\x.dll`, `C:\x.dll`, `sub\x.dll` are rejected |
| A4 | BuiltIn: resolve only types in the BL assembly (or types with a new `[AgentTool]` attribute); drop the all-assemblies scan and case-insensitive fallback for unknown types (F7) | `BuiltInToolExecutor`, seed check of existing `TypeName`s | Existing seeded tools still work; `System.IO.File` is rejected |
| A5 | Script child process: clear the environment, pass only declared variables (F4) | `GenericAgentProcessBL` | A script printing `$env:*` shows no app secrets |
| A6 | Documentation corrections: DynamicCSharp limits, "super-admin only", TimeoutSeconds | `GenericAgent-Architecture.md` §5 | Doc matches the code |

Phase A risk: turning executable tools off by default can break tenants that use scripts today (the PLM data-integration flow uses `run_agent_script`). Mitigation: list existing tools by type first (query below), enable the flag explicitly where needed.

```sql
-- Who uses executable tool types today?
SELECT 'agent' AS Owner, SkillKey, ToolName, ToolType FROM dbo.AppAgentToolRegister
  WHERE ToolType IN ('PowerShell','DynamicCSharp','ExternalDll','BuiltIn') AND IsActive = 1
UNION ALL
SELECT 'library', LibraryKey, ToolName, ToolType FROM dbo.AppAgentLibraryTool
  WHERE ToolType IN ('PowerShell','DynamicCSharp','ExternalDll') AND IsActive = 1;
```

### Phase B — Policy, approval, audit (about 1–2 weeks)

| Task | Change | Done when |
|---|---|---|
| B1 | Migration: `AppAgentSkillSet.SafetyMode` (`Safe`/`Standard`/`Privileged`, default `Safe`); `AppAgentToolAudit`; `AppAgentScriptApproval` | Migrations apply on a clean and an existing tenant |
| B2 | `AgentExecutionPolicy.Evaluate` called from `Dispatch` and from tool save | Every tool type has a rule; unknown type = deny |
| B3 | SqlQuery read-only: reject non-SELECT statements and use a read-only login where available (F9) | `UPDATE/DELETE/DROP/EXEC` in `Sql` is rejected on save and at run time |
| B4 | HttpRest: host allow-list per tenant, reuse `McpSecurityPolicy` URL rules, no redirects (F10) | Internal/metadata hosts blocked; allow-listed hosts pass |
| B5 | Script approval by hash: store, check in `run_agent_script`, approval UI via `ask_user`, admin script library | Edited file = blocked until re-approved |
| B6 | Audit writer in `Dispatch`; per-run and per-hour script limits; blocked attempts logged | Audit rows appear for allowed and blocked calls |
| B7 | UI: agent "Safety mode" selector with explanation, tool save errors shown, audit viewer (read-only grid) | A tenant admin sees why a tool was refused |

### Phase C — OS sandbox (about 2–4 weeks; needs infrastructure decisions)

| Task | Change | Done when |
|---|---|---|
| C1 | Decide platform and runner form (§5 questions) | Decision recorded here |
| C2 | Runner process/service with low-privilege account, NTFS ACLs, job object, firewall rule, clean environment | A script cannot read outside its folder, open a socket to a non-allow-listed host, or spawn unbounded children |
| C3 | PowerShell ConstrainedLanguage + JEA allowed-cmdlet list; Standard mode uses it | `Add-Type`, `Invoke-Expression`, .NET method calls are refused |
| C4 | Move the in-process PowerShell and DynamicCSharp tool types to the runner or retire them | No code executes inside the web process from tool config |
| C5 | Security regression test suite (§6) in CI | Suite runs on every build |

---

## 5. Open questions (decide before Phase C)

1. **Where does production run?** Windows + IIS (options in §3.4) or Linux/containers (one container per run). This decides C2–C3.
2. Does any tenant rely on DynamicCSharp, ExternalDll or unapproved BuiltIn today? (Run the SQL in Phase A.)
3. Who approves scripts: tenant admin, or platform admin only? Does approval expire?
4. Should Deterministic (unattended) agents ever run LLM-written scripts? Recommendation: no.
5. Retention for `AppAgentToolAudit` (suggest 90 days, arguments redacted for secrets).
6. Is `pwsh` 7 guaranteed on the servers (`APP_AI_PWSH` override exists)? JEA and ConstrainedLanguage behave differently between 5.1 and 7.

---

## 6. Test plan

Security regression tests (xUnit, plus one Playwright scenario for the UI error path). Each test names the finding it covers.

| Test | Covers |
|---|---|
| Tenant admin cannot save a PowerShell / DynamicCSharp / ExternalDll / BuiltIn tool | F1 |
| Flag off: executable tool types and script run return the disabled error | F2, F5, F6, F8 |
| ExternalDll rejects `..\`, rooted and sub-folder names | F8 |
| BuiltIn rejects `System.IO.File`, `System.Diagnostics.Process`, types outside the BL assembly | F7 |
| Script run prints no inherited secrets from the environment | F4 |
| Edited script is refused until re-approved (hash mismatch) | F3 |
| SqlQuery rejects UPDATE, DELETE, DROP, EXEC, `;` batches | F9 |
| HttpRest blocks 169.254.169.254, credentials in URL, redirects | F10 |
| Blocked and allowed calls produce audit rows | F11 |
| Phase C: script cannot read `C:\Windows\win.ini` outside its folder, cannot connect to a blocked host, is killed at memory/process limit | F2, F4 |

---

## 7. Rollout and compatibility

- Ship Phase A behind the flag with the current behaviour available (`AllowExecutableTools=true`) for tenants that need it, then tighten per tenant.
- Existing agents get `SafetyMode = Safe`. Agents that use scripts need `Standard`; migrate them with a one-off script after reviewing the SQL above.
- Announce the authoring change (A1): tenant admins lose the ability to create executable tool types.
- Keep `SwallowLog`-style logging for every refusal so support can see why a tool was blocked.

## 8. Related documents

- `GenericAgent-Architecture.md` — §5 tool types, §8 MCP security policy, §13 backlog
- `ToolNameConvention-ProviderLimits.md`
- `AppAI.Web/Migrations/README.md` — migration rules
