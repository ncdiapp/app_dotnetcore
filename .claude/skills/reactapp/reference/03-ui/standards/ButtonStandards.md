# Button Standards

## Overview

This document defines the standard button styles and patterns used throughout the React application. All buttons should follow these standards to ensure visual consistency.

## Theme-Based Buttons

### Primary Button (Default)

**Standard Pattern**:
```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
  Button Text
</button>
```

**Key Classes**:
- `px-3 py-1.5` - Padding: 12px horizontal, 6px vertical
- `text-sm` - Text size: 14px
- `rounded-[4px]` - Border radius: 4px
- `theme.button_default` - Theme colors (background, text, border, hover states)
- `flex items-center gap-1` - Flex layout for icon + text

**Theme Token**: `theme.button_default` includes:
- Background color
- Text color
- Border color
- Hover states (background, text, border)

### Secondary Button

**Standard Pattern**:
```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} flex items-center gap-1`}>
  Button Text
</button>
```

**Key Classes**: Same as primary, but use `theme.button_secondary`

## Button Sizes

### Small Icon Button

**Standard Pattern**:
```tsx
<button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`} title="Tooltip">
  <i className="fa-solid fa-icon-name"></i>
</button>
```

**Key Classes**:
- `w-8 h-6` - Fixed size: 32px × 24px
- `text-xs` - Text size: 12px (for icons)
- `rounded-[4px]` - Border radius: 4px

**Common Icons**:
- Refresh: `fa-solid fa-rotate` or `fa fa-refresh`
- Save: SVG or `fa-solid fa-save`
- Add: `fa-solid fa-plus`
- Edit: `fa-solid fa-pen-to-square` or `fa fa-edit`
- Delete: `fa-solid fa-trash`

### Standard Button

**Pattern**:
```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
  Button Text
</button>
```

**Dimensions**: Approximately 48-56px height (py-1.5 = 6px top + 6px bottom + text height)

### Large Button

**Pattern**:
```tsx
<button className={`px-4 py-2 text-base rounded-[4px] ${theme.button_default}`}>
  Button Text
</button>
```

**Dimensions**: Approximately 56-64px height

## Button States

### Disabled State

```tsx
<button 
  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed`}
  disabled={isDisabled}
>
  Button Text
</button>
```

**Key Classes**:
- `disabled:opacity-50` - Reduce opacity when disabled
- `disabled:cursor-not-allowed` - Show not-allowed cursor

### Active/Pressed State

```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} active:scale-95`}>
  Button Text
</button>
```

**Key Classes**:
- `active:scale-95` - Slight scale down on click

### Hover State

Handled automatically by `theme.button_default` which includes hover states.

## Button with Icons

### Icon Before Text

```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
  <i className="fa-solid fa-save"></i>
  Save
</button>
```

**Key Classes**:
- `flex items-center` - Horizontal flex layout
- `gap-1` - 4px gap between icon and text

### Icon Only

```tsx
<button className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs flex items-center justify-center`} title="Save">
  <i className="fa-solid fa-save"></i>
</button>
```

**Key Classes**:
- `flex items-center justify-center` - Center icon

## Button Groups

### Horizontal Button Group

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

**Key Classes**:
- `flex items-center` - Horizontal layout
- `space-x-2` - 8px horizontal gap between buttons

### Vertical Button Group

```tsx
<div className="flex flex-col space-y-2">
  <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
    Button 1
  </button>
  <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
    Button 2
  </button>
</div>
```

## Color Variations (Non-Standard)

**Note**: These are examples from existing code. Prefer theme-based buttons when possible.

### Blue Button (Refresh)

```tsx
<button className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500" title="Refresh">
  <i className="fa fa-refresh"></i>
</button>
```

### Orange Button (Save)

```tsx
<button className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500" title="Save">
  <i className="fa fa-save"></i>
</button>
```

### Green Button (Add)

```tsx
<button className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600" title="Add">
  <i className="fa fa-plus"></i>
</button>
```

**Recommendation**: Migrate these to use theme tokens when possible.

## Common Button Patterns

### Header Toolbar Buttons

```tsx
<div className="flex items-center space-x-2">
  <button
    onClick={handleRefresh}
    className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
    title="Refresh"
  >
    <i className="fa fa-refresh"></i>
  </button>
  <button
    onClick={handleSave}
    className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
    title="Save"
  >
    <i className="fa fa-save"></i>
  </button>
</div>
```

### Form Action Buttons

```tsx
<div className="flex items-center space-x-2">
  <button
    onClick={handleCancel}
    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
  >
    Cancel
  </button>
  <button
    onClick={handleSave}
    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
  >
    Save
  </button>
</div>
```

### Context Menu Buttons

```tsx
<button
  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
  onClick={handleAction}
>
  <i className="fa fa-edit mr-2"></i>
  Edit
</button>
```

**Key Classes**:
- `w-full text-left` - Full width, left-aligned text
- `px-4 py-2` - Padding: 16px horizontal, 8px vertical
- `text-xs` - Text size: 12px
- `theme.contextMenu` - Context menu theme colors

## Button Best Practices

1. **Always use theme tokens**: Prefer `theme.button_default` or `theme.button_secondary` over hardcoded colors.

2. **Consistent sizing**: Use standard sizes (w-8 h-6 for icon buttons, px-3 py-1.5 for text buttons).

3. **Accessibility**: Always include `title` attribute for icon-only buttons.

4. **Icon consistency**: Use Font Awesome 6 with `fa-solid` prefix (see Font Awesome conversion guide).

5. **Hover states**: Theme tokens include hover states automatically. Don't add custom hover classes unless necessary.

6. **Disabled state**: Always handle disabled state with `disabled:opacity-50 disabled:cursor-not-allowed`.

7. **Loading state**: Consider showing loading indicator or disabling button during async operations.

## Related Documents

- See `ThemeUsageStandards.md` for theme system usage
- See `PageLayoutStandards.md` for button placement in layouts
- Use Font Awesome 6 with `fa-solid` prefix (e.g. `fa-solid fa-floppy-disk`, `fa-solid fa-rotate`). See ThemeUsageStandards for theme-based styling.
