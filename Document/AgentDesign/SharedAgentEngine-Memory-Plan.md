# Plan: Three-layer chat memory for the Generic Agent

**Date:** 2026-10-21 · **Design doc:** `SharedAgentEngine-Memory-Architecture.md` (current design, scope and open decisions)

**Status (2026-10-21):**
- Built: all four phases (L1 repairs, L2 facts, L3 episodes, extraction job, viewer UI, tools). 66 unit tests pass, web project builds, React type-checks. **Not yet run against a real tenant DB or LLM.**
- Deliberately deferred: making the server copy the only chat history source (needs its own change and Playwright coverage).
- Next: run migrations V054/V055, enable memory on one test agent, then `AgentMemory:Mode` DryRun → Preview → On (see `SharedAgentEngine-Memory-Architecture.md` §9).
- Changes since this plan was approved: the summary is stored as turn count plus fingerprint (not `SummarizedUpToIndex`); `ExtractedUpToIndex` is replaced by `ExtractedAt` / `ExtractedTurnCount` on the extraction job; scope of facts and episodes across chats is an open decision (see the design doc, §4.3 and §7).

## Context
Review of `Document/AgentDesign/SharedAgentEngine-Architecture.md` (§4, §9, §13) against your 3-layer memory model (L1 short-term, L2 entity/fact, L3 episodic/fuzzy). Verified against code: `GenericAgentEngine.BuildChatHistoryAsync` (line 566), `AppGenericAgentSessionBL`, `MemoryContextPlugin`, `AppBuilderAgentMemoryBL`.

## Review: where each layer stands today

| Layer | Status | Evidence |
|---|---|---|
| L1 short-term | **Partial** | Window = last `RecentWindowSize` (10) messages; older turns summarised by `SummarizeOldTurnsAsync` into one system message. |
| L2 facts | **Mostly missing for Generic agents** | `InjectMemory` flag (16) + `MemoryContextPlugin` just call `AppBuilderAgentMemoryBL`, which reads 3 markdown files from `{BaseDirectory}/memory/appbuilder/` — App Builder only, no UserId, no tenant. |
| L3 episodic | **Not built** | `AppGenericAgentSession.MessagesJson` stores transcripts but nothing retrieves across chats. `AppAgentToolCatalog.Embedding` is reserved for tool RAG only. |

### Defects in L1 (fix first)
1. `SummarizeThreshold` / `MaxHistoryTokens` are stored and shown in §3.1 but `BuildChatHistoryAsync` ignores them: it windows by **message count**, not tokens. Ten long messages can still overflow; two short ones get summarised needlessly.
2. The summary is **recomputed every turn** (extra LLM call) and never persisted, so it drifts and costs tokens each run.
3. Tool-call/tool-result turns (array content) and `tool` roles are dropped, so on turn N+1 the model forgets which tools it called and what they returned (IDs, row counts, file names).
4. The client sends the `Messages` array while the server also persists `MessagesJson` — two sources of truth; the doc (§9) doesn't say which wins.
5. Doc §4 describes `InjectMemory` as "relevant memory appended to system prompt" — implementation is a tool plugin for one agent. Doc is inaccurate.

## Recommended design

### Layer 1 — Working memory (per chat session)
- Source of truth = server `AppGenericAgentSession`; client sends only the new user message.
- Add columns `SummaryText NVARCHAR(MAX)`, `SummarizedUpToIndex INT` (new migration, follow `Migrations/README.md` naming).
- Build history = `[system prompt] + [L2 facts] + [L3 recalls] + [SummaryText] + [last N turns verbatim]`.
- Trigger summarisation by **token estimate > SummarizeThreshold** (chars/4 estimate is enough), summarise only turns after `SummarizedUpToIndex`, persist result (rolling summary).
- Keep a compact one-line record of each tool call + truncated result in history instead of dropping them.

### Layer 2 — Entity/fact memory (per user, per tenant, cross-session)
- New tenant-DB table `AppAgentMemoryFact`: `FactId, UserId, SkillKey NULL (null = all agents), Category (preference|entity|rule|note), FactKey, FactValue, Source (user|agent|extracted), UpdatedAt, IsActive`. Unique on `(UserId, SkillKey, FactKey)` so facts are upserted, not duplicated (precision/stability).
- Access via `AppTenantAdapterBL.GetTenantAdapter()`, parameterised SQL (handbook Rule 5).
- Write paths: (a) agent tools `remember_fact` / `forget_fact` registered as a library tool; (b) after-session extractor pass (cheap LLM call at chat end/clear) proposing facts. Start with (a) only.
- Read path: engine injects top ≤ ~1.5k chars of facts into the system prompt when `InjectMemory` flag is set. Re-define the flag to mean this generically; keep `MemoryContextPlugin` for App Builder compatibility.
- Admin/user UI to view and delete facts (reuse existing aiskill list pattern) — users must be able to see what is remembered.

### Layer 3 — Episodic memory (fuzzy recall)
- Phase 2, after L1/L2. New table `AppAgentEpisode`: `EpisodeId, UserId, SkillKey, SessionKey, Summary, Embedding VARBINARY(MAX), CreatedAt`. Written when a chat is cleared/idle: LLM produces a 3–5 sentence episode summary + embed.
- Retrieval: embed the new user message, cosine top-3 (in-process over the user's rows is fine at this scale), inject as `[Possibly relevant past conversations]`.
- Reuse the `IToolCatalogRetriever` / `Embedding` seam pattern from §12 for the embedding provider; needs an embedding endpoint per provider (Gemini/OpenAI have one; Anthropic does not — decide a tenant setting `AIConfigEmbeddingProvider`).

### Cross-cutting
- Isolation: every memory read/write keyed by tenant DB + UserId; never via LLM arguments (§3.4 rule).
- Prompt-injection: memory text is injected as data, labelled and length-capped; `remember_fact` rejects values that look like instructions? — at minimum log and cap size.
- Observability: log which facts/episodes were injected (feeds backlog #12).

## Decisions from review discussion

**Raw chat history stays.** `AppGenericAgentSession.MessagesJson` is the source for L1, the UI, the extractor and audit. Facts/episodes are derived. Make the server copy the single source of truth (client sends only the new message; confirm in code which copy wins today before changing). Add `SummaryText`, `SummarizedUpToIndex`, `ExtractedUpToIndex`. Extraction must run before "Clear conversation" wipes `MessagesJson`. Add a transcript retention setting.

**Storage.** Two new tables in the tenant DB (not MasterDB, not files, not AppTenantSetting): `AppAgentMemoryFact`, `AppAgentEpisode` (embedding as VARBINARY, in-process cosine). No separate vector DB. Tool registrations/catalog are configuration, not memory.

**Fact vs episode.** Fact = stable key->value about user/entity/rule, replaced on change. Episode = narrative of one chat, append-only, retrieved by similarity. Explicit `remember_fact` wins; otherwise default to episode; promote to fact only on rule-wording, repetition across 2+ sessions, or stable entity. Extracted facts are `pending` until the user confirms. Never store secrets.

**Extraction pipeline.** Triggered at chat clear/idle (not per turn), background, cheap model, temp 0. One call returns `{episode, facts[]}` JSON. Facts need a verbatim user-quote `evidence`; code validates JSON, evidence match, secret scan, key/length caps. Upsert facts on `(UserId, SkillKey, FactKey)`; log every extraction. Build explicit `remember_fact` + episode summary first; add auto fact extraction after reviewing real episodes.

## Files to change
- `Document/AgentDesign/SharedAgentEngine-Architecture.md` — fix §4 `InjectMemory`, add new "Memory layers" subsection to §9, new tables in §3.7, new backlog rows.
- `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs` — `BuildChatHistoryAsync` (token-based, persisted summary, tool-turn compaction), memory injection step.
- `APP.BL/AIAgent/GenericAgent/AppGenericAgentSessionBL.cs` — load/save summary fields.
- New: `AppAgentMemoryFactBL.cs`, `MemoryFactPlugin.cs` (tools), migration SQL in `AppAI.Web/Migrations/`.
- Phase 2: `AppAgentEpisodeBL.cs`, embedding helper.
- Small file rule: put new logic in new files, not in the oversized engine (backlog #11).

## Phasing
1. **Doc fix + L1 repairs** (token threshold, persisted rolling summary, tool-turn compaction).
2. **L2 facts** (table, tools, injection, viewer UI).
3. **L3 episodes** (embedding, retrieval).

## Verification
- xUnit: history builder (token trigger, summary persisted once, tool turns kept); fact upsert/uniqueness; tenant isolation (user A cannot read user B's facts).
- Playwright: chat → "remember I prefer Gemini" → new chat shows fact applied; Clear conversation keeps facts, drops L1; fact viewer delete works.
- Manual: 30-turn chat with large tool results; confirm only one summarisation call per threshold crossing (check logs).
