---
name: angularjs-to-react-migration
description: Use when migrating features from the AngularJS application to React. Contains all standards, patterns, and conversion rules for this project's migration (service conversion, UI rules, Wijmo, theme, flexbox).
---

# AngularJS to React Migration Skill

Use this skill when migrating features from the AngularJS application to React. This skill contains all standards, patterns, and conversion rules for the migration project.

**Detailed rules**: Full prompts in repo `.claude/reference/` — see `.claude/reference/MainPrompt.md`, `.claude/reference/02-migration/` (includes Wijmo ComboBox Selection Rule and cascading pointer), `.claude/reference/03-ui/UIMainPrompt.md`, `.claude/reference/03-ui/standards/WijmoComboBoxCascadingStandards.md` (parent DDL → child DDLs: init ref only, remove handler before load, attach after bind), `.claude/reference/03-ui/standards/WijmoFlexGridColumnVisibility.md` (show/hide columns via FlexGridColumn `visible` prop; do not conditionally render columns), `.claude/reference/01-reference-guides/AngularJsReferenceGuide.md`, `.claude/reference/05-Specific-Component-Rule/DEBUG_LOG_USAGE.md`.

## Project Context

- **React App Path**: This repo (`app-react2`)
- **AngularJS Solution Path**: `C:\DevApp\App\PlmApplication`
- **Solution Root**: `C:\DevApp\App\`
- **Stack**: React 18 + TypeScript + Redux Toolkit + Tailwind CSS + Wijmo

## AngularJS Reference Locations

### Frontend (AngularJS)
- **Controllers**: `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\`
- **Services**: `{SolutionRoot}\PlmApplication\Scripts1x\Services\`
- **Routes**: `{SolutionRoot}\PlmApplication\Scripts1x\mgtRoute.js`

### Backend (.NET)
- **Server Controllers (MVC)**: `{SolutionRoot}\PlmApplication\Server\Controllers\`
- **Server Views (Razor)**: `{SolutionRoot}\PlmApplication\Server\Views\`
- **Server WebAPI**: `{SolutionRoot}\PlmApplication\Server\WebApi\`
- **Business Logic**: `{SolutionRoot}\APP.BL\` (e.g. AppTransactionBL, AppMasterDetailFormDataLoadBL, AppFormBL, AppSearchBL)
- **DTOs**: `{SolutionRoot}\APP.Components.Dto\` — Entity: `EntityDto\`, Extended: `EntityExdto\`, User-defined: `UserDefine\` (AppFormData, AppIntegration, Search, ProjectWorkFlow), Enums: `AppEnums.cs`

---

## Service Conversion Rules

### 1. Method Name Preservation
Keep the exact method name from Angular (PascalCase preserved).

```typescript
// Angular: RetrieveAppProjectOrWorkFlows: function(...)
// React:
async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any>
```

### 2. Query Parameters - Always Include All Parameters
Use `${param || ''}` to convert null/undefined to empty string:

```typescript
async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType=${projectOrWorflowType || ''}&isPredefined=${isPredefined || ''}&isHierarchy=${isHierarchy || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app project or workflows');
    return response.json();
}
```

### 3. POST Requests with Body

```typescript
async SaveProjectSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectSettingExDto`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project setting');
    return response.json();
}
```

### 4. Parameter Types
- **3+ types**: Use `any` (e.g., `string | number | null` → `any`)
- **2 types**: Can use union (e.g., `string | number`)

### 5. Service Class Structure

```typescript
import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class ServiceName {
    async MethodName(param1: any, param2: any): Promise<any> {
        const response = await fetch(`${endpoints.BASE_URL}/webapi/Controller/Action?param1=${param1 || ''}&param2=${param2 || ''}`, {
            headers: getHeaders()
        });
        if (!response.ok) throw new Error('Failed to [action]');
        return response.json();
    }
}

export const serviceName = new ServiceName();
```

---

## UI Conversion Rules

### Flexbox Remaining Space - CRITICAL
**Never use `flex-1`**. Use instead:
- **Horizontal**: `w-1 flex-auto`
- **Vertical**: `h-1 flex-auto`

### Font Awesome 6 Icon Conversion
Always use `fa-solid` prefix:
- `fa-file-text-o` → `fa-solid fa-file-lines`
- `fa-trash-o` → `fa-solid fa-trash`
- `fa-pencil` → `fa-solid fa-pencil`
- `fa-navicon` → `fa-solid fa-bars`
- `fa-refresh` → `fa-solid fa-rotate`
- `fa-edit` → `fa-solid fa-pen-to-square`
- `fa-save` → `fa-solid fa-floppy-disk` (no bare `fa fa-*`)

### Theme System - Always Use Theme Tokens
```tsx
const { t, tw, theme } = useTheme();

// Layout
<div className={theme.mainContentSection}>

// Button
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>

// Input
<input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} />

// Label
<label className={`w-32 text-xs ${theme.label} mr-2`}>
```

### Standard Page Layout

```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header Toolbar */}
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>Page Title</div>
    <div className="flex items-center space-x-2">
      {/* Action buttons */}
    </div>
  </div>

  {/* Main Content Area — use h-1 flex-auto (never flex-1); scrollable: w-full h-1 flex-auto overflow-auto px-5 py-5 */}
  <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
    {/* Page content */}
  </div>
</div>
```

### Button Standards
```tsx
// Standard button
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>

// Small icon button
<button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`} title="Tooltip">
  <i className="fa-solid fa-icon-name"></i>
</button>
```

### Form Field Standards
```tsx
<div className="flex items-center py-1">
  <label className={`w-32 text-xs ${theme.label} mr-2`}>Field Label</label>
  <input
    type="text"
    autoComplete="off"
    value={value}
    onChange={e => handleChange(e.target.value)}
    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
  />
</div>
```

---

## Wijmo Component Rules

### ComboBox Selection
Set selected value to empty string first, then use setTimeout:
```tsx
comboBox.selectedValue = '';
setTimeout(() => {
  comboBox.selectedValue = actualValue;
}, 0);
```

### FlexGridCellTemplate Data Access
**CRITICAL**: Use `cell.item`, NOT `cell.dataItem`:
```tsx
<FlexGridCellTemplate cellType="Cell" template={(cell: any) => {
  const dataItem = cell.item;
  return <div>{dataItem.Name}</div>;
}} />
```

### FlexGrid Control Access from Ref
Access via `.control` property:
```tsx
const flex = flexGridRef.current.control;
const selectedRows = flex.selectedRows || [];
```

### FlexGrid Row Selection (selectionChanged)
**Use this pattern** when the selected row must drive parent state (e.g. master-detail: left grid selection updates right panel). React/Wijmo may pass an event object or the grid; `collectionView.currentItem` can be stale when `selectionChanged` fires.

1. Get the grid: `const flex = s?.control ?? s` (handles both event object and direct grid).
2. Get selected row index: `const rowIndex = flex?.selection?.row ?? -1`.
3. Get the row's data: `const item = flex.rows[rowIndex]?.dataItem`.

```tsx
const handleRowSelect = useCallback((s: any) => {
  const flex = s?.control ?? s;
  const rowIndex = flex?.selection?.row ?? -1;
  if (rowIndex < 0 || !flex?.rows?.length) return;
  const item = flex.rows[rowIndex]?.dataItem as MyDto | null;
  if (item) setCurrentItem(item);
}, []);

<FlexGrid
  itemsSource={itemsCV}
  selectionMode="Row"
  selectionChanged={handleRowSelect}
  ...
/>
```

When the selected item drives a child (e.g. detail panel), pass a **stable key** so the child remounts on selection change: `key={currentItem?.Id ?? 'none'}`.

**Reference**: `BusinessPartnerManagement.tsx`, `RestApiImportEditor.tsx`, `CompanyRoleSetup.tsx`.

### FlexGrid Spacer Column
Always add a final spacer column (last column, fills remaining width):
```tsx
<FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
```

### FlexGrid Multi-Select and Dual-List Transfer
For **multi-row selection** (ListBox / MultiRange), **reading selected rows from ref**, and **dual-list (Available / Selected) with arrow buttons**: follow `.claude/reference/03-ui/standards/WijmoGridMultiSelectStandards.md`. Summary: use `selectionMode="ListBox"`; get grid via `ref.current?.control ?? ref.current`; use `grid.selectedRows` and `row.dataItem`; transfer only on arrow button click, not on row click. Reference components: `RoleEditorModal.tsx`, `TransactionReportEditor.tsx`.

---

## Dictionary Response API (getMassEntitiesLookupItem)
When calling APIs that accept pipe-separated entity codes and return a dictionary: build entity code list programmatically, join with `'|'`, access response by key. Use constants for entity codes; provide `|| []` fallback for CollectionView. See `.claude/reference/02-migration/ConverterAngularServiceToReact.md` and `src/components/project/ProjectMgt.tsx`.

---

## Server-Side Model vs API
Some server-side model properties (e.g. Razor views) may NOT be in API responses. Prefer adding to API; otherwise use reasonable defaults and document. See `.claude/reference/02-migration/ConverterAngularJsPage.md` and `DevelopmentPrompt.md`.

---

## Resizable Panels / Popups
- **Resizable panels**: Follow `src/components/transaction/metaDataManagement.tsx` (dimensions + resize flags, document mousemove/mouseup, cleanup). Use `w-1 flex-auto` / `h-1 flex-auto` for fill.
- **Resizable popup buttons**: Fullscreen toggle `w-5 h-5`, close `w-9 h-9`; container `ml-4 flex items-center gap-1`. See `.claude/reference/02-migration/ConverterAngularJsPage.md`.

---

## Reference Components
- Form editor: `src/components/admin/UserLoginInfoEditor.tsx`
- List/grid: `src/components/admin/UserManagement.tsx`
- Multi-panel: `src/components/message/MessageManagement.tsx`
- Master-detail: `src/components/formMgt/FormMasterDetail.tsx`

---

## Enum Values - Use useEnumValues Hook
```tsx
import { useEnumValues } from '../../hooks/useEnumDictionary';

const MyComponent: React.FC = () => {
  const emAppMyEnum = useEnumValues('EmAppMyEnum');

  // Use with optional chaining
  if (emAppMyEnum?.Select !== undefined) {
    doSomething(emAppMyEnum.Select);
  }
};
```

---

## State Management Patterns

### AngularJS to React State
```typescript
// AngularJS: $scope.dataModel.currentFormData = formData;
// React:
const [dataModel, setDataModel] = useState({
    currentFormData: null,
    currentFormStructure: null
});

setDataModel(prev => ({
    ...prev,
    currentFormData: formData,
    currentFormStructure: formStructure
}));
```

### Data Loading Pattern
```typescript
useEffect(() => {
    const loadData = async () => {
        const formStructure = await loadFormStructure();
        if (rootPrimaryKeyValue) {
            await loadFormData(formStructure);
        } else {
            prepareNewFormData(formStructure);
        }
    };
    loadData();
}, [transactionId, rootPrimaryKeyValue]);
```

---

## Debug Logging
**Always use `appHelper.debugLog()` instead of `console.log()`** (global switch: `window.__DEBUG_ENABLED__` or `appHelper.setDebugEnabled()`). See `.claude/reference/05-Specific-Component-Rule/DEBUG_LOG_USAGE.md`.
```typescript
import appHelper from '../../helper/appHelper';

appHelper.debugLog('消息', { 数据对象 });
```

---

## JSON Pretty-Print
Use `JSON.stringify(value, null, 2)` for display in textarea/memo/config. See `.claude/reference/03-ui/standards/JsonEditingStandards.md`.

---

## Migration Workflow

1. **Find AngularJS Implementation**: Locate controller, service, view, WebAPI
2. **Understand Business Logic**: Check `APP.BL\` for data processing
3. **Find DTOs**: Check `APP.Components.Dto\` for data structures
4. **Map to React**: Controllers → Components, Services → webapi/, $scope → useState/Redux
5. **Handle Missing Data**: Server-side model properties may not be in API responses
6. **Follow UI Standards**: Use theme tokens, standard layouts, proper spacing
7. **Test Parity**: Ensure React matches AngularJS behavior

---

## Key Principles

1. **Always reference AngularJS implementation** - Source of truth for functionality
2. **Preserve business logic** - Don't change how features work
3. **Handle missing data gracefully** - Some server-side properties may not be in APIs
4. **Use theme system** - Never hardcode colors
5. **Follow UI standards** - Consistent layouts, buttons, forms
6. **Use `appHelper.debugLog()`** - Never use `console.log()` for debug

## Quality Standards
- ✅ No TypeScript errors
- ✅ No linter errors
- ✅ No console errors/warnings
- ✅ Clean imports (no `.tsx` suffix)
- ✅ UI matches standards
- ✅ Functionality matches AngularJS
