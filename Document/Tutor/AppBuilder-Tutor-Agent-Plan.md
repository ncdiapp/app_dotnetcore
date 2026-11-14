# AppBuilder Tutor Agent: Plan

## Context
Newcomers to the AppAI/AppBuilder platform have no in-app help. Exploration found no tour, onboarding or end-user guide, and `Document/` holds only developer and design docs. We want an AI tutor that teaches a non-coder to build an app with the no-code tools (Form Builder, SearchEditor, Workflow, Formula Engine) by explaining, guiding and demonstrating in the context of the screen they are on.

My view: this is a good fit. About 80% of the infrastructure already exists, so the tutor should be a configured agent plus a knowledge base, not new bespoke code. That also matches the "AppAI is no-code" rule. Custom code is limited to one thin UI shell and a context hook.

## What we reuse (found in the codebase)
| Need | Existing piece |
|---|---|
| Agent runtime, tool loop, HITL | `APP.BL/AIAgent/GenericAgent/SharedAgentEngine.cs`, `AgentAskUserPlugin`, `AgentHitlBridge` |
| Agent definition | Row in `dbo.AppAgentSkillSet` (+ `AppAgentToolRegister`); seed like `AppAI.Web/Migrations/V077__AppConfiguratorAgent_xhu.sql` |
| Designer UI | `components/aiskill/AgentSkillSetManagement.tsx` |
| Chat UI | `components/aiskill/SharedAgentChat.tsx` (takes `skillKey`) |
| Screen-context pattern | `formMgt/FormAgent/formAgentHost.ts` + `FormSidePanel.tsx`; `tabnavSlice.ts` for active tab and route |
| Learner memory | `GenericAgent/Memory/*` (L1/L2/L3, `AgentMemory:Mode`, currently Off) |
| Knowledge retrieval | `GenericAgent/VectorStore/*` (V075 `Kind`/`SourceKey`) or skills (`AppAISkill`) |
| Read-only platform inspection | `PlatformExplorerPlugin`, `TenantCatalogPlugin` (read-only status to be confirmed) |
| Doing it for them | `AppConfiguratorAgent`, `FormConfigPlugin`, `AppConfigPackPlugin` (invoked as child agent via `AgentCallPlugin`) |

## Design

### Product concept: three modes in one agent
1. **Explain** (default): answers "what is X / how do I do Y", written for non-coders, with no jargon.
2. **Guide** (coach): walks the learner through a task step by step ("Build a leave-request app"). One step at a time. It checks that the learner finished each step by reading platform state, then moves on.
3. **Show me** (optional, phase 3): delegates to the Configurator agent to build it, with the learner's approval, then explains what it did.

The tutor never writes code for the learner. When a formula is needed, it teaches the Formula Engine and offers templates.

### Architecture
```
Floating "Tutor" button (all screens)
   -> SharedAgentChat(skillKey="appbuilder-tutor") in a side panel
      + host context: {route, tab label, screen-specific getContext()}
   -> SharedAgentController.RunAgent
   -> Tutor agent (AppAgentSkillSet row)
        tools: Knowledge search, platform read-only explorer,
               ask-user (quizzes/choices), learner progress,
               [phase 3] call child Configurator agent (HITL confirm)
```

### Knowledge base (the real work)
Without end-user docs the tutor will hallucinate. Content is therefore the critical path.
- Source 1: new end-user guides in Markdown, `Document/Tutor/` (Form Builder, SearchEditor, Workflow, Formula, Data model, Reports, Permissions, Glossary). Written plain-language and task-based.
- Source 2: distilled from existing docs (`AppConfigurator-*`, `NoCodingAgent-PRD`, `ReportEngine_UserGuide`, `PLM_StyleTemplate_Config_Guide`) and the UI sources (`FieldFormulaDialog.tsx`, `SearchEditor.tsx`, `WorkflowAutomation.tsx`).
- Source 3: curriculum files, i.e. lessons as structured Markdown (goal, prerequisites, steps, success check, common mistakes).
- Delivery: start with small docs loaded as skills or in the prompt (phase 1). Move to vector retrieval over the same files (phase 2) when the corpus outgrows the prompt. This needs a new `Kind='TutorDoc'` in the V075 vector tables, with the retrieval query still to be implemented, since SQL `VECTOR_DISTANCE` is not filled and the store uses in-memory cosine.

### Curriculum: one sample app, "Leave Tracker", across all lessons
Every lesson adds one layer to the same app, so the learner sees how the pieces fit together and finishes with a working app, not seven disconnected exercises. Leave requests are familiar to everyone and need all four tools (data, form, formula, search, workflow), with no domain knowledge.

| # | Lesson | What the learner adds to Leave Tracker |
|---|---|---|
| 1 | Platform tour | Open the finished reference "Leave Tracker (Demo)" read-only to see the goal, then create an empty "My Leave Tracker" app |
| 2 | Data model | `LeaveRequest` entity: Employee, LeaveType, StartDate, EndDate, Days, Reason, Status |
| 3 | Form Builder | "New Leave Request" entry form: layout, required fields, LeaveType dropdown |
| 4 | Formulas | `Days` = EndDate - StartDate + 1; validation EndDate >= StartDate; default Status = Draft |
| 5 | SearchEditor | "My Requests" and "Pending Approval" lists with filters |
| 6 | Workflow | Submit -> manager approval -> Approved/Rejected, with Status updated |
| 7 | Wrap-up | Permissions (who sees what), optional report/dashboard, "what to build next" |

**Checkpoints (key to making this work):** each lesson has a start snapshot and an end snapshot of the app, stored as config packs (reuse `AppConfigPackPlugin`). These are produced in Phase 0 by the AppBuilder + Configurator pipeline, not built by hand. This gives us:
- "I'm stuck / I broke it": the learner can reset to the start of the current lesson, or skip ahead to the end of any lesson.
- Join anywhere: a learner can start at lesson 5 with lessons 1-4 pre-built.
- Show-me mode (phase 3) and the tests use the same snapshots as the expected result, so "did the learner build it right?" is a diff against the end snapshot.
- One demo app for the tutor to describe, one set of screenshots, one place to update when the UI changes.

The tutor refers back to the same app in every answer ("remember the Days field we added in lesson 4"). Learner progress is just "which lesson/step plus which snapshot".

### Tutor behaviour (system prompt rules)
- Assume zero technical background; avoid terms like table, schema or API unless defined first.
- One step per message, then confirm.
- Answer from the knowledge base only. If it is not covered, say so and log a "knowledge gap".
- Use the screen context ("I see you're in Form Builder").
- Never modify data unless in Show-me mode with explicit confirmation.
- Short, friendly, with offered next actions as quick-reply choices.

## Corrections found while drafting (2026-11-13)
- **Who builds what.** Per `Document/AgentDesign/MyApplicationEditor-AiAgent-Design.md`, creating and changing data models (datasource, schema) belongs to the managed `app-builder` agent. `app-configurator` is deliberately narrow (runtime form and search layout, formulas) and cannot create things from scratch. So Phase 0 stage 2 (data model and datasource fixes, snapshot B) should be done with `app-builder`; the Configurator handles form layout, formulas and search (snapshots C and D).
- **Beginner create path.** In `DataModelDesign.tsx`, "Create Data Model From Form" and "From Excel" show "not yet implemented", and "From Database" needs existing tables. So lesson 2 uses the assistant (App Builder agent), not a click-through.
- **Assistant dock is not built.** It is designed but absent from `MyApplicationEditor.tsx`. Until then, learners reach the agent through the App Builder Agent page (or Agent Management Test Run). Building the dock helps the tutor too and could be a Phase 1 dependency.
- **Vocabulary.** "Transaction" = data model; "Entity List Of Value" = dropdown choice lists. See `Glossary.md`.

## Phases

### Phase 0: Build the sample app with our own agents (1 week)
I read "PR" as PRD (product requirements doc). Tell me if you meant a pull request.

Instead of building Leave Tracker by hand, we build it with the platform's existing agents. This gives us the demo app, the lesson snapshots and the lesson content in one pass, and tests both agents on a realistic job.

1. **PRD first**: write `Document/Tutor/LeaveTracker-PRD.md`: users and roles, the `LeaveRequest` entity and fields, screens (entry form, My Requests, Pending Approval), rules (Days formula, validation), and the approval flow. It is also the source for the lesson goals.
2. **AppBuilder agent creates the skeleton** (`APP.BL/AIAgent/AppBuilderAgent/`: `SchemaBuilderPlugin`, `TransactionBuilderPlugin`, `SearchBuilderPlugin`, `ApplicationManagerPlugin`). Input is the PRD. Output is the app with its data model/datasource, a basic form and basic lists. Capture **snapshot A**.
3. **Configurator agent refines it** (`AppConfiguratorAgent`, V077; `FormConfigPlugin`, `FormConfigFormulaLayoutService`, `AppConfigPackPlugin`), run in separate passes, each followed by a snapshot:
   - Datasource/data model fixes (field types, dropdown LeaveType source, Status default) -> snapshot B
   - Form UI/layout (grouping, required fields, labels) -> snapshot C
   - Formulas and validation (Days, date rule) -> snapshot D
   - Search/list config and workflow (remaining steps, by agent or by hand where the agents cannot yet do it) -> snapshot E
4. **Map snapshots to lessons**: A-E become the start/end checkpoints of lessons 2-6. The agent transcripts and the diffs between snapshots become the draft step-by-step lesson content ("here is what changes in this step"); a human then rewrites them for newbies and checks them in the UI.
5. **Gap log**: anything the agents cannot do or get wrong is recorded. It feeds a fix list for those agents and marks where the tutor must teach manual steps.

Open question for the spike: whether these agents run reliably on a clean tenant, since the agent memory module has not been run on a real tenant (see risks).

- Confirm the open questions below, and write the glossary.

### Phase 1: MVP, Explain mode (1.5 weeks)
- Migration `Vxxx__SeedTutorAgent.sql` (model on V077): skillset row, prompt, tool bindings. Take the next free version number.
- Knowledge docs loaded as skills or prompt context.
- Entry point: a Tutor button on the main layout opening `SharedAgentChat` with `skillKey` (reuse `UniversalAgentPage.tsx`/side-panel pattern).
- Pass the active tab's route and label as context.

### Phase 2: Guide mode and context awareness (2 weeks)
- Lesson runner: curriculum files with step state; progress stored via agent memory facts (turn on `AgentMemory` for this agent only) or a small `AppTutorProgress` table.
- Per-screen `getContext` for Form Builder, SearchEditor and Workflow (selected field, current formula), copying the `formAgentHost.ts` pattern.
- Read-only verification: the tutor checks that the thing was actually built.
- Vector retrieval over the docs if needed.

### Phase 3: Show-me and polish (2 weeks)
- Child-agent call to the Configurator agent (and AppBuilder agent for new apps), the same pair used in Phase 0, with HITL confirmation, in a sandbox/demo app so a newbie cannot break production.
- Lesson snapshots: start/end config packs for each of the 7 lessons, with "reset to lesson N" and "skip ahead" actions (need to verify `AppConfigPackPlugin` can import into a sandbox app).
- Optional UI highlight overlay ("click here"), which is the only substantial custom frontend work.
- Feedback buttons, knowledge-gap report, usage analytics.

## Risks
- **Content gap**: no end-user docs exist. This is the largest effort; plan for it.
- **Hallucination of UI steps** when the UI changes. Mitigation: docs live in-repo, with a CI/PR checklist to update them with UI changes.
- **Cost/latency**: the LLM is currently set to Gemini. Pick a cheaper model for the tutor, and set the agent token budget.
- **Safety**: read-only by default; Show-me runs only with confirmation and in a sandbox tenant/app.
- **Unverified assumptions**: whether `PlatformExplorerPlugin` is read-only, and whether the agent memory module is production-ready (it is unit-tested but not run on a real tenant).

## Open questions for you
1. Scope of phase 1: Explain-only, or Explain plus Guide?
2. Should the tutor be able to build things (Show-me), or teach only?
3. Languages: English only, or Chinese as well (docs and prompts)?
4. Is "Leave Tracker" a good sample app, or would you prefer something closer to your customers (for example a simple fashion PLM style/sample tracker)?
5. Who writes the lesson content: should I draft it from the existing docs and code, with you reviewing?
6. LLM for the tutor: the current default provider, or a specific model?

## Verification
- Seed migration applies cleanly; agent appears in Agent Designer with the right skill key.
- Scripted test conversations per lesson (golden Q&A set of about 30 questions) checked for accuracy against the docs.
- Playwright (already available): open the app, open the Tutor panel on each of the three screens, check the context is passed and the reply references the current screen.
- Snapshot test: apply each lesson's start snapshot, follow the lesson steps, and diff the result against that lesson's end snapshot.
- Usability test with 2-3 non-technical users building Leave Tracker end to end unassisted; track completion and number of stuck points.
- Safety test: confirm that the tutor cannot change data without a confirmation prompt.
