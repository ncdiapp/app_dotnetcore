# React App AI Help

This folder holds **reference documentation** for the React app (`PlmApplication/AppReact/`). It is used by Claude Code and developers when writing or modifying React UI and calling backend APIs. There is no Angular/CSHTML UI in this solution—only the React SPA and its WebAPI backend.

## Entry point

- **Index**: [reference/ReactAppReferenceIndex.md](./reference/ReactAppReferenceIndex.md) — overview and links to all reference docs (API standard, conventions, UI standards).

## Reference structure

- **ReactAppApiStandard.md** — How to implement WebAPI services (query params, POST, headers, errors).
- **ReactAppConventions.md** — Debug logging (`appHelper.debugLog`), enums (`useEnumValues`), JSON display, imports.
- **03-ui/** — UI standards: UIMainPrompt.md (index), layout/, standards/ (page layout, buttons, forms, theme, Tailwind, Wijmo, context menu, etc.).

## Commands and skills (Claude Code)

Claude Code loads **commands** and **skills** from the repo root:

- **`.claude/commands/`** — e.g. `fix-ui` (fix component UI to project standards).
- **`.claude/skills/reactapp/`** — `SKILL.md` (this index), `reactapp-fix-api.md`, `reactapp-use-theme.md`, `reactapp-use-wijmo.md`, `reactapp-create-component.md`, `reactapp-fix-ui.md`, `reactapp-use-context-menu.md`.

Use the index above and those skills when working on the React app; do not look for or reference any Angular/CSHTML UI (it has been removed).
