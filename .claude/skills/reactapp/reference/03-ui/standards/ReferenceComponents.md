# Reference Components

## Overview

This document lists reference components that demonstrate best practices for UI implementation. Use these components as templates when creating new pages or components.

## Standard Reference Pages

### 1. UserLoginInfoEditor

**Location**: `src/components/admin/UserLoginInfoEditor.tsx`

**Purpose**: Form editor page with standard layout

**Key Features**:
- Header toolbar with title and action buttons
- Two-column form layout (left-right split)
- Standard form field patterns
- Proper spacing and theming
- Tab data caching integration

**Layout Pattern**:
```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header */}
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>Page Title</div>
    <div className="flex items-center space-x-2">
      {/* Action buttons */}
    </div>
  </div>

  {/* Content */}
  <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
    <div className="h-full w-full overflow-auto px-5 py-5">
      <div className="flex flex-wrap gap-10 w-full">
        {/* Form fields */}
      </div>
    </div>
  </div>
</div>
```

**Use When**: Creating form editor pages

### 2. UserManagement

**Location**: `src/components/admin/UserManagement.tsx`

**Purpose**: List/grid page with standard layout

**Key Features**:
- Header toolbar with title and action buttons
- Wijmo FlexGrid integration
- Context menu implementation
- Standard spacing and theming
- Tab navigation integration

**Layout Pattern**:
```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header */}
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>List Title</div>
    <div className="flex items-center space-x-2">
      {/* Action buttons */}
    </div>
  </div>

  {/* Grid */}
  <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
    <FlexGrid itemsSource={data} style={{ width: '100%', height: '100%' }} />
  </div>
</div>
```

**Use When**: Creating list/grid pages

**CollectionView state (grid itemsSource)**: Initialize state with `useState<any>([])` and on load error set back to `[]`, never `null`, so FlexGrid always receives a valid itemsSource. See `.claude/react-app/reference/03-ui/standards/WijmoCollectionViewStandards.md`.

**Context menu (grid/list)**: For list pages with row actions, use the standard **Action column + floating context menu**: first column = "Actions" with a pencil+bars icon button per row; click opens a floating popup menu; no button column on the right. Reference: **DataModelDesign** (`src/components/admin/MyApplicationEditor/DataModelDesign.tsx`), **RestApiImportManagement** (`src/components/dbmgt/RestApiImportManagement.tsx`). Full standard: `.claude/react-app/reference/03-ui/standards/ContextMenuStandards.md`.

**Two-level dropdown (FIELD / Api Operation style)**: For category → sub-items cascading menus (e.g. Api Operation: integration → API operations), use one Portal with first level and second level as siblings in a single wrapper; close-delay timer only on wrapper `onMouseLeave`; no `onMouseLeave` on first-level rows; wrapper `items-start` so first column is not stretched. Reference: **DataSetEditor** (`src/components/dbmgt/DataSetEditor.tsx`, Api Operation dropdown). Full standard: `.claude/react-app/reference/03-ui/standards/TwoLevelDropdownStandards.md`.

### 3. MessageManagement

**Location**: `src/components/message/MessageManagement.tsx`

**Purpose**: Complex multi-panel layout

**Key Features**:
- Left-right split layout
- Multiple panels with proper spacing
- Resizable panels (if needed)
- Standard header and content sections

**Layout Pattern**:
```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header */}
  <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection} flex-shrink-0 mb-1`}>
    {/* Header content */}
  </div>

  {/* Multi-panel content */}
  <div className="flex h-full">
    {/* Left panel */}
    <div className={`w-[100px] ${theme.mainContentSection} overflow-y-auto flex-shrink-0`}>
      {/* Left content */}
    </div>
    
    {/* Middle panel */}
    <div className={`flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      {/* Middle content */}
    </div>
    
    {/* Right panel */}
    <div className={`w-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
      {/* Right content */}
    </div>
  </div>
</div>
```

**Use When**: Creating complex multi-panel layouts

### 4. FormMasterDetail

**Location**: `src/components/formMgt/FormMasterDetail.tsx`

**Purpose**: Master-detail form layout

**Key Features**:
- Full-page form layout
- Proper theme usage
- Loading states
- Error handling

**Layout Pattern**:
```tsx
<div className={`w-full h-full flex ${theme.default}`}>
  <div className={`flex-auto h-full overflow-auto ${theme.mainContentSection}`}>
    {/* Form content */}
  </div>
</div>
```

**Use When**: Creating master-detail forms

## Standard Components

### Header Component

**Location**: `src/components/mainLayout/Header.tsx`

**Purpose**: Application header with navigation and actions

**Key Features**:
- Standard header height (h-16)
- Theme integration
- Responsive design
- Icon buttons

**Use When**: Understanding header patterns

### LandingPage Component

**Location**: `src/components/mainLayout/LandingPage.tsx`

**Purpose**: Main application layout wrapper

**Key Features**:
- Overall page structure
- Sidebar integration
- Tab navigation
- Outlet for page content

**Use When**: Understanding overall page structure

## Component Patterns

### Standard Button Group

**Pattern**:
```tsx
<div className="flex items-center space-x-2">
  <button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}>
    <i className="fa-solid fa-rotate"></i>
  </button>
  <button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}>
    <i className="fa-solid fa-save"></i>
  </button>
</div>
```

**Reference**: See UserLoginInfoEditor.tsx, UserManagement.tsx

### Standard Form Field

**Pattern**:
```tsx
<div className="flex items-center py-1">
  <label className={`w-32 text-xs ${theme.label} mr-2`}>Label</label>
  <input
    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
  />
</div>
```

**Reference**: See UserLoginInfoEditor.tsx

### Standard Context Menu

**Pattern**:
```tsx
{isVisible && (
  <div
    className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
    style={{ left: x, top: y }}
  >
    <button
      className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
      onClick={handleAction}
    >
      <i className="fa fa-edit mr-2"></i>
      Action
    </button>
  </div>
)}
```

**Reference**: See UserManagement.tsx

## How to Use Reference Components

### Step 1: Identify Your Page Type

- **Form Editor**: Use UserLoginInfoEditor as reference
- **List/Grid**: Use UserManagement as reference
- **Multi-Panel**: Use MessageManagement as reference
- **Master-Detail**: Use FormMasterDetail as reference

### Step 2: Copy the Layout Structure

Copy the basic layout structure from the reference component, including:
- Container div with proper classes
- Header toolbar structure
- Content area structure

### Step 3: Adapt to Your Needs

- Replace placeholder content with your actual content
- Adjust spacing and sizing as needed
- Add or remove sections as required
- Maintain theme usage

### Step 4: Verify Standards

Check that your component follows:
- Page layout standards (PageLayoutStandards.md)
- Button standards (ButtonStandards.md)
- Form standards (FormStandards.md)
- Theme usage standards (ThemeUsageStandards.md)

## Component Checklist

When creating a new component, verify:

- [ ] Uses standard page layout structure
- [ ] Uses theme tokens (no hardcoded colors)
- [ ] Follows spacing standards
- [ ] Uses standard button patterns
- [ ] Uses standard form field patterns (if applicable)
- [ ] Includes proper error handling
- [ ] Includes loading states
- [ ] Follows accessibility best practices
- [ ] Matches reference component patterns

## Related Documents

- See `PageLayoutStandards.md` for layout patterns
- See `ButtonStandards.md` for button patterns
- See `FormStandards.md` for form patterns
- See `ThemeUsageStandards.md` for theme usage
