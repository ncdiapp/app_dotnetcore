# Generic Agent and Skill Library — Design

| | |
|---|---|
| **Status** | **Replaces** the 2026-10-26 AppAISkill / Available-Skills plan (S1–S4). That code is reverted. |
| **Date** | 2026-10-27 |
| **Language** | English (canonical) |
| **Skill store** | Existing `AppAISkill` + `AppAISkillRef` only |
| **Not used** | `AppAgentSkillSetSkill`, Private / Visibility / SourceType columns, Specialist ↔ Skill binding |

The 2026-10-26 design bound skills onto Specialist agents (`Agent Management` → Skills tab, `load_app_skill` / `save_as_app_skill` inside every agent). That model is withdrawn. Skills belong only to the Generic Agent.

---

## 1. Two kinds of agent

| | Specialist Agent | Generic Agent |
|---|------------------|---------------|
| Where it is configured | **Agent Management** | Not in Agent Management. No prompt, tool, or limit configuration. |
| What it is today | Every agent that has its own Prompt and Library Tools (and the rest of Agent Management: Child-Agent, Files, Limits, MCP). | New. One product surface. |
| Chat UI | Existing agent chat | Same family of UI: multiple chats, Thinking, Tools, Files, Results |
| Tools | Only the libraries and tools configured for that agent | Every Library Tool |
| Skills | None. No skill tab, no skill tools, no skill index | Only this agent loads and saves skills |
| Menu | Existing entries (configured agents, Agent Management, AI Skill Management) | Hard-coded **first child** under the left-nav **AI Agent** menu |

Specialist agents stay the configured workers. The Generic Agent is the unconfigured front door: it can use any library tool, pick skills from context, and hand a task to a Specialist when a configured agent is the better next step.

---

## 2. Generic Agent

### 2.1 Page

- Fixed page. Opened from the left menu **AI Agent**, as the **first submenu item** (ahead of DBA Agent, App Builder, configured agents, AI Skill Management, and Agent Management).
- The menu item is hard-coded in the sidebar. It is not a row created in Agent Management.
- There is no SkillKey to edit, no System Prompt field, and no tool-subscription screen for this agent.

### 2.2 Chats

- A user can open many Generic chats.
- Each chat has the same working surfaces as a Specialist chat: Thinking, tool activity, files, and results.
- Chats are independent. Skill choice is per chat, from that chat’s context (see §4).

### 2.3 Tools

The Generic Agent may call any tool in any tool library. It does not subscribe library-by-library. Specialist tool exclusions and per-agent tool lists do not apply here.

---

## 3. Skills

### 3.1 Who they are for

- Only the Generic Agent uses skills.
- A Specialist Agent has no link to skills: it does not list them, load them, or save them.
- Any signed-in user may create many skills.
- A skill is visible to every user. v1 does **no** owner, private/tenant, or permission check.
- One Generic chat may use **multiple** skills at once. The model chooses them from the chat context. The user does not pick a skill chip list.

### 3.2 Store

Reuse `AppAISkill` (Name, Description, SkillContent, IsActive) and `AppAISkillRef` (text references). Do not add Private columns. Do not add `AppAgentSkillSetSkill`.

`AI Skill Management` stays the manual screen: create, edit, and delete skills and their text references.

### 3.3 Save as Skill

On a Generic chat, **Save as Skill** generates a **new** skill. It does not update a Specialist, and it does not attach the skill to Agent Management.

Generation follows the same idea as the withdrawn implementation, and must not turn the transcript into an unrelated essay:

1. Take the current task context (the user request that led to this work, the assistant reply, and the tools actually used).
2. Pass tool name, arguments, and the shape of the tool result (field names, not a full row dump).
3. An LLM writes Name, Description, and SkillContent (Role / Steps / Constraints / Outputs) from that evidence.
4. Steps name the tools to call again and the fields or dates to keep.
5. Do not reinterpret the task as a different kind of work. Do not paste the raw chat as the skill body.
6. The user did not type a name: the model that is chatting must not invent the stored title. The draft LLM names the skill from the evidence.
7. Persist a new `AppAISkill` row. Optional audit text may sit in an `AppAISkillRef` whose file name starts with `_`. Those refs are not part of the composed instructions.

### 3.4 Use in a Generic chat

- The runtime can see the skill catalog (Name, Description, active flag).
- From the chat context, the model selects one or more skills and loads their composed content (SkillContent plus refs, skipping `_` file names).
- Loaded skill text is instructions for this chat. It does not change Specialist configuration and does not change the tool libraries.

---

## 4. Hand-off to a Specialist Agent

The Generic Agent may propose a move to **one specific** Specialist Agent (one Agent Management entry).

1. The model names that Specialist and why.
2. Nothing opens until the **user agrees**.
3. What the Specialist chat receives at the start is an **AI-written summary** of the task (goal, decisions, relevant tool outcomes). Not the raw transcript as the first user message.
4. The full Generic transcript is stored with the hand-off so it can be read later if needed.
5. Agreement opens a **new** chat on that Specialist. It does not continue an existing Specialist chat.
6. After the jump, that chat is a normal Specialist chat: its own Prompt and its own tools. It does not load skills.

---

## 5. Out of scope for this revision

- Skill permissions, owners, or a private/tenant flag
- Binding skills onto a Specialist
- `load_app_skill` / `save_as_app_skill` inside Specialist runs
- An Agent Management Skills tab
- Auto-creating a Specialist from a skill

---

## 6. Withdrawn implementation

The uncommitted S1–S4 code (draft-from-prompt on top of the old bind model, Available Skills tab, engine skill tools on every agent) is reverted.

`V065__ReusableSkillLibrary_xhu.sql` is a **rollback** script. It drops `AppAgentSkillSetSkill` and the columns `OwnerUserId`, `Visibility`, and `SourceType` when they exist. Tenants that already applied the old add-script must delete that version from `_SchemaMigrations` before migrations will run this file.

---

## 7. Acceptance (when implementation starts)

1. AI Agent menu opens the Generic Agent first. It does not appear in Agent Management.
2. A Generic chat can call library tools that are not subscribed on any single Specialist.
3. Save as Skill on a Generic chat creates one shared skill whose steps match the tools and fields just used.
4. A later Generic chat can load more than one skill from context, without a user-picked skill list.
5. A Specialist chat never offers Save as Skill and never loads skills.
6. A hand-off asks the user, then opens a new Specialist chat seeded with a summary; the full Generic transcript remains available.
7. AI Skill Management can still create, edit, and delete skills by hand.
)
