# UI Standards and Guidelines - Main Index

This is the main entry point for all UI-related standards, guidelines, and patterns for the React application.

## Overview

This UI prompt library contains comprehensive documentation for:
- **Page Layout Standards** - Standard page structures and layouts
- **Component Standards** - Button, form, and other component patterns
- **Theme System** - Dynamic theme usage and guidelines
- **Layout Patterns** - Flexbox, split layouts, and spacing patterns
- **Reference Components** - Real-world examples and templates

## Quick Start

**For creating new pages**: Start with `standards/PageLayoutStandards.md` and `standards/ReferenceComponents.md`

**For styling components**: See `standards/ButtonStandards.md` and `standards/FormStandards.md`

**For theme usage**: See `standards/ThemeUsageStandards.md`

**For layout patterns**: See `layout/TailwindFlexBoxRemainSpace.md`

**For drag and drop**: See `standards/DesignPanel_DragAndDropStandards.md`

**For JSON editing (memo/textarea, config strings)**: See `standards/JsonEditingStandards.md` — use `JSON.stringify(value, null, 2)` for pretty-print.

---

## Directory Structure

```
03-ui/
├── UIMainPrompt.md (this file)
├── layout/                        # Layout patterns and techniques
│   ├── TailwindFlexBoxRemainSpace.md
│   └── ApplicationConfigurationMenu.md
└── standards/                     # UI standards and guidelines
    ├── PageLayoutStandards.md
    ├── ButtonStandards.md
    ├── FormStandards.md
    ├── ThemeUsageStandards.md
    ├── TailwindCSSStandards.md
    ├── DesignPanel_DragAndDropStandards.md
    ├── ReferenceComponents.md
    ├── ContextMenuStandards.md    # Grid/list context menu (Actions column + floating menu)
    ├── ModalPopupStandards.md     # 弹窗：点击 backdrop 不关闭，仅通过按钮关闭
    ├── TwoLevelDropdownStandards.md  # Category → sub-items cascading dropdown (FIELD / Api Operation)
    ├── WijmoGridMultiSelectStandards.md  # FlexGrid 多选 (ListBox/MultiRange)、selectedRows、双列表转移
    ├── WijmoGridNullRefFixes.md   # FlexGrid editRange/finishEditing 空引用：patch 在 src/wijmoPatches.ts，index 引入
    └── JsonEditingStandards.md   # JSON stringify: use null, 2 for pretty-print
```

---

## UI Standards

### [PageLayoutStandards.md](./standards/PageLayoutStandards.md)
- **Purpose**: Standard page layout patterns and structure guidelines
- **Use When**: Creating new pages or modifying page layouts
- **Key Topics**:
  - Standard page structure (header + main content)
  - Header toolbar standards (height, spacing, layout)
  - Main content area standards
  - Left-right and top-bottom split layouts
  - Spacing and padding standards
  - Reference page patterns

### [ButtonStandards.md](./standards/ButtonStandards.md)
- **Purpose**: Standard button styles, sizes, and patterns
- **Use When**: Creating or modifying buttons
- **Key Topics**:
  - Theme-based buttons (primary, secondary)
  - Button sizes (small icon, standard, large)
  - Button states (disabled, active, hover)
  - Button with icons
  - Button groups
  - Common button patterns

### [FormStandards.md](./standards/FormStandards.md)
- **Purpose**: Standard form field styles, layouts, and patterns
- **Use When**: Creating forms or form fields
- **Key Topics**:
  - Form field structure (label + input)
  - Input types (text, textarea, select, checkbox)
  - Form layouts (horizontal, vertical, inline, two-column)
  - Form spacing standards
  - Form validation patterns
  - Form examples

### [ThemeUsageStandards.md](./standards/ThemeUsageStandards.md)
- **Purpose**: How to use the dynamic theme system
- **Use When**: Using theme tokens in components
- **Key Topics**:
  - Theme hook usage (`useTheme`)
  - Theme classes (`theme.button_default`, `theme.mainContentSection`, etc.)
  - Theme parameters (`t()` function)
  - Theme switching
  - Best practices and common mistakes

### [TailwindCSSStandards.md](./standards/TailwindCSSStandards.md)
- **Purpose**: Tailwind CSS usage standards and patterns
- **Use When**: Writing Tailwind classes or styling components
- **Key Topics**:
  - Critical rules (never use `flex-1`)
  - Common Tailwind patterns (spacing, sizes, borders, text)
  - Flexbox patterns
  - Overflow patterns
  - Component patterns
  - Best practices and common mistakes

### [ReferenceComponents.md](./standards/ReferenceComponents.md)
- **Purpose**: Reference components demonstrating best practices
- **Use When**: Creating new components and need examples
- **Key Topics**:
  - UserLoginInfoEditor (form editor reference)
  - UserManagement (list/grid reference)
  - MessageManagement (multi-panel reference)
  - FormMasterDetail (master-detail reference)
  - Component patterns and checklist

### [TwoLevelDropdownStandards.md](./standards/TwoLevelDropdownStandards.md)
- **Purpose**: Two-level (category → sub-items) dropdown that stays open when moving from first to second level
- **Use When**: Building FIELD-style or Api Operation-style cascading selectors
- **Key Topics**: One Portal + one wrapper; first and second level as siblings; close timer only on wrapper `onMouseLeave`; `items-start` so first column is not stretched; reference: `DataSetEditor.tsx` (Api Operation)

### [WijmoGridMultiSelectStandards.md](./standards/WijmoGridMultiSelectStandards.md)
- **Purpose**: Wijmo FlexGrid 多选（ListBox / MultiRange）、获取选中行、双列表转移标准
- **Use When**: 实现网格多选、双列表（Available / Selected）箭头转移、或需通过 ref 读取 selectedRows
- **Key Topics**: selectionMode（Row / ListBox / MultiRange）；ref 取控件（`ref.current?.control ?? ref.current`）；`grid.selectedRows` 与 `row.dataItem`；仅通过中间箭头按钮转移、不在行点击时转移；参考：`RoleEditorModal.tsx`、`TransactionReportEditor.tsx`、`DataGridLayout.tsx`

### [WijmoGridNullRefFixes.md](./standards/WijmoGridNullRefFixes.md)
- **Purpose**: FlexGrid 与弹窗/Context Menu 配合时出现的 editRange、finishEditing 空引用错误的统一修复规则
- **Use When**: 遇到或预防 `Cannot read properties of null (reading 'editRange')` / `(reading 'finishEditing')`；修改/新增 FlexGrid 相关功能时勿移除既有防护
- **Key Topics**: 通过应用内 patch（`src/wijmoPatches.ts`）防护 `DirectiveCellFactoryBase._autoSizeIfRequired` 与 `FlexGrid.finishEditing`；`index.tsx` 须在应用最前 `import './wijmoPatches'`

### [ModalPopupStandards.md](./standards/ModalPopupStandards.md)
- **Purpose**: 弹窗/Modal 行为：点击 backdrop 不关闭
- **Use When**: 实现或修改任何 Modal、Popup、Dialog
- **Key Topics**: Backdrop 使用 `onClick={(e) => e.stopPropagation()}`，不调用 onClose；内容区保持 stopPropagation；仅通过关闭按钮或确定/取消关闭

### [JsonEditingStandards.md](./standards/JsonEditingStandards.md)
- **Purpose**: JSON serialization for UI / memo / config textarea — always pretty-print
- **Use When**: Editing or stringifying JSON that is shown in a textarea or stored for display
- **Rule**: Use `JSON.stringify(value, null, 2)` so output has 2-space indentation and line breaks (reference: `src/helper/integrationPayloadHelper.ts`)

### [DesignPanel_DragAndDropStandards.md](./standards/DesignPanel_DragAndDropStandards.md)
- **Purpose**: Comprehensive guidelines for implementing drag and drop functionality
- **Use When**: Implementing drag and drop features, especially with nested elements
- **Key Topics**:
  - All required event handlers (draggable, onDragStart, onDragEnd, onDragOver, onDrop, onMouseMove, onMouseLeave)
  - Handling nested draggable elements
  - Preventing child hover/drag from affecting parent
  - State management patterns
  - Tree manipulation functions
  - Visual feedback patterns
  - Troubleshooting guide
  - Reference implementation: `TestDomDragAndDrop.tsx`

---

## Layout Patterns

### [TailwindFlexBoxRemainSpace.md](./layout/TailwindFlexBoxRemainSpace.md)
- **Purpose**: Critical pattern for handling remaining space in Tailwind flexbox layouts
- **Use When**: Creating layouts with flex containers that need to fill remaining space
- **Key Topics**:
  - Horizontal remaining space pattern (`w-1 flex-auto`)
  - Vertical remaining space pattern (`h-1 flex-auto`)
  - Why `flex-1` doesn't work in this codebase

### [ApplicationConfigurationMenu.md](./layout/ApplicationConfigurationMenu.md)
- **Purpose**: Analysis of dynamic sub-menu implementation for Application Configuration
- **Use When**: Working on menu/navigation features or application configuration UI
- **Key Topics**:
  - Dynamic menu generation from user application list
  - AngularJS vs React implementation differences
  - Routing logic based on GlobalGuid
  - Menu click handlers and route parameters

---

## Usage Guidelines

### For AI Assistants

When working on UI-related tasks:

1. **Start with UIMainPrompt.md**: Read this file first to understand the UI structure
2. **Reference specific standards**: Based on the task, read the relevant standard files
3. **Check reference components**: Use `ReferenceComponents.md` for real-world examples
4. **Follow layout patterns**: Use `TailwindFlexBoxRemainSpace.md` for flexbox layouts

### For Developers

1. **Creating a new page**: 
   - Read `standards/PageLayoutStandards.md`
   - Check `standards/ReferenceComponents.md` for similar pages
   - Follow the standard page structure

2. **Styling components**:
   - Buttons: See `standards/ButtonStandards.md`
   - Forms: See `standards/FormStandards.md`
   - Theme: See `standards/ThemeUsageStandards.md`

3. **Layout issues**:
   - Flexbox: See `layout/TailwindFlexBoxRemainSpace.md`
   - Menu: See `layout/ApplicationConfigurationMenu.md`

---

## Quick Reference

| Task | File to Read |
|------|-------------|
| Create new page | `standards/PageLayoutStandards.md` + `standards/ReferenceComponents.md` |
| Style buttons | `standards/ButtonStandards.md` |
| Create forms | `standards/FormStandards.md` |
| Use theme system | `standards/ThemeUsageStandards.md` |
| Tailwind CSS patterns | `standards/TailwindCSSStandards.md` |
| Find examples | `standards/ReferenceComponents.md` |
| **JSON editing (pretty-print)** | **`standards/JsonEditingStandards.md`** |
| Drag and drop | `standards/DesignPanel_DragAndDropStandards.md` |
| Flexbox layout | `layout/TailwindFlexBoxRemainSpace.md` |
| Menu implementation | `layout/ApplicationConfigurationMenu.md` |

---

## Key Principles

1. **Always use theme tokens**: Never hardcode colors. Use `theme.*` classes or `t()` function.

2. **Follow standard layouts**: Use the standard page structure from `PageLayoutStandards.md`.

3. **Consistent spacing**: Follow spacing standards for padding, margins, and gaps.

4. **Reference components**: Use reference components as templates for new pages.

5. **Test theme switching**: Always test components with different themes.

6. **Use flexbox patterns**: Follow `TailwindFlexBoxRemainSpace.md` for flex layouts.

---

## Component Checklist

When creating a new component, verify:

- [ ] Uses standard page layout structure (`PageLayoutStandards.md`)
- [ ] Uses theme tokens (no hardcoded colors) (`ThemeUsageStandards.md`)
- [ ] Follows spacing standards (`PageLayoutStandards.md`)
- [ ] Uses standard button patterns (`ButtonStandards.md`)
- [ ] Uses standard form field patterns (`FormStandards.md`)
- [ ] Matches reference component patterns (`ReferenceComponents.md`)
- [ ] Follows Tailwind CSS standards (`TailwindCSSStandards.md`)
- [ ] Implements drag and drop correctly if needed (`DesignPanel_DragAndDropStandards.md`)
- [ ] Follows flexbox patterns (`layout/TailwindFlexBoxRemainSpace.md`)
- [ ] Includes proper error handling
- [ ] Includes loading states
- [ ] Follows accessibility best practices

---

## Related Documents

- **Reference index**: See `../ReactAppReferenceIndex.md` for project overview and API/conventions

---

*Last Updated: 2026-01-16*
