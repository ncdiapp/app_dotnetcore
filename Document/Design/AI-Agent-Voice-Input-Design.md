# AI Agent Voice Input (Mic) — Requirements & Design

**Status:** Approved for implementation (2026-11-13)  
**Date:** 2026-11-13  
**Source reference (PLM):** `C:\Dev\PLM3\PLM` — MediaRecorder + server STT (behavior reference only; **do not copy messy PLM code**)  
**Related:** [AI-Agent-Chat-UI.md](./AI-Agent-Chat-UI.md), [AI-Chat-UI-Productization.md](./AI-Chat-UI-Productization.md), [SharedAgentEngine-UserGuide.md](../AgentDesign/SharedAgentEngine-UserGuide.md)

### Locked decisions (§8)

| # | Decision |
|---|---|
| 1 | **STT:** Prefer MediaRecorder → server **Whisper/Gemini**; if server voice **not enabled**, fallback **Web Speech API**. Icons differ: `fa-microphone` (Whisper) vs `fa-microphone-lines` (browser). |
| 2 | **Language:** auto-detect (omit Whisper `language` / let model detect) |
| 3 | **Unsupported:** **hide** Mic |
| 4 | **Scope:** SharedAgentChat + refactor existing + Data Integration / Query / Workflow **same batch** |
| 5 | **UX:** simple **toggle** icon (no Speak-now popup) |
| 6 | **Gate:** browser MediaRecorder **and** tenant AI switch (`IsAiConfigured` + `AIConfigEnableVoiceInput` + OpenAI **or** Gemini STT key). Client falls back to `IsAiConfigured` when `IsAiVoiceInputEnabled` is missing (old session). |

---

## 0. Why confirmation is needed

Scope is cross-cutting (shared Generic Agent composer + several standalone agent pages). There is a **technology fork**:

| | AppAI today | PLM reference |
|---|---|---|
| Client | Web Speech API (`useVoiceInput`) | `MediaRecorder` + VAD + base64 upload |
| Server | None | Whisper / Gemini STT (`LLMServiceBL.ConvertVoice2Text`) |
| UX | Toggle listen → final transcript append | Recording popup, silence auto-stop, then STT |
| Coverage | App Builder / App Report / DB Genie only | Chat, search, memo, notification |

“搬过来” can mean **UX feature parity** (mic on all agent composers) or **full PLM STT pipeline**. That choice drives backend work and browser behavior. §8 lists the questions to lock before coding.

---

## 1. Goal / Non-goals

### Goal (v1)

Give users a **microphone option** on AI Agent chat composers so spoken words become text in the input box (append), then the user can edit and Send as today.

Primary surfaces:

1. **Generic Agent stack** — one shared composer in `SharedAgentChat` (covers Agent Chat / Universal Agent / Skill Test Run / **Form Agent** / **Form Configurator** via `AgentUiChatHost` / `FormSidePanel`).
2. **Standalone agents that already have mic** — unify UI via shared control (App Builder, App Report, DB Genie).
3. **Standalone agents missing mic** — Data Integration Agent, Query Agent panel, Workflow Automation Agent panel.

### Non-goals (v1)

- Auto-send after transcription (user always reviews text and clicks Send), unless product later opts in.
- Replacing typed input; mic is additive.
- Voice output (TTS).
- File attach on composer (still out of scope per Chat UI docs).
- **Form Memo** mic in v1 — **leave an extension hook only** (see §6).
- `SearchView.tsx` itself has no agent composer; search-domain agent is **App Report Agent** (already has mic).
- Human messaging composers (e.g. `ConversationEditorEmbedded`) — out of scope.

---

## 2. Current state (AppAIClean)

### 2.1 Hook already present

`AppReact/src/hooks/useVoiceInput.ts`

- Browser `SpeechRecognition` / `webkitSpeechRecognition`
- `lang: 'en-US'`, final results only, `maxAlternatives: 1`
- Returns `{ isListening, supported, toggle }`
- On result: caller appends transcript to input (`prev + ' ' + transcript`)

### 2.2 Mic already wired

| Component | Path |
|---|---|
| App Builder Agent | `AppReact/src/components/integration/AppBuilderAgent.tsx` |
| App Report Agent | `AppReact/src/components/search/AppReportAgent.tsx` |
| DB Genie Chat | `AppReact/src/components/dbgenie/DbaGenieChat.tsx` |

Button UI is **copy-pasted** (no shared `MicButton`).

### 2.3 Shared Generic composer — no mic

`AppReact/src/components/aiskill/SharedAgentChat.tsx` — dock composer: textarea + Stop/Send only.  
Docs currently say “no microphone” ([AI-Agent-Chat-UI.md](./AI-Agent-Chat-UI.md) §Composer / Left out).

Hosts that inherit this composer:

- `AgentChatManagement` / `AgentChatPage` / `UniversalAgentPage`
- `AgentSkillSetManagement` (Test Run)
- `FormSidePanel` (Form Assistant + Configurator)

### 2.4 Missing mic (standalone)

- `DataIntegrationAgent.tsx`
- `QueryAgentPanel.tsx`
- `WorkflowAutomationAgentPanel.tsx`

### 2.5 Form Memo (future)

`AppReact/src/components/formMgt/.../FormControls/MemoControl.tsx` — plain form textarea; no agent chrome. v1 documents hook only.

---

## 3. PLM reference (what we are porting from)

### Architecture

Hybrid: client records audio → server STT → text returned to UI.

| Layer | Behavior |
|---|---|
| Gate | AI company flag + `enumerateDevices` audioinput |
| Capture | `getUserMedia` + `MediaRecorder` + analyser VAD |
| UX | Global recording popup: Speak now → Listening… → silence 3s auto-stop / 30s no-speech timeout |
| STT | `POST` audio base64 → Whisper (`whisper-1`) or Gemini multimodal; Ollama incomplete |
| Insert | By `callingFrom`: chat append, search replace+run, memo append, notification append |

Key PLM files (absolute):

- Client orchestration: `...\navigationCtrl.js` (`executeAiVoiceSearch`, `startMicrophoneVoiceInput`, …)
- Chat: `AiAssistant*.cshtml` / `aiAssistantChatCtrl.js` — `MemoboxVoiceInput`
- Memo: `_MemoLayout.cshtml` — `SubitemVoiceInput`
- Server: `SearchController.GetTextFromVoiceInputByAiWebApiService`, `LLMServiceBL.ConvertVoice2Text`

---

## 4. Requirements

### R1 — Mic on Generic Agent composer

- Mic button visible on `SharedAgentChat` composer when voice is supported (or always visible but disabled with tooltip when not).
- Placement: **left of Send** (and left of Stop when Stop is shown), bottom-right action row.
- Click starts/stops listening (or start/stop recording — depends on §5 tech choice).
- Final transcript **appends** to the current input with a space if non-empty; does **not** clear existing text.
- Disabled while composer is blocked (history loading, turn running, pending ask_user / plan / schema), matching Send disabled rules — **except** Stop remains available when running; mic stays off while blocked/running.

### R2 — Shared Mic control

- Extract shared UI + behavior so App Builder / App Report / DB Genie / new surfaces do not diverge.
- Suggested names: `MicButton` (UI) + existing/extended `useVoiceInput` (or `useVoiceCapture` if STT path changes).

### R3 — Cover remaining agent composers

- Add the same control to Data Integration / Query / Workflow agent panels.

### R4 — Language / i18n

- v1 must decide STT language (see §8). Current hardcode `en-US` is insufficient for multi-locale tenants if Web Speech stays.

### R5 — Permissions & errors

- Mic permission denied / no device / unsupported browser: clear non-blocking feedback (toast or inline), no crash.
- Optional: hide mic when unsupported vs show disabled — pick one UX (recommend: **hide when unsupported** to match App Builder today).

### R6 — Form Memo extension point (no UI in v1)

- Document a stable callback/hook shape so Memo can later call the same voice pipeline and append into field value (PLM `SubitemVoiceInput` parity).
- Do not change `MemoControl` UI in v1 unless explicitly approved later.

### R7 — Docs update

- After ship: update [AI-Agent-Chat-UI.md](./AI-Agent-Chat-UI.md) to remove “no microphone” and describe Mic next to Send/Stop.

---

## 5. Design options (STT)

### Option A — Extend Web Speech (recommended for fast v1)

**What:** Keep browser SpeechRecognition; productize shared `MicButton`; wire Generic + missing agents; improve language from tenant/UI locale.

| Pros | Cons |
|---|---|
| Already in repo; zero new backend | Chrome/Edge best; Safari/Firefox gaps |
| Low latency, streaming-friendly later | Accuracy / language depend on OS/browser |
| Matches existing App Builder / Report / Genie | Not a literal PLM Whisper port |

**Done when:** All §1 surfaces use shared Mic; Memo hook documented; Chat UI doc updated.

### Option B — Port PLM MediaRecorder + server Whisper

**What:** Record blob → new AppAI API (e.g. `GetTextFromVoiceInput`) → LLM provider Whisper/Gemini → append text. Optional VAD + recording overlay like PLM.

| Pros | Cons |
|---|---|
| Closer to PLM; more consistent quality | New API, provider keys, payload size, cost |
| Works wherever MediaRecorder works | Heavier UX; must map AppAI LLM provider settings |
| Same path can serve Memo later | Larger change; Ollama STT still weak in PLM |

**Done when:** Capture + API + Mic UI on §1 surfaces; provider routing documented.

### Option C — Hybrid (phased)

1. **v1:** Option A — ship mic everywhere on agents quickly.  
2. **v1.1:** Add server STT as fallback when `!SpeechRecognition`, or as preferred path when tenant AI voice is enabled (PLM-style flag).

Recommended default if product wants speed **and** eventual PLM parity: **Option C**, implement A first.

---

## 6. Shared component design (frontend)

### 6.1 `MicButton` (new)

Suggested path: `AppReact/src/components/common/MicButton.tsx` (or `aiskill/`).

```ts
type MicButtonProps = {
  onTranscript: (text: string) => void;
  disabled?: boolean;
  className?: string;
  size?: 'dock' | 'compact'; // SharedAgentChat vs standalone bars
  title?: string;
};
```

Behavior:

- Uses voice hook internally (or accepts `voice` from parent — prefer internal for DRY).
- If `!supported` → render `null` (v1 default).
- Listening: red / pulse + stop icon (match App Builder).
- Idle: microphone icon (`fa-solid fa-microphone`).
- Theme tokens only; no hardcoded brand colors.

### 6.2 Composer wiring (Generic)

In `SharedAgentChat` action row:

```
[ Mic ] [ Stop? ] [ Send ]
```

```ts
useVoiceInput((t) => setInput((prev) => (prev ? `${prev} ${t}` : t)));
```

### 6.3 Standalone agents

Replace inline mic markup with `<MicButton onTranscript={…} />`.

### 6.4 Memo extension hook (v1 doc only)

Stable contract for later:

```ts
/** Future: MemoControl / form memo toolbar */
type VoiceFieldTarget = {
  appendText: (text: string) => void;
  disabled?: boolean;
};
// Same MicButton; parent supplies appendText bound to FormData field.
```

Optional later PLM-like `callingFrom`: `'AgentChat' | 'FormMemo' | 'Search'`.

---

## 7. Backend (only if Option B / C phase 2)

Sketch (align with PLM, adapt to AppAI LLM stack):

| Item | Proposal |
|---|---|
| Endpoint | e.g. `POST /appai/.../GetTextFromVoiceInput` |
| Body | `{ audioBase64: string, mimeType?: string }` |
| Response | `{ sentence: string, errorMsg?: string }` |
| BL | Route via current LLM / OpenAI-compatible audio transcription settings |
| Auth | Same as other AI endpoints; respect tenant AI enablement |
| Limits | Max audio duration / size (e.g. 60s / few MB) |

No backend change for Option A v1.

---

## 8. Open questions (must confirm before implementation)

1. **STT path:** A (Web Speech only) / B (PLM Whisper pipeline) / C (A now, B later)?  
   - **Recommendation:** **C** — ship mic on Generic Agent immediately with existing hook; plan Whisper as follow-up.

2. **Language:** keep `en-US`, use app UI language, or user-selectable?  
   - **Recommendation:** map from app/session culture when available; fallback `en-US`.

3. **Unsupported browser:** hide mic vs disabled + tooltip?  
   - **Recommendation:** hide (current App Builder pattern).

4. **v1 surface list:** confirm include Data Integration / Query / Workflow panels in the same change as SharedAgentChat?  
   - **Recommendation:** yes — same PR or same milestone.

5. **Recording overlay:** PLM popup (“Speak now / Listening…”) in v1, or simple toggle icon state only?  
   - **Recommendation:** toggle icon for Web Speech v1; full overlay if/when Whisper lands.

6. **Feature flag:** always on when browser supports, or tenant/company AI flag like PLM?  
   - **Recommendation:** no new flag for Web Speech; if Whisper later, gate on existing AI provider config.

---

## 9. Implementation plan (after confirmation)

### Phase 0 — Lock §8 answers; update this doc Status to “approved”

### Phase 1 — Shared control + Generic Agent (highest leverage)

1. Add `MicButton` (+ extend `useVoiceInput` for language if agreed).
2. Wire into `SharedAgentChat` composer (covers Generic + Form + Configurator).
3. Update this doc + `AI-Agent-Chat-UI.md`.

### Phase 2 — Unify + fill gaps

1. Refactor App Builder / App Report / DB Genie to `MicButton`.
2. Add mic to Data Integration / Query / Workflow agent panels.

### Phase 3 — Optional server STT (if B or C)

1. Backend transcription API + provider wiring.
2. Capture path + optional recording overlay.
3. Fallback / preferred-path switch in the voice hook.
4. Keep Memo as consumer of the same API (still UI-later).

### Phase 4 — Form Memo (explicit follow-up)

1. Add mic affordance on `MemoControl` / memo toolbar using the same `MicButton` + `appendText`.

---

## 10. Acceptance criteria (v1, assuming Option A or C-phase1)

- [ ] Opening Generic Agent / Form Agent / Configurator chat shows Mic when browser supports speech.
- [ ] Speaking appends text into the composer; Send still required.
- [ ] Mic disabled/hidden appropriately while turn blocked/running and when unsupported.
- [ ] App Builder / App Report / DB Genie use the same Mic control (no divergent behavior).
- [ ] Data Integration / Query / Workflow agent inputs have Mic (if included in confirmed scope).
- [ ] Memo has **no** mic UI yet; design hook documented in this file.
- [ ] `AI-Agent-Chat-UI.md` no longer lists microphone as left out.

---

## 11. File touch list (expected)

| Area | Files |
|---|---|
| Docs | This file; `Document/Design/AI-Agent-Chat-UI.md` |
| Hook | `AppReact/src/hooks/useVoiceInput.ts` |
| Shared UI | `AppReact/src/components/common/MicButton.tsx` (new) |
| Generic | `AppReact/src/components/aiskill/SharedAgentChat.tsx` |
| Refactor | `AppBuilderAgent.tsx`, `AppReportAgent.tsx`, `DbaGenieChat.tsx` |
| Add | `DataIntegrationAgent.tsx`, `QueryAgentPanel.tsx`, `WorkflowAutomationAgentPanel.tsx` |
| Backend | Only if Option B/C phase 2 |

---

## 12. Decision log

| Date | Decision | Notes |
|---|---|---|
| 2026-11-13 | Draft created | Awaiting answers to §8 |
| 2026-11-13 | §8 locked | Whisper rewrite; auto language; hide unsupported; same-batch surfaces; toggle UX; browser + tenant AI (`AIConfigEnableVoiceInput` + OpenAI key + `IsAiConfigured`) |
| 2026-11-13 | Implemented v1 | `VoiceInputController` / `WhisperSpeechToTextService`; `MicButton` + `useVoiceInput` (MediaRecorder); SharedAgentChat + standalone agents; Memo deferred (hook only) |
| 2026-11-13 | Web Speech fallback | When `IsAiVoiceInputEnabled` / server STT gate is off → browser `SpeechRecognition`; Mic idle icon `fa-microphone-lines`; tooltip / `data-voice-engine` expose path |

### Implemented touch list

| Area | Path |
|---|---|
| Tenant setting | `AIConfigEnableVoiceInput` (V081), enum 3219 |
| UserContext | `IsAiVoiceInputEnabled` |
| STT | `ISpeechToTextService` / `WhisperSpeechToTextService` |
| API | `POST /appai/webapi/VoiceInput/GetTextFromVoiceInput` |
| Hook / UI | `useVoiceInput.ts`, `MicButton.tsx`, `voiceInputSvc.ts` |
| Agents | SharedAgentChat, AppBuilder, AppReport, DbaGenie, DataIntegration, Query, Workflow |
| Memo | Extension contract in §6.4 only — no UI yet |
