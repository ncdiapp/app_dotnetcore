# React App Skills — Overview

This folder contains **Claude Code skills** for working on the React app (`PlmApplication/AppReact/`). All skills use the same reference docs under **`.claude/react-app/reference/`**. Use this file to choose the right skill and understand how they work together.

---

## Reference (shared)

- **Index**: `.claude/react-app/reference/ReactAppReferenceIndex.md` — project overview and links to all reference docs.
- **API**: `.claude/react-app/reference/ReactAppApiStandard.md`
- **Conventions**: `.claude/react-app/reference/ReactAppConventions.md`
- **UI**: `.claude/react-app/reference/03-ui/UIMainPrompt.md` and `03-ui/standards/`, `03-ui/layout/`

---

## Skills and when to use them

| Skill | File | Use when |
|-------|------|----------|
| **reactapp-fix-api** | [reactapp-fix-api.md](./reactapp-fix-api.md) | Adding or fixing API calls: new WebAPI service method, fixing API errors, or adding code in components to call the backend. |
| **reactapp-use-theme** | [reactapp-use-theme.md](./reactapp-use-theme.md) | Styling UI with the theme system. Use whenever applying colors, buttons, inputs, or layout—never hardcode colors. |
| **reactapp-use-wijmo** | [reactapp-use-wijmo.md](./reactapp-use-wijmo.md) | Using Wijmo (FlexGrid, ComboBox, CollectionView). Use when building grids, combo boxes, or fixing Wijmo binding/ref issues. |
| **reactapp-create-component** | [reactapp-create-component.md](./reactapp-create-component.md) | **Creating** new React components or pages. Applies theme, Tailwind, forms, Wijmo, and conventions in one place. |
| **reactapp-fix-ui** | [reactapp-fix-ui.md](./reactapp-fix-ui.md) | **Fixing** existing component UI to match project standards (theme, layout, forms, Wijmo). Use with the `fix-ui` command. |
| **reactapp-use-context-menu** | [reactapp-use-context-menu.md](./reactapp-use-context-menu.md) | Implementing or aligning context menu on list/grid pages (Actions column + floating menu, no Edit/Delete column on the right). |

---

## How to use

1. **Pick the skill** that matches your task (create vs fix, API vs UI, theme vs Wijmo vs context menu).
2. **Open the skill file** (e.g. `reactapp-create-component.md`) for rules and a short summary; it points to the full reference under `.claude/react-app/reference/`.
3. **For creating components/pages**: Use **reactapp-create-component** — it pulls in theme, Tailwind, forms, Wijmo, and conventions. Optionally read **reactapp-use-theme** or **reactapp-use-wijmo** for more detail.
4. **For fixing existing UI**: Use **reactapp-fix-ui** (or the **fix-ui** command, which uses this skill). It lists the checklist and workflow.
5. **For API work**: Use **reactapp-fix-api** for service methods and calling APIs from components.
6. **For grid/list context menu**: Use **reactapp-use-context-menu** and the reference `03-ui/standards/ContextMenuStandards.md`.

---

## Command

- **fix-ui** (`.claude/commands/fix-ui.md`): Fix a React component’s UI. It delegates to the **reactapp-fix-ui** skill for the full checklist and workflow.

---

## Summary

- **Create** → reactapp-create-component  
- **Fix UI** → reactapp-fix-ui (or `fix-ui` command)  
- **API** → reactapp-fix-api  
- **Theme** → reactapp-use-theme  
- **Wijmo** → reactapp-use-wijmo  
- **Context menu** → reactapp-use-context-menu  

All reference docs live in `.claude/react-app/reference/`; skill files only summarize and link to them.
