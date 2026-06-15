# Page Layout Standards

## Overview

This document defines the standard page layout patterns used throughout the React application. All new pages should follow these patterns to ensure consistency and maintainability.

## Standard Page Structure

### Basic Page Layout Pattern

Every page should follow this structure:

```tsx
import { useTheme } from '../../redux/hooks/useTheme';

const MyPage: React.FC = () => {
  const { t, tw, theme } = useTheme();
  
  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      {/* Header Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Page Title
        </div>
        <div className="flex items-center space-x-2">
          {/* Action buttons */}
        </div>
      </div>

      {/* Main Content Area */}
      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {/* Page content */}
      </div>
    </div>
  );
};
```

## Layout Components

### 1. Page Container

**Standard Container**:
```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
```

**Key Classes**:
- `w-full h-full` - Full width and height
- `flex flex-col` - Vertical flex layout
- `rounded-t-md rounded-b-md` - Rounded top and bottom corners
- `overflow-hidden` - Prevent content overflow

### 2. Header Toolbar

**Standard Header**:
```tsx
<div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
```

**Key Classes**:
- `flex items-center justify-between` - Horizontal flex with space between
- `px-3 py-2` - Padding: 12px horizontal, 8px vertical
- `mb-1` - Margin bottom: 4px (spacing between header and content)
- `theme.mainContentSection` - Theme background, text, and border colors

**Header Height**: Approximately 40-44px (py-2 = 8px top + 8px bottom + content height)

**Title Section**:
```tsx
<div className={`text-md font-semibold ${theme.title}`}>
  Page Title
</div>
```

**Action Buttons Section**:
```tsx
<div className="flex items-center space-x-2">
  {/* Buttons */}
</div>
```

### 3. Main Content Area

**Standard Content Container**:
```tsx
<div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
```

**Key Classes**:
- `w-full` - Full width
- `h-[200px]` - Minimum height (adjust as needed)
- `flex-auto` - Take remaining vertical space
- `overflow-hidden` - Prevent overflow (use `overflow-auto` if scrolling needed)
- `theme.mainContentSection` - Theme background, text, and border colors

**With Scrolling**:
```tsx
<div className={`w-full h-[200px] flex-auto overflow-auto ${theme.mainContentSection}`}>
  <div className="h-full w-full overflow-auto px-5 py-5">
    {/* Scrollable content */}
  </div>
</div>
```

## Layout Variations

### Left-Right Split Layout

```tsx
<div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
  <div className="flex h-full">
    {/* Left Panel */}
    <div className="w-[300px] flex-shrink-0 border-r">
      {/* Left content */}
    </div>
    
    {/* Right Panel */}
    <div className="flex-auto w-1 overflow-auto">
      {/* Right content */}
    </div>
  </div>
</div>
```

**Key Points**:
- Use `flex` for horizontal layout
- Left panel: Fixed width with `flex-shrink-0`
- Right panel: `flex-auto w-1` for remaining space (see TailwindFlexBoxRemainSpace.md)

### Top-Bottom Split Layout

```tsx
<div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection} flex flex-col`}>
  {/* Top Section */}
  <div className="h-[200px] flex-shrink-0 border-b">
    {/* Top content */}
  </div>
  
  {/* Bottom Section */}
  <div className="flex-auto h-1 overflow-auto">
    {/* Bottom content */}
  </div>
</div>
```

**Key Points**:
- Use `flex flex-col` for vertical layout
- Top section: Fixed height with `flex-shrink-0`
- Bottom section: `flex-auto h-1` for remaining space

## Spacing Standards

### Padding

- **Header**: `px-3 py-2` (12px horizontal, 8px vertical)
- **Content Area**: `px-5 py-5` (20px all around) for form pages
- **Grid/Table Content**: `px-4` (16px horizontal) or no padding

### Margins

- **Between Header and Content**: `mb-1` (4px)
- **Between Form Fields**: `py-1` (4px vertical) or `gap-10` (40px) for flex-wrap layouts
- **Between Sections**: `mb-4` (16px) or `mb-6` (24px)

### Gaps

- **Button Groups**: `space-x-2` (8px horizontal gap)
- **Form Fields (Flex Wrap)**: `gap-10` (40px gap)

## Reference Components

### Standard Reference Pages

1. **UserLoginInfoEditor** (`src/components/admin/UserLoginInfoEditor.tsx`)
   - Form editor with header toolbar
   - Left-right column layout for form fields
   - Standard spacing and theming

2. **UserManagement** (`src/components/admin/UserManagement.tsx`)
   - List page with header toolbar
   - Grid/table content area
   - Action buttons in header

3. **MessageManagement** (`src/components/message/MessageManagement.tsx`)
   - Complex left-right split layout
   - Multiple panels with proper spacing

## Common Patterns

### Form Page Pattern

```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header */}
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>Form Title</div>
    <div className="flex items-center space-x-2">
      <button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}>
        Save
      </button>
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

### List/Grid Page Pattern

```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
  {/* Header */}
  <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
    <div className={`text-md font-semibold ${theme.title}`}>List Title</div>
    <div className="flex items-center space-x-2">
      <button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}>
        Add
      </button>
    </div>
  </div>

  {/* Grid Content */}
  <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
    <FlexGrid
      itemsSource={data}
      style={{ width: '100%', height: '100%' }}
    />
  </div>
</div>
```

### FlexGrid Column Rule (Wijmo)

When using Wijmo `FlexGrid` with explicit `FlexGridColumn` definitions, **always add a final spacer column** to fill remaining width.

- **Purpose**: Prevent trailing empty space and keep header/body aligned when the container is wider than the sum of columns.
- **Rule**: The last column must be:
  - `header=""`
  - `binding=""` (no binding)
  - `width="*"` (fill remaining space)
  - `allowSorting={false}`
  - `isReadOnly={true}`

**Example**:

```tsx
<FlexGrid itemsSource={data} style={{ width: '100%', height: '100%' }}>
  <FlexGridColumn binding="Name" header="Name" width={200} />
  <FlexGridColumn binding="State" header="State" width={120} />

  {/* Spacer column to fill remaining width (must be last) */}
  <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
</FlexGrid>
```

## Important Notes

1. **Always use theme tokens**: Never hardcode colors. Use `theme.mainContentSection`, `theme.title`, etc.

2. **Consistent spacing**: Follow the spacing standards above for consistency.

3. **Responsive considerations**: The current layout is optimized for desktop. Mobile responsiveness may need additional work.

4. **Height constraints**: Use `h-[200px]` as minimum height, then `flex-auto` for remaining space.

5. **Overflow handling**: Use `overflow-hidden` on containers, `overflow-auto` on scrollable content areas.

## Related Documents

- See `ButtonStandards.md` for button styling
- See `FormStandards.md` for form field standards
- See `ThemeUsageStandards.md` for theme system usage
- See `../layout/TailwindFlexBoxRemainSpace.md` for flexbox patterns
