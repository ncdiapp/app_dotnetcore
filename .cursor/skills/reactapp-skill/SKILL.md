---
name: reactapp-skill
description: Invoke a React app Claude skill by name. Use when the user says "skill reactapp-xxx" or asks to run a reactapp skill (e.g. reactapp-create-component, reactapp-fix-ui). Loads the Claude skill file and its references, then executes the skill.
---

# reactapp-skill — Invoke a Claude React App Skill

Use this skill when the user asks to run a **React app skill** by name (e.g. "skill reactapp-create-component", "use reactapp-fix-ui", "run reactapp-use-theme"). This skill loads the corresponding Claude skill and its references, then you execute that skill’s rules and complete the task.

---

## Available Claude skills (by name)

| Name | Use when |
|------|----------|
| **reactapp-create-component** | Creating new React components or pages |
| **reactapp-fix-ui** | Fixing existing component UI to match standards |
| **reactapp-fix-api** | Adding/fixing API calls, WebAPI services, calling API from components |
| **reactapp-use-theme** | Styling with theme system, no hardcoded colors |
| **reactapp-use-wijmo** | Using FlexGrid, ComboBox, CollectionView |
| **reactapp-use-context-menu** | Grid/list context menu (Actions column + floating menu) |

---

## How to execute (workflow)

When the user provides a skill name (e.g. `reactapp-create-component`, `reactapp-fix-ui`):

1. **Read the index**  
   Open **`.claude/skills/reactapp/SKILL.md`** to see the list of skills, how they are used, and how they relate.

2. **Read the requested Claude skill**  
   Open **`.claude/skills/reactapp/{skill-name}.md`**.  
   - Normalize the name: drop `.md` if present; use the exact file name (e.g. `reactapp-create-component` → file `reactapp-create-component.md`).  
   - If the user wrote a typo (e.g. `reactapp-crate-component`), treat it as **reactapp-create-component**.

3. **Load referenced docs**  
   From that skill file, follow any **Full reference** or **Related skills** links (paths under `.claude/react-app/reference/` or `.claude/skills/reactapp/`). Load the reference files needed to perform the task (e.g. ReactAppApiStandard.md, 03-ui/standards/*, ReactAppConventions.md).

4. **Execute the skill**  
   Apply the rules and steps from the Claude skill and the loaded references. Do what the user asked (create a component, fix UI, fix API, apply theme, use Wijmo, add context menu, etc.). If the user gave extra context (e.g. file path, feature name), use it.

5. **If the skill name is missing or unclear**  
   Suggest one of the skills above or ask which one to run (create / fix UI / fix API / theme / Wijmo / context menu).

---

## Paths (from repo root)

- **Index and usage**: `.claude/skills/reactapp/SKILL.md`
- **Claude skill file**: `.claude/skills/reactapp/{skill-name}.md`  
  Examples: `reactapp-create-component.md`, `reactapp-fix-ui.md`, `reactapp-fix-api.md`, `reactapp-use-theme.md`, `reactapp-use-wijmo.md`, `reactapp-use-context-menu.md`
- **Reference docs**: `.claude/react-app/reference/` (e.g. `ReactAppReferenceIndex.md`, `ReactAppApiStandard.md`, `03-ui/UIMainPrompt.md`, `03-ui/standards/`)

---

## Example

**User**: "skill reactapp-create-component — add a new page for X"

You:  
1. Read `.claude/skills/reactapp/SKILL.md` and `.claude/skills/reactapp/reactapp-create-component.md`.  
2. Load any referenced docs (e.g. PageLayoutStandards, ThemeUsageStandards, FormStandards from `.claude/react-app/reference/03-ui/standards/`).  
3. Apply the create-component rules (theme, Tailwind, forms, conventions) and implement the new page for X.
