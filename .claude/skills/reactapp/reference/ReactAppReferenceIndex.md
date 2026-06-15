# React App Reference Index

This is the main index for AI and developer reference when working on the **React app** (`PlmApplication/AppReact/`). The React app is the UI; backend APIs are in `PlmApplication/Server/WebApi/`. There is no Angular/CSHTML UI in this solution—only the React SPA and its APIs.

---

## Quick reference

| Topic | Reference |
|-------|-----------|
| **API / WebAPI services** | [ReactAppApiStandard.md](./ReactAppApiStandard.md) |
| **Conventions** (debug log, enums, JSON) | [ReactAppConventions.md](./ReactAppConventions.md) |
| **UI standards (main)** | [03-ui/UIMainPrompt.md](./03-ui/UIMainPrompt.md) |
| **Page layout** | [03-ui/standards/PageLayoutStandards.md](./03-ui/standards/PageLayoutStandards.md) |
| **Buttons** | [03-ui/standards/ButtonStandards.md](./03-ui/standards/ButtonStandards.md) |
| **Forms** | [03-ui/standards/FormStandards.md](./03-ui/standards/FormStandards.md) |
| **Theme** | [03-ui/standards/ThemeUsageStandards.md](./03-ui/standards/ThemeUsageStandards.md) |
| **Tailwind** | [03-ui/standards/TailwindCSSStandards.md](./03-ui/standards/TailwindCSSStandards.md) |
| **Context menu (grid/list)** | [03-ui/standards/ContextMenuStandards.md](./03-ui/standards/ContextMenuStandards.md) |
| **Wijmo (grid, combo, etc.)** | 03-ui/standards: WijmoCollectionViewStandards, WijmoGrid*, WijmoComboBoxCascadingStandards, WijmoFlexGrid* |
| **Reference components** | [03-ui/standards/ReferenceComponents.md](./03-ui/standards/ReferenceComponents.md) |
| **Flexbox (no flex-1)** | [03-ui/layout/TailwindFlexBoxRemainSpace.md](./03-ui/layout/TailwindFlexBoxRemainSpace.md) |
| **JSON editing** | [03-ui/standards/JsonEditingStandards.md](./03-ui/standards/JsonEditingStandards.md) |

---

## Directory structure

```
.claude/react-app/reference/
├── ReactAppReferenceIndex.md   (this file)
├── ReactAppApiStandard.md      # WebAPI service rules
├── ReactAppConventions.md      # Debug log, enums, JSON, imports
└── 03-ui/                      # UI standards and layout
    ├── UIMainPrompt.md         # UI index
    ├── layout/
    │   ├── TailwindFlexBoxRemainSpace.md
    │   └── ApplicationConfigurationMenu.md
    └── standards/
        ├── PageLayoutStandards.md
        ├── ButtonStandards.md
        ├── FormStandards.md
        ├── ThemeUsageStandards.md
        ├── TailwindCSSStandards.md
        ├── ReferenceComponents.md
        ├── ContextMenuStandards.md
        ├── ModalPopupStandards.md
        ├── TwoLevelDropdownStandards.md
        ├── JsonEditingStandards.md
        ├── WijmoCollectionViewStandards.md
        ├── WijmoGridMultiSelectStandards.md
        ├── WijmoGridNullRefFixes.md
        ├── WijmoFlexGridContainerStandards.md
        ├── WijmoFlexGridColumnVisibility.md
        ├── WijmoComboBoxCascadingStandards.md
        ├── DesignPanel_DragAndDropStandards.md
        └── ValidationMessagesRule.md
```

---

## Principles

1. **UI**: Use theme tokens from `useTheme()`; never hardcode colors. No `flex-1`—use `w-1 flex-auto` or `h-1 flex-auto`.
2. **API**: Query params in URL with `${param || ''}`; use `getHeaders()`; check `response.ok`.
3. **Debug**: Use `appHelper.debugLog()`, not `console.log()`.
4. **Enums**: Use `useEnumValues('EmAppEnumName')`.
5. **Reference**: Follow 03-ui standards and ReferenceComponents for layout, buttons, forms, and Wijmo usage.
