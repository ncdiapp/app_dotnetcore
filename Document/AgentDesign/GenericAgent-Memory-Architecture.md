# Generic AI Agent Platform — Memory Architecture

**Project:** App-netore  
**Author:** Sean Zhang  
**Date:** 2026-10-21  
**Status (2026-10-21):** Implemented end to end and unit-tested (66 tests pass, web project builds, React type-checks). **Not yet run against a real tenant database or LLM.** Rollout starts in `DryRun`, then `Preview` (see §9).  
**Audience:** Senior developers, backend engineers  
**Related:** `GenericAgent-Architecture.md` (§3.7, §4, §9), `ToolNameConvention-ProviderLimits.md`

---

## 1. Goal and scope

Give each chat session useful memory without bloating the prompt, using three layers:

| Layer | Name | Holds | Lifetime | Retrieved by |
|---|---|---|---|---|
| L1 | Working memory | Live dialogue and current task state | One chat | Always (window + rolling summary) |
| L2 | Entity / fact memory | Precise, stable facts about the user, entities and rules | Across chats | Always injected (small, capped) |
| L3 | Episodic memory | Summaries of past conversations | Across chats | Similarity search per new message |

**In scope:** chat memory for generic agents (`GenericAgentEngine`).  
**Out of scope:** tool registrations, libraries, MCP servers and the tool catalog. These are agent **configuration** (admin-authored, same for all users), not memory. See §3.

---

## 2. Current state (verified in code, 2026-10-21)

| Area | Behaviour | Location |
|---|---|---|
| Which history the model sees | The client's `request.Messages` array. The server copy (`MessagesJson`) is written in parallel for reload and audit but is **not** what the model sees. | `GenericAgentController` → `GenericAgentEngine.RunAsync` |
| Conversation storage | `AppGenericAgentSession` (`SessionKey`, `SkillKey`, `UserId`, `MessagesJson`, `DisplayTitle`, `UpdatedAt`) | `AppGenericAgentSessionBL` |
| History building | Token-estimate trigger (chars / 4 vs `SummarizeThreshold`), rolling summary persisted per chat. **Implemented.** | `ChatHistoryBuilder`, `AgentSessionSummaryBL` |
| Tool turns in history | Array-content and `tool` role messages are still skipped, but an assistant turn that ran tools keeps a compact note (up to 6 steps, 800 chars: name, short args, ok/failed, short result) built from its stored `toolSteps`. | `ChatHistoryBuilder.ToolNote` |
| Memory switch | An agent has memory when it has the `InjectMemory` flag (16) **or** subscribes to the `platform-memory` library. The admin UI does not expose capability flags (new agents get `3`), so the library checkbox is the no-code switch. | `GenericAgentEngine`, `AgentMemoryInjector`, `AgentMemorySessionBL` |
| App Builder legacy memory | `MemoryContextPlugin` still reads shared markdown files under `{BaseDirectory}/memory/appbuilder/` (no `UserId`, no tenant). Unchanged; separate from the per-user memory below. | `AppBuilderAgentMemoryBL` |
| L2 / L3 | Implemented: `AppAgentMemoryFact`, `AppAgentEpisode`, injection, tools, extraction job, viewer UI (§4.4–§5). | `APP.BL/AIAgent/GenericAgent/Memory/` |

---

## 3. Boundaries: what is and is not memory

| Thing | Class | Author | Scope |
|---|---|---|---|
| System prompt, tool registrations, libraries, MCP servers, tool catalog | Configuration | Admin | Per agent / library, same for all users |
| Transcript (`MessagesJson`) | Source data | The chat itself | One chat |
| Rolling summary | L1 memory (derived) | Engine | One chat |
| Facts | L2 memory (derived) | User or extractor | See §4.3 |
| Episodes | L3 memory (derived) | Extractor | See §4.3 |

Two points that were questioned during design:

- **Tool registrations are never facts.** The `remember_fact` tool is configuration; the data it writes is memory.
- **Raw chat history is kept.** It is the source for L1, the chat list UI, the extractor, audit, and re-extraction when the extractor prompt improves. Facts and episodes are lossy by design.

---

## 4. Design

### 4.1 Layer 1 — working memory (implemented)

```
[system prompt] + [L2 facts] + [L3 recalls] + [rolling summary] + [recent turns verbatim] + [new user message]
```

`ChatHistoryBuilder.BuildAsync`:

1. Convert stored messages to plain text user/assistant turns.
2. If no usable stored summary and (`turns <= RecentWindowSize` or estimated tokens `<= SummarizeThreshold`) → send everything verbatim.
3. Otherwise a **rolling summary** covers the oldest turns. The verbatim tail stays between `1x` and `2x` `RecentWindowSize`; when it exceeds `2x`, the oldest turns are folded into the summary with one LLM call. That is about one call per window, not per run.
4. New turns are merged into the existing summary ("existing summary + new turns").
5. If the summary call fails, the previous summary is kept and recent turns still go verbatim.

**Persistence (migration V054):** `SummaryText`, `SummarizedTurnCount`, `SummaryFingerprint` on `AppGenericAgentSession`.

- Stored as a **turn count plus fingerprint** (SHA-256 of the first turn, first 200 chars), not an index, because the client array and `MessagesJson` can differ. If the fingerprint or count no longer fits the history, the stored summary is ignored.
- `AgentSessionSummaryBL` (`Load` / `Save` / `Reset`) ensures the columns exist at runtime (same pattern as `DisplayTitle`) and never fails a run.
- "Clear conversation" (`ClearBySessionKey`) calls `Reset`.

**Behaviour change vs before:** chats below `SummarizeThreshold` (default 60000 estimated tokens) now send full history; previously everything older than 10 messages was summarised. Lower `SummarizeThreshold` per agent to keep the old cost profile.

**Tool calls in history:** done (compact note per assistant turn, see §2).

**Deliberately not done:** making the server copy the single source of truth. The controller still passes the client's `Messages` to the engine. Changing that touches the HITL gate and `[session_start]` flows that persist partial transcripts, and cannot be verified without running them; it needs its own change with Playwright coverage.

### 4.2 Fact vs episode

| Test | Fact (L2) | Episode (L3) |
|---|---|---|
| Shape | `key = value` | Narrative ("we debugged X and found Y") |
| Retrieval | By key / category, always relevant | By similarity, sometimes relevant |
| Truth over time | Current state; new value **replaces** old | Historical; never overwritten |
| Cost of a wrong value | High (injected every time) | Low (only surfaces when it matches) |
| Cost of a miss | Agent repeats a mistake | Agent lacks background |

Rule of thumb: if it is true next month regardless of the conversation, it is a fact; if it only makes sense as "that time when…", it is an episode.

Classification rules:

1. An explicit `remember_fact` call or "remember that…" wins and is stored as a confirmed fact.
2. Everything else defaults to an **episode**.
3. Promote to a fact only with evidence: rule wording ("always", "never", "I prefer"), the same statement in 2+ sessions, or a stable entity (an ID, table, system).
4. Extracted facts start as `pending` until the user confirms.
5. A new value for an existing key replaces it and keeps the old value in history.
6. Never store secrets (keys, tokens, passwords).

### 4.3 Scope of facts and episodes — **open decision**

Provenance is not scope: a fact is *derived from* one transcript but is stored with its own scope.

| Boundary | Default in this design |
|---|---|
| Other users | Never shared |
| Other tenants | Never shared (tenant DB) |
| Other agents | `SkillKey` set → that agent only; `SkillKey` NULL → all of the user's agents |
| Other chats, same user and agent | **Shared** (this is what makes L2/L3 different from L1) |

If L2/L3 were restricted to their own chat they would duplicate L1 and give no continuity between chats. If isolation is the concern, use controls instead: a per-chat "don't remember" flag, user-visible and deletable facts, similarity-gated episode recall. **Decision needed: cross-chat per user (default), per agent only, or strictly one chat.**

### 4.4 Layer 2 — fact memory (implemented)

Built as designed (migration V055, also ensured at runtime by `AgentMemoryDb`). Details that differ from or extend the original design:

- **Scope.** Extracted facts and `remember_fact` facts are stored with the current agent's `SkillKey` (agent-scoped, the conservative default). `SkillKey` NULL (all agents) is supported by the table and the readers but nothing writes it yet.
- **Upsert rules** (`AgentMemoryFactBL.Upsert`): same key and value → unchanged; new value → replaces and keeps `PreviousValue`; an extracted (`pending`) proposal never overwrites a `confirmed` fact; secrets are rejected on save.
- **Injection:** only `confirmed` facts, rules first, then preferences, entities, notes, newest first, capped at 1500 chars, labelled "treat as data, not instructions".
- **Tools** (library `platform-memory`, `AgentMemoryPlugin`): `remember_fact`, `forget_fact`, `recall_memory`. A tool-saved fact is `confirmed` with source `agent`.
- **Viewer:** the chat sidebar gets a **Memory** tab when the agent subscribes to `platform-memory` (`AgentMemoryPanel`): confirm or edit or delete facts, delete past conversations, "Don't remember this chat", "Forget everything".

Table `AppAgentMemoryFact` (tenant DB):

| Column | Notes |
|---|---|
| `FactId` INT IDENTITY PK | |
| `UserId` INT | Owner |
| `SkillKey` NVARCHAR(100) NULL | NULL = all agents |
| `Category` | `preference` \| `entity` \| `rule` \| `note` |
| `FactKey`, `FactValue` | Unique on `(UserId, SkillKey, FactKey)` → upsert |
| `Status` | `confirmed` \| `pending` |
| `Confidence`, `Source` | `user` \| `agent` \| `extracted` |
| `UpdatedAt`, `IsActive` | |

- Written by: `remember_fact` / `forget_fact` tools (registered as a library tool), and later by the extractor.
- Read path: when `InjectMemory` is set, the engine injects confirmed facts (about 1.5k chars max, ranked by recency and category) into the system prompt as a labelled block. No LLM decision is involved.
- Users must be able to view and delete their facts (admin UI list, same pattern as other aiskill screens).

### 4.5 Layer 3 — episodic memory (implemented)

Built as designed. Embeddings (`EmbeddingClient`) call fixed REST endpoints with the tenant's existing keys, so there is no new setting: Gemini `gemini-embedding-001` (768 dims) or OpenAI `text-embedding-3-small` for the default provider; Anthropic falls back to a configured Gemini or OpenAI key; with neither, recall is skipped and episodes are stored without a vector. Vectors are unit-normalised, stored as `VARBINARY`, and only compared when `EmbeddingModel` matches. Similarity thresholds are per model (Gemini 0.70, OpenAI 0.45) and are **uncalibrated starting values** to tune from the `[Memory]` log lines. Recall looks at the 500 most recent episodes of the user and agent.

Table `AppAgentEpisode` (tenant DB): `EpisodeId`, `UserId`, `SkillKey`, `SessionKey` (provenance only), `Summary`, `Topics`, `Embedding VARBINARY(MAX)`, `CreatedAt`. Append-only.

**Recall (per new user message, done by code):**

1. Skip trivial messages ("hi", "ok").
2. Embed the message (for follow-ups, the last user message plus the previous assistant message or the L1 summary).
3. Cosine similarity over this user's and agent's episodes, in process (fine up to a few thousand rows per user; no separate vector DB).
4. Keep the top 3 above a similarity threshold (about 0.75) and inject them as `[Possibly relevant past conversations]`. If none pass, inject nothing.

Embedding provider: Gemini and OpenAI have embedding endpoints; Anthropic does not. Needs a tenant setting (for example `AIConfigEmbeddingProvider`). An optional `recall_memory(query)` tool can supplement automatic recall later.

### 4.6 Isolation and safety

- Every memory read and write is keyed by tenant DB plus `UserId`, resolved from the execution identity — never from an LLM argument (see `GenericAgent-Architecture.md` §3.4).
- Use `AppTenantAdapterBL` / data-source routing, parameterised SQL, tenant DB (not `AppMasterDB`).
- Memory text is injected as labelled, length-capped data, never as instructions.
- Log which facts and episodes were injected on each run (feeds backlog #12, observability).

---

## 5. Extraction

### 5.1 Pipeline (one LLM call per chat)

Input: new user/assistant text turns since the last extraction (tool results dropped; one-line note per tool call), plus the user's existing facts so the model updates instead of duplicating. Cap about 20k chars; use the L1 summary plus recent turns if longer. Cheap model, temperature 0.

Output (JSON only):

```json
{
  "episode": { "summary": "3-5 sentences", "topics": ["plm"], "worth_keeping": true },
  "facts": [{ "key": "date_format", "value": "yyyy-MM-dd", "category": "preference",
              "evidence": "verbatim user quote", "confidence": 0.9, "action": "add|update|none" }]
}
```

Code-side validation (the model is not trusted):

1. JSON parses; on failure log and skip.
2. Each fact's `evidence` must appear verbatim (or fuzzy-match) in a **user** message.
3. Secret scan (key-like strings, `Bearer `, `sk-`) on every fact and the episode summary.
4. Cap lengths; normalise keys to `snake_case`; check `category` against the allowed list.

Storage: upsert facts on `(UserId, SkillKey, FactKey)` as `pending` unless the user said "remember"; insert the episode with its embedding.

### 5.2 Triggers

| Trigger | Notes |
|---|---|
| "Clear conversation" | Extract **before** `MessagesJson` is wiped, or the source is lost |
| Idle timeout (about 30 min) | Background sweep over `AppGenericAgentSession.UpdatedAt` |
| Explicit `remember_fact` | Bypasses extraction |

Not per turn: too costly and produces half-formed facts.

### 5.3 Background job

`MemoryExtractionService : BackgroundService` registered in AppAI.Web, following `APP.OAuth/OAuthMaintenanceService.cs` (2-minute start delay, `PeriodicTimer`, public `RunOnceAsync`, failures logged and retried next round). No Hangfire or Quartz is used in the codebase and none is needed for a single node.

```
every AgentMemory:IntervalMinutes (default 10)
  for each company
    skip if no API key (AIConfigSettingBL.IsConfigured(identity))
    select ready chats (bounded batch)
    for each chat: claim → extract → validate → store → mark done
```

Ready chats: `MessagesJson <> '[]'` AND `UpdatedAt < now - idle` AND (`ExtractedAt IS NULL` OR `UpdatedAt > ExtractedAt`) AND at least 4 text turns. New columns on the session row: `ExtractedAt`, `ExtractedTurnCount`, `ExtractingAt` (claim), retry count, and an opt-out flag.

Operational rules: kill switch `AgentMemory:Enabled` (off by default in production), per-tenant daily token budget, tenants processed sequentially, one bad chat never blocks the rest (log, count retry, stop after 3), a SysAdmin endpoint to run one round on demand, and one log line per chat (facts proposed, accepted, drop reasons). No silent `catch`.

Implemented in `MemoryExtractionService` (AppAI.Web) and `AgentMemoryJob` / `AgentMemorySessionBL` / `AgentMemoryExtractor` (APP.BL). Differences from the sketch above: the session row carries `ExtractedAt`, `ExtractedTurnCount`, `ExtractingAt` (claim, expires after 15 min), `ExtractRetryCount` (3 failures then the chat is marked extracted until it has new activity) and `MemoryOptOut`. The configuration is a `Mode` (`Off`, `DryRun`, `Preview`, `On`) instead of an on/off switch (§9). "Clear conversation" captures the transcript, queues a background extraction (facts and episode only; it does not touch the session bookkeeping) and then wipes and resets the row. The extraction call uses the tenant's configured chat model, with no temperature override (the custom Anthropic wrapper and the SK connectors take it differently); the prompt asks for JSON only and the parser tolerates code fences and surrounding prose.

### 5.4 Running without an HTTP request (spike result, 2026-10-21)

| Question | Finding |
|---|---|
| Can an identity be built without a request? | Yes. `AgentCallPlugin` builds an `AppClientIdentity` from plain fields (`UserId`, `CurrentWorkingCompanyId`, `CurrentUserDbConnectionString`, `CurrentUserDataBaseName`, `DataSourceId`); `AppSaasUserSessionMgtBL.SetupCurrentSaasUserClientInfo` does the same from a data-source row. |
| LLM config from an identity? | Yes. `AIConfigSettingBL` has "background-thread overloads"; `AppTenantSettingBL.GetStringValue(key, identity)` needs only company ID and connection string. |
| Session rows without identity? | Yes. `AppGenericAgentSessionBL` and `AgentSessionSummaryBL` resolve the connection from `dataSourceId` via `AppCacheManagerBL.GetOneDatabaseFixture`. |
| Tenant enumeration? | `AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity()` filtered by `IsCompanyMasterDb`, connection string decrypted with `AppConnectionStringEncryptionBL.Decrypt` (see `AppTenantMigrationRunnerBL`). |
| Precedent | `PlmImportEngine.Jobs.RunJobInBackground` captures the identity and uses `Task.Run`; it also calls `WindowsIdentityProvider.RegisterIdentity` for code that reads `ServerContext` implicitly. The extraction job should avoid needing that by using only the identity overloads and `dataSourceId`-based SQL. |

Required refactor: `GenericAgentEngine.BuildKernel(identity)` is private; move it to a shared internal `KernelFactory` (also backlog #11).

**Resolved:** a company-master data-source row carries its company in `DataSourceOwnerCompanyId` (this is the same lookup `AppCacheManagerBL.GetCurrentCompanyMasterDataSource` uses); rows with a null owner are unassigned reserve databases and are skipped. The job reads the tenant list straight from AppMasterDB and needs no session. `KernelFactory` was extracted from the engine.

**Still to confirm in the first real run:** no hidden `ServerContext` read on the paths the job calls (only the identity overloads and `dataSourceId`-based SQL are used, but this has not been exercised against a live tenant).

---

## 6. Phasing

| Phase | Scope | Status |
|---|---|---|
| 1 | L1: token-based trigger, persisted rolling summary (V054), doc fixes | **Built**, unit-tested |
| 1b | L1: compact tool-call record in history | **Built**, unit-tested. Server copy as single source of truth: deliberately deferred (§4.1) |
| 2a | `KernelFactory` extraction; `MemoryExtractionService` and `AgentMemoryJob` (`DryRun` lists candidate chats, no LLM, no writes) | **Built** |
| 2b | Extraction in `Preview` mode (LLM call, proposals logged, nothing written) so real output can be reviewed first | **Built**; needs a real run |
| 3 | L2: `AppAgentMemoryFact`, tools, injection, viewer UI | **Built**; parser/formatting unit-tested, SQL paths not yet run |
| 4 | L3: embeddings, recall, automatic fact extraction (all `pending`) | **Built**; thresholds uncalibrated |

---

## 7. Open decisions

| # | Decision | Options | Recommendation |
|---|---|---|---|
| 1 | Scope of facts and episodes (§4.3) | Per user across chats / per agent only / strictly one chat | Per user across chats with a per-chat opt-out and visible facts |
| 2 | L3 retrieval | Automatic injection only / plus `recall_memory` tool | Automatic first, tool later |
| 3 | Embedding provider | New tenant setting per provider | Gemini or OpenAI; Anthropic has no embedding endpoint |
| 4 | Job trigger | Own timer (feasible per §5.4) / after the chat response | Own timer, with the "Clear conversation" hook |
| 5 | Transcript retention | Keep forever / configurable limit | Configurable; episodes outlive raw transcripts |

---

Defaults applied while building (change them here if the decision goes the other way): #1 facts are shared across a user's chats, scoped to the agent; #2 automatic recall for agents with memory, plus the optional `recall_memory` tool; #3 no new setting, the embedding provider follows the default provider with the fallbacks in §4.5; #4 own timer plus the "Clear conversation" hook; #5 no transcript retention limit yet (episodes are kept regardless).

## 8. Verification plan

- **xUnit:** history builder (threshold trigger, summary reused until the tail passes `2x` window, fingerprint mismatch discards the stored summary, summary failure keeps the old one); fact upsert and uniqueness; tenant and user isolation; extraction validation (bad JSON, quote not found, secret detected).
- **Playwright:** chat → "remember I prefer X" → new chat applies it; "Clear conversation" keeps facts and drops L1; fact viewer delete works.
- **Manual:** run V054 (`POST RunMigrations` or Development startup), chat past a low `SummarizeThreshold`, confirm one summary call, a populated `SummaryText`, and no new call until about a window of turns later. Run the dry-run job against a real tenant and confirm the company and identity wiring.

**Done so far (automated, `APP.BL.Tests/AgentMemory`, 40 tests):** history builder (threshold, summary reuse, incremental refresh, fingerprint mismatch, failed refresh, tool notes), extraction parser (evidence, secrets, confidence, fences, invalid JSON, episode rules), secret scanner, key and category normalisation, prompt-block ordering and cap, embedding byte round trip and cosine, recall query selection. **Not covered:** every SQL path (needs a tenant DB), the LLM and embedding calls, the controller and the React panel (needs a Playwright scenario).

---

## 9. Configuration, rollout and operations

`appsettings.json`, section `AgentMemory` (all default to the values shown; `Mode` defaults to `Off`):

```json
"AgentMemory": { "Mode": "Off", "IntervalMinutes": 10, "IdleMinutes": 30, "BatchPerTenant": 20, "MinNewTurns": 4 }
```

| Mode | Behaviour |
|---|---|
| `Off` | Nothing is extracted, including on "Clear conversation". Injection of already stored facts and episodes still works for agents with memory. |
| `DryRun` | Each round lists the chats that would be extracted. No LLM call, no writes. |
| `Preview` | Calls the LLM and logs what would be saved (facts proposed, drop reasons, whether an episode would be stored). Writes nothing and does not mark chats done. |
| `On` | Extracts and stores. |

Recommended rollout: enable memory for one test agent (subscribe to `platform-memory` in the library tab) → `Mode=DryRun` and read the log → `Preview` and read a few dozen proposals → `On`. Run `POST /webapi/AgentMemory/RunExtraction?mode=DryRun|Preview|On` (SysAdmin) to trigger one round immediately without waiting for the timer or changing the configured mode.

Endpoints (`AgentMemoryController`, all scoped to the signed-in user and tenant): `ListFacts`, `ConfirmFact`, `UpdateFact`, `DeleteFact`, `ListEpisodes`, `DeleteEpisode`, `ForgetEverything`, `SetChatMemoryOptOut`, plus the SysAdmin `RunExtraction`.

Log lines to watch: `[MemoryJob] ...` (one per chat: facts proposed, added, updated, skipped, drop reasons) and `[Memory] skill=… user=… factsChars=… episodes=… ids=id:score` (what was injected into each run, used to tune the thresholds).

Known limitations: single node only (the claim column prevents double work but the timer assumes one host); extraction uses the tenant's main chat model, so cost scales with chat volume (cap with `BatchPerTenant` and `MinNewTurns`); there is no per-tenant token budget; opt-out applies to extraction only (facts already stored are still used); no transcript retention limit.
