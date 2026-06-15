# Theme Usage Standards

## Overview

This document defines how to use the dynamic theme system in the React application. All components should use theme tokens instead of hardcoded colors to support theme switching.

## Theme Hook

### Basic Usage

```tsx
import { useTheme } from '../../redux/hooks/useTheme';

const MyComponent: React.FC = () => {
  const { t, tw, theme } = useTheme();
  
  return (
    <div className={theme.mainContentSection}>
      {/* Component content */}
    </div>
  );
};
```

### Hook Return Values

- `theme`: Object containing theme class strings (e.g., `theme.button_default`, `theme.mainContentSection`)
- `t`: Function to get theme parameter classes (e.g., `t('bg_default')`)
- `tw`: Function to convert theme values to Tailwind classes
- `param`: Direct access to theme parameter values

## Theme Classes (theme object)

### Available Theme Classes

Use these classes from the `theme` object:

#### Layout Classes
- `theme.default` - Default background, text, and border
- `theme.mainContentSection` - Main content area styling
- `theme.mainHeader` - Header/toolbar styling
- `theme.sideBar` - Sidebar styling

#### Component Classes
- `theme.button_default` - Primary button styling
- `theme.button_secondary` - Secondary button styling
- `theme.inputBox` - Input field styling
- `theme.label` - Label text styling
- `theme.title` - Title/heading text styling
- `theme.tab` - Tab styling
- `theme.tab_active` - Active tab styling
- `theme.modalHeader` - Modal header styling
- `theme.contextMenu` - Context menu item styling

#### Menu Classes
- `theme.sideBar_menu` - Sidebar menu item styling
- `theme.sideBar_menu_active` - Active sidebar menu item
- `theme.menu_default` - Default menu styling
- `theme.menu_secondary` - Secondary menu styling

### Usage Examples

```tsx
// Page container
<div className={theme.mainContentSection}>
  {/* Content */}
</div>

// Button
<button className={`px-3 py-1 ${theme.button_default}`}>
  Click Me
</button>

// Input
<input className={`px-2 py-1 border ${theme.inputBox}`} />

// Label
<label className={`text-xs ${theme.label}`}>Field Label</label>

// Title
<div className={`text-md font-semibold ${theme.title}`}>Page Title</div>
```

## Theme Parameters (t function)

### Using t() Function

The `t()` function converts theme parameter keys to Tailwind classes:

```tsx
const { t } = useTheme();

// Background color
<div className={t('bg_default')}>

// Text color
<div className={t('text_default')}>

// Border color
<div className={t('border_default')}>

// Hover states
<div className={t('bg_default_hover')}>
```

### Common Theme Parameters

#### Background Colors
- `bg_default` - Default background
- `bg_header` - Header background
- `bg_mainContentSection` - Main content background
- `bg_sidebar` - Sidebar background
- `bg_button_default` - Primary button background
- `bg_button_secondary` - Secondary button background
- `bg_input_box` - Input box background
- `bg_modalHeader` - Modal header background
- `bg_tab` - Tab background
- `bg_tab_active` - Active tab background

#### Text Colors
- `text_default` - Default text
- `text_header` - Header text
- `text_mainContentSection` - Main content text
- `text_title` - Title text
- `text_button_default` - Primary button text
- `text_button_secondary` - Secondary button text

#### Border Colors
- `border_default` - Default border
- `border_header` - Header border
- `border_mainContentSection` - Main content border
- `border_button_default` - Primary button border
- `border_input_box` - Input box border

#### Hover States
- `bg_default_hover` - Default background hover
- `text_default_hover` - Default text hover
- `bg_button_default_hover` - Primary button hover background
- `text_button_default_hover` - Primary button hover text

#### Active States
- `bg_default_active` - Default background active
- `text_default_active` - Default text active

### Usage Examples

```tsx
const { t } = useTheme();

// Custom background
<div className={t('bg_default')}>

// Custom text color
<span className={t('text_title')}>

// Hover state
<button className={`px-3 py-1 ${t('bg_button_default')} ${t('bg_button_default_hover')}`}>

// Logo filter
<img src="logo.png" className={`h-8 ${t('logo_filter')}`} />
```

## Theme Class Combinations

### Combining Theme Classes with Tailwind

```tsx
// Theme class + Tailwind utilities
<div className={`px-3 py-2 ${theme.mainContentSection} border-b`}>

// Multiple theme classes
<button className={`px-3 py-1 ${theme.button_default} ${theme.title}`}>

// Theme class + conditional classes
<div className={`${theme.mainContentSection} ${isActive ? 'ring-2 ring-blue-500' : ''}`}>
```

### Common Patterns

```tsx
// Header toolbar
<div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>

// Page title
<div className={`text-md font-semibold ${theme.title}`}>

// Form field
<div className="flex items-center py-1">
  <label className={`w-32 text-xs ${theme.label} mr-2`}>Label</label>
  <input className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`} />
</div>

// Button
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
  Button
</button>
```

## Theme Switching

### Programmatic Theme Change

```tsx
import { useDispatch } from 'react-redux';
import { setThemeById } from '../../redux/features/ui/theme/themeSlice';

const MyComponent: React.FC = () => {
  const dispatch = useDispatch();
  
  const handleThemeChange = (themeId: string) => {
    dispatch(setThemeById(themeId));
  };
  
  return (
    <button onClick={() => handleThemeChange('dark')}>
      Switch to Dark Theme
    </button>
  );
};
```

### Available Themes

- `'light'` - Light theme (default)
- `'dark'` - Dark theme
- Custom themes defined by user

## Best Practices

1. **Always use theme tokens**: Never hardcode colors. Use `theme.*` classes or `t()` function.

2. **Prefer theme classes**: Use `theme.button_default` instead of `t('bg_button_default')` when a theme class exists.

3. **Combine with Tailwind**: Theme classes work alongside Tailwind utilities. Combine them as needed.

4. **Consistent usage**: Use the same theme tokens across similar components for consistency.

5. **Test theme switching**: Always test components with different themes to ensure proper styling.

6. **Fallback values**: Theme tokens have default values, but ensure your component works with all themes.

7. **Avoid inline styles**: Prefer theme classes over inline styles for colors.

8. **Document custom usage**: If you need custom theme parameters, document them and ensure they're added to the theme system.

## Common Mistakes to Avoid

### ❌ Don't Hardcode Colors

```tsx
// ❌ Bad
<div className="bg-white text-black">

// ✅ Good
<div className={theme.mainContentSection}>
```

### ❌ Don't Use Hardcoded Hover States

```tsx
// ❌ Bad
<button className="bg-blue-500 hover:bg-blue-600">

// ✅ Good
<button className={theme.button_default}>
```

### ❌ Don't Mix Theme Systems

```tsx
// ❌ Bad - mixing theme classes with hardcoded colors
<div className={`${theme.mainContentSection} bg-white`}>

// ✅ Good - use theme classes only
<div className={theme.mainContentSection}>
```

## Theme Token Reference

### Complete List of Theme Classes

See `src/redux/features/ui/theme/types.ts` for the complete `ThemeClasses` interface:

- `default`
- `sideBar`
- `sideBar_menu`
- `sideBar_menu_active`
- `mainHeader`
- `mainContentSection`
- `mainContent` (alias)
- `tab`
- `tab_active`
- `modalHeader`
- `button_default`
- `button_secondary`
- `menu_default`
- `menu_secondary`
- `contextMenu`
- `title`
- `label`
- `inputBox`

### Complete List of Theme Parameters

See `src/redux/features/ui/theme/types.ts` for the complete `BaseTheme` interface. All parameters follow the pattern:
- `bg_*` - Background colors
- `text_*` - Text colors
- `border_*` - Border colors
- `*_hover` - Hover states
- `*_active` - Active states

## Related Documents

- See `PageLayoutStandards.md` for layout theme usage
- See `ButtonStandards.md` for button theme usage
- See `FormStandards.md` for form theme usage
- See `src/redux/features/ui/theme/types.ts` for type definitions
- See `src/helper/themeHelper.ts` for theme helper functions
