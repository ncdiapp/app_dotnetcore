# AngularJS to React Conversion Guide

You are guiding a developer through converting an old AngularJS feature into our React + Redux + Tailwind + Wijmo app.

## Reference Guide

**IMPORTANT**: Before starting, refer to `Prompt/01-reference-guides/AngularJsReferenceGuide.md` for:
- Solution folder structure and paths
- Business logic class locations
- DTO definitions and locations
- How to find and reference the original AngularJS code

**React Application Path**: `C:\DevApp\app-react` (or your React app location)

---

Follow this interactive workflow and pause after each major step so the user can supply inputs or confirm before you continue.

## 0. Collect Inputs
- **Reference the AngularJS Solution**: Use `Prompt/01-reference-guides/AngularJsReferenceGuide.md` to locate:
  - AngularJS controllers, services, routes
  - Server controllers, views, WebAPI endpoints
  - Business logic classes
  - DTO definitions
- Ask for any screenshots or notes that show the intended UI or nuanced behavior.
- Request confirmation that the located assets cover the feature; if something is missing, ask the user to point you to it in the legacy project.
- Gather the related API interface, backend methods, business logic, and DTO definitions (C#) from the legacy codebase.
- **IMPORTANT**: Understand the difference between server-side model properties (available in Razor views) and API response properties. Some data may be in server models but NOT in API responses.
- Confirm the input set with the user before planning the migration.

## 1. Baseline the legacy feature
- Summarize routing, controller responsibilities, directives, and data flow.
- **Identify server-side model properties**: Check what properties are available in the server controller action's model (e.g., `AppTransactionExDto`) that may NOT be in API responses. Find the DTO definition using the reference guide.
- **Trace business logic**: For each WebAPI endpoint, find the corresponding business logic class and understand what it does:
  - Data loading/retrieval logic
  - Validation rules
  - Calculations or transformations
  - Save/update operations
- **Extract the UI layout**: Focus on:
  - Sections, panels, Wijmo controls, modals
  - CSS classes and styling patterns
  - Theme usage (`dictThemeParam*` in AngularJS)
  - Custom CSS overrides
  - Responsive behavior
- List API calls, DTO fields, validation logic, busy loader usage, and theme hooks.
- **Map data sources**: Identify which data comes from:
  - Server-side model (Razor view `@model`) - check DTO in reference guide
  - API endpoints (`GetFormStructure`, `GetFormData`, etc.) - check WebAPI controller and business logic
  - Business logic processing - check business logic classes
  - Client-side calculations
- **Document DTO structure**: Note all DTOs used, their properties, and where they're defined (use reference guide).
- Share the summary with the user and wait for confirmation or corrections.

## 2. Model the data layer
- Convert DTOs and key objects into TypeScript representations; default to `any` for payloads/results so backend schema changes don't require constant interface maintenance.
- Map legacy service methods to the React-era services (e.g., `adminsvc.ts`); document request/response shapes, success flags, and error handling.
- **API Parameter Placeholders**: When building query strings with `URLSearchParams`, always include ALL parameters even when they are null or undefined. Use empty strings (`''`) for null values to maintain consistent URL structure. Example: `params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');` This ensures the backend receives all expected parameters in the correct format.
- **Handle Missing Server-Side Model Properties**: If AngularJS uses properties from server-side models (like `TransactionOrganizedType` in `AppTransactionExDto`) that are NOT in API responses:
  - First, check if the property should be added to the API response (preferred solution)
  - If not possible, use reasonable defaults based on component context (e.g., `FormMasterDetail` component defaults to MasterDetail type = 1)
  - Document these assumptions clearly
- Identify which state belongs in Redux (shared lists, selections, busy flags) versus local component state (form values, dialog visibility).
- Present the data model plan to the user for approval.

## 3. Design the React composition
- Propose the React component tree (panels, editors, toolbars, filters) with responsibilities and props/state flow.
- Specify Redux slices/thunks/selectors and any new hooks needed.
- Choose Wijmo React controls that replace the legacy ones; list required props/events.
- **Plan Tailwind + `useTheme()` tokens**: 
  - Map AngularJS `dictThemeParam*` usage to React theme tokens
  - Identify which theme tokens exist in `BaseTheme`/`ThemeClasses`
  - Plan new theme tokens if needed (hex values only)
- Review the proposed architecture with the user before coding.

## 4. Set up Redux + services
- Scaffold or extend Redux slices and async thunks that call the mapped `adminsvc.ts` (or other service) functions.
- Ensure async flows toggle the global busy loader (`setIsBusy` / `setIsNotBusy`) and surface success/error messaging.
- Walk the user through the planned state transitions and get the go-ahead.

## 5. Build the UI incrementally
- **Recreate layout sections** with Tailwind classes and `theme.*` tokens:
  - Use `mainContentSection`, `inputBox`, `button_default`, `button_secondary`, etc.
  - Match the AngularJS layout structure
  - Preserve spacing, alignment, and visual hierarchy
- **Integrate Wijmo components** (`FlexGrid`, `ComboBox`, `MultiSelect`, etc.) and wire their events to React handlers.
- **Implement dialogs/overlays/panels** patterned after `ThemeManagementPanel` and `ThemeEditorPanel`.
- **Mirror legacy validation, button states, and inline error messaging**. Use literal fallback labels for headings/buttons when theme tokens don't exist to avoid multiple translation passes.
- After each major UI segment, show diffs or excerpts and confirm with the user.
- **Hook up global messaging**: wire `useErrorMessage`, ensure messages stack, and rely on the shared `ErrorMessageButton` / `ErrorMessagePopup` duo so success/warning/error states appear immediately without manual clearing.

## 6. Align theme + styling
- **Audit `dictThemeParam*` usage** from the legacy assets; ensure matching tokens exist in `BaseTheme`/`ThemeClasses`.
- **Update theme files** if new tokens are required:
  - Update `baseThemeToDict`, `dictToBaseTheme`
  - Update default `light.json` / `dark.json` (hex values only)
- **Extend `applyThirdPartCssOverrides`** with any Wijmo overrides from the legacy implementation.
- **Match CSS classes**: Convert AngularJS CSS classes to Tailwind equivalents:
  - Use Tailwind utility classes where possible
  - Preserve custom styles in theme system
  - Maintain responsive behavior
- Demonstrate the theming updates and wait for user confirmation.

## 7. Preserve logic + workflows
- Port controller logic, watchers, and directive behaviors into React hooks/utilities.
- Maintain default selections, focus handling, and keyboard shortcuts.
- Support reset/revert actions by deep-cloning baseline data.
- Review the functional parity checklist with the user.

## 8. Test + QA
- Guide the user through manual QA: CRUD flows, busy loader, theme switching, Wijmo styling, navigation (sidebar, tabs, routing).
- Compare the React page against legacy screenshots; capture any discrepancies.
- Test with same data/parameters as AngularJS to ensure parity.

## 9. Finalize & handoff
- Remove or archive legacy artifacts related to the converted feature.
- Document the new components, services, and types added.
- Run lint/tests; ensure imports are clean (no `.tsx` suffix) and there's no unused code.
- Provide a concise summary of what was built and flag any follow-up tasks.

---

## UI Conversion Rules

### Wijmo ComboBox Selection Rule
- Whenever you load or refresh a Wijmo ComboBox's item source, set its selected value to an empty string first and then, inside `setTimeout(() => { ... }, 0)`, assign the actual selected value. This prevents Wijmo from auto-selecting the first item during render (see `src/components/admin/ApplicationSetting.tsx` for reference).

### Wijmo ComboBox Cascading (parent DDL drives child DDLs)
- When a parent ComboBox (e.g. Table/View Name) drives child ComboBox itemsSource (e.g. Identity Field, Display Fields), **do not** attach `selectedIndexChanged` in `initialized`—that would fire on first bind and clear child values. **Remove** the handler at the start of load/refresh; **attach** it only after initial data binding is complete (e.g. `setTimeout(attachHandler, 200)` in load's `finally`). See **`../03-ui/standards/WijmoComboBoxCascadingStandards.md`** and `src/components/admin/EntityListOfValue/AppEntityInfoEdit.tsx` for the full pattern.

### Wijmo FlexGridCellTemplate Data Access
- **CRITICAL**: In Wijmo React's `FlexGridCellTemplate`, always use `cell.item` to access the row data item, NOT `cell.dataItem`. The `cell.dataItem` property exists in Wijmo's JavaScript API but is undefined in the React wrapper. Using `cell.dataItem` will cause templates to fail silently with empty cells.
- Example: `<FlexGridCellTemplate cellType="Cell" template={(cell: any) => { const dataItem = cell.item; return <div>{dataItem.Name}</div>; }} />`
- For group header rows, check `if (!cell.item)` and return an empty div or null. Group headers are handled separately by Wijmo.
- See `src/components/transaction/metaDataManagement.tsx` for a working example with grouped data.

### Wijmo FlexGrid Control Access
- **CRITICAL**: When accessing the Wijmo FlexGrid control from a ref in React, you must use `.control` property to get the actual Wijmo control instance. The ref points to the React wrapper component, not the underlying Wijmo control.
- **Pattern**: `const flex = flexGridRef.current.control;` - Always access the control via `.control` property.
- **Example**:
  ```typescript
  const flexGridRef = useRef<any>(null);
  
  // In JSX
  <FlexGrid ref={flexGridRef} ... />
  
  // In handler function
  const deleteHandler = () => {
    if (!flexGridRef.current) return;
    const flex = flexGridRef.current.control; // Access the actual Wijmo control
    if (!flex) return;
    
    // Now you can use flex.selectedRows, flex.selection, flex.rows, etc.
    const selectedRows = flex.selectedRows || [];
  };
  ```
- **Why**: The React `FlexGrid` component is a wrapper around the native Wijmo control. The ref points to the React component, and you need to access `.control` to get the underlying Wijmo `FlexGrid` instance where properties like `selectedRows`, `selection`, `rows`, etc. are available.
- See `src/components/transaction/metaDataTableDesign.tsx` for a working example.

### Page Layout Consistency
- New React pages should follow the `UserLoginInfoEditor` layout: a header toolbar (title + icon buttons), followed by the main body wrapped in `theme.mainContentSection` with rounded corners and scroll handling. Reuse that structure unless there's a strong user requirement to diverge. (see `src/components/admin/UserLoginInfoEditor.tsx` and `src/components/admin/UserManagement.tsx` for reference).

### Font Awesome Icon Conversion
- **CRITICAL**: The React app uses Font Awesome 6.4.0 (loaded via CDN in `public/index.html`), while the legacy AngularJS app likely used Font Awesome 4. Icon class names have changed significantly between versions.
- **Always use style prefixes**: Font Awesome 6 requires style prefixes (`fa-solid`, `fa-regular`, `fa-brands`). Most icons should use `fa-solid`.
- **Common icon conversions**:
  - `fa-file-text-o` → `fa-solid fa-file-lines` (or `fa-solid fa-file-text`)
  - `fa-trash-o` → `fa-solid fa-trash` (or `fa-solid fa-trash-can`)
  - `fa-pencil` → `fa-solid fa-pencil` (still valid, but needs prefix)
  - `fa-eye` → `fa-solid fa-eye` (still valid, but needs prefix)
  - `fa-navicon` → `fa-solid fa-bars` (hamburger menu icon)
  - `fa-refresh` → `fa-solid fa-rotate` (or `fa-solid fa-arrow-rotate-right`)
  - `fa-edit` → `fa-solid fa-pen-to-square` (or `fa-solid fa-pencil`)
  - `fa-plus` → `fa-solid fa-plus` (still valid, but needs prefix)
  - `fa-table` → `fa-solid fa-table` (still valid, but needs prefix)
  - `fa-bolt` → `fa-solid fa-bolt` (still valid, but needs prefix)
- **Format**: Always use the format `<i className="fa-solid fa-icon-name"></i>` instead of the old `<i className="fa fa-icon-name"></i>` format.
- When in doubt, check the Font Awesome 6 icon library at https://fontawesome.com/icons or test the icon in the browser console to verify it renders correctly.

### Enum Value Conversion (useEnumValues)
- **CRITICAL**: Never hardcode enum values in React components. The server provides all enums with the "EmApp" prefix (e.g., `EmAppBuiltInQueryType`, `EmAppApplicationSettingCategory`, `EmAppDataServerType`) through the enum dictionary.
- **Always use `useEnumValues` hook**: Import `useEnumValues` from `'../../hooks/useEnumDictionary'` and use it to fetch enum values from the server.
- **Usage pattern**:
  ```typescript
  import { useEnumValues } from '../../hooks/useEnumDictionary';
  
  const MyComponent: React.FC = () => {
    const emAppMyEnum = useEnumValues('EmAppMyEnum');
    
    // Use enum values with optional chaining and null checks
    onClick={() => {
      if (emAppMyEnum?.Select !== undefined) {
        doSomething(emAppMyEnum.Select);
      }
    }}
  ```
- **Key points**:
  - The hook returns `Record<string, number> | null`, so always use optional chaining (`?.`) when accessing enum values.
  - Add null/undefined checks before using enum values in function calls to satisfy TypeScript.
  - Disable buttons/controls if the enum isn't loaded yet: `disabled={!emAppMyEnum?.Select}`
  - See `src/components/admin/ApplicationSetting.tsx` for a reference implementation.
- **Do NOT**: Create local enum objects like `const MyEnum = { Select: 0, Update: 1 }`. Always fetch from the server using `useEnumValues`.

### Resizable Panels Implementation
- When implementing resizable panels (horizontal or vertical dividers), follow the pattern established in `src/components/transaction/metaDataManagement.tsx`.
- **State Management**:
  - Use `useState` for panel dimensions (e.g., `leftPanelWidth`, `queryResultHeight`) with default values.
  - Use `useState` for resize flags (e.g., `isResizingHorizontal`, `isResizingVertical`) to track active resize operations.
- **Resize Handlers Pattern**:
  - **Start handler**: `handleHorizontalResizeStart` / `handleVerticalResizeStart` - Sets resize flag, prevents default, updates cursor and user-select styles on document.body.
  - **Move handler**: `handleHorizontalResize` / `handleVerticalResize` - Calculates new dimensions based on mouse position, enforces min/max constraints, updates state.
  - **End handler**: `handleHorizontalResizeEnd` / `handleVerticalResizeEnd` - Clears resize flag, restores cursor and user-select styles.
- **Event Listener Management**:
  - Use `useEffect` hooks to add/remove document-level `mousemove` and `mouseup` event listeners when resize flags are active.
  - Always clean up event listeners in the effect's return function to prevent memory leaks.
- **Visual Resizer Elements**:
  - Create a thin divider element (e.g., `w-1` for horizontal, `h-1` for vertical) with appropriate cursor styles (`cursor-col-resize` or `cursor-row-resize`).
  - Add hover effects (`hover:bg-blue-400`) and active state styling (change background color when `isResizing` is true).
  - Include visual indicators (borders or lines) to show the resizable area.
- **Panel Styling**:
  - Apply dynamic dimensions using inline styles: `style={{ width: \`${leftPanelWidth}px\`, minWidth: '200px' }}` for horizontal panels.
  - Apply dynamic dimensions: `style={{ height: \`${queryResultHeight}px\`, minHeight: '100px' }}` for vertical panels.
  - Use `calc()` for complementary panels: `style={{ width: \`calc(100% - ${leftPanelWidth}px - 4px)\` }}` to account for the resizer width.
- **Key Implementation Details**:
  - Calculate new dimensions relative to the container's bounding rectangle, not the viewport.
  - Enforce minimum and maximum constraints to prevent panels from becoming too small or too large.
  - Use `useCallback` for all resize handlers to prevent unnecessary re-renders.
  - The move handlers should check the resize flag before executing to avoid unnecessary calculations.
- **Reference**: See `src/components/transaction/metaDataManagement.tsx` for a complete working example with both horizontal (left-right) and vertical (top-bottom) resizable panels.

### Resizable Popup Standard Button Sizes
- **CRITICAL**: All resizable popups (modals, dialogs, panels) must use standardized button sizes and positioning for the fullscreen toggle and close buttons.
- **Button Group Container**:
  - Wrap both buttons in a container div with `ml-4 flex items-center gap-1` classes.
  - The `ml-4` (margin-left: 1rem) creates space between these buttons and the other action buttons on the left.
  - The `gap-1` (0.25rem) keeps the fullscreen and close buttons close together.
- **Fullscreen Toggle Button**:
  - **Size**: `w-5 h-5` (20px × 20px)
  - **Icon Size**: `text-xs`
  - **Padding**: `px-0.5`
  - **Classes**: `text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center`
  - **Icon**: `<i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>`
- **Close Button**:
  - **Size**: `w-9 h-9` (36px × 36px)
  - **Icon Size**: `text-2xl`
  - **Padding**: `px-2`
  - **Classes**: `text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center`
  - **Icon**: `&times;` (HTML entity for × symbol)
- **Complete Pattern**:
  ```tsx
  <div className="ml-4 flex items-center gap-1">
    <button
      onClick={() => setIsFullscreen(!isFullscreen)}
      className="text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center"
      title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}
    >
      <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>
    </button>
    <button
      onClick={onClose}
      className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
      title="Close"
    >
      &times;
    </button>
  </div>
  ```
- **References**: See `src/components/transaction/metaDataViewDesign.tsx`, `src/components/transaction/metaDataTableDesign.tsx`, and `src/components/transaction/TableDataPreview.tsx` for working examples.

### Tailwind Flexbox Remaining Space Pattern
- **CRITICAL**: Never use `flex-1` in Tailwind CSS. It does not work well in this codebase. Instead, use the following patterns based on the flex direction:
- **For Horizontal Remaining Space** (when parent is `flex` without `flex-col`):
  - Use: `w-1 flex-auto`
  - This makes the element take up the remaining horizontal space in a row layout.
  - Example: `<div className="flex-auto w-1 overflow-hidden">...</div>`
- **For Vertical Remaining Space** (when parent is `flex flex-col`):
  - Use: `h-1 flex-auto`
  - This makes the element take up the remaining vertical space in a column layout.
  - Example: `<div className="flex-auto h-1 overflow-hidden">...</div>`
- **Determining the Correct Pattern**:
  - Check the parent container's className:
    - If parent has `flex flex-col` or `flex-col`: use `h-1 flex-auto`
    - If parent has `flex` (without `flex-col`): use `w-1 flex-auto`
  - When in doubt, check the layout direction:
    - Row layout (horizontal): `w-1 flex-auto`
    - Column layout (vertical): `h-1 flex-auto`
- **Why**: The `flex-1` utility in Tailwind can cause layout issues in complex nested flex containers. Using `flex-auto` with explicit width/height constraints (`w-1` or `h-1`) provides more predictable behavior and better control over remaining space distribution.
- **References**: See `src/components/transaction/metaDataViewDesign.tsx` for examples of both patterns used throughout the component.

### Need to follow app theme, header, button standard.

Always wait for the user's confirmation or additional assets before moving to the next section, and keep cross-checking the legacy project folder for any missing context. The final deliverables must include the new React component files, updated Redux/services, theme/type adjustments, and any supporting documentation so the feature is production-ready.
