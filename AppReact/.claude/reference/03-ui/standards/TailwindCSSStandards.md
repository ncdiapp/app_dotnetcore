# Tailwind CSS Standards

## Overview

This document defines the Tailwind CSS usage standards and patterns for the React application. All components should follow these standards to ensure consistency and maintainability.

## Critical Rules

### ⚠️ Never Use `flex-1`

**CRITICAL**: Never use `flex-1` in Tailwind CSS. It does not work well in this codebase.

**Use Instead**:
- **Horizontal remaining space**: `w-1 flex-auto`
- **Vertical remaining space**: `h-1 flex-auto`

See `../layout/TailwindFlexBoxRemainSpace.md` for detailed patterns.

## Common Tailwind Patterns

### Layout Classes

#### Container Patterns

**Full Width/Height Container**:
```tsx
<div className="w-full h-full">
```

**Flex Container (Row)**:
```tsx
<div className="flex items-center">
```

**Flex Container (Column)**:
```tsx
<div className="flex flex-col">
```

**Flex with Space Between**:
```tsx
<div className="flex items-center justify-between">
```

**Flex with Gap**:
```tsx
<div className="flex items-center space-x-2">  // Horizontal gap
<div className="flex flex-col space-y-2">     // Vertical gap
<div className="flex flex-wrap gap-10">        // Both directions
```

### Spacing Standards

#### Padding

**Standard Padding Patterns**:
- `px-3 py-2` - Header toolbar (12px horizontal, 8px vertical)
- `px-5 py-5` - Form content area (20px all around)
- `px-4` - Grid/table content (16px horizontal)
- `px-2 py-1` - Compact padding (8px horizontal, 4px vertical)
- `px-3 py-1.5` - Standard button padding (12px horizontal, 6px vertical)

**Common Padding Values**:
- `px-2` = 8px horizontal
- `px-3` = 12px horizontal
- `px-4` = 16px horizontal
- `px-5` = 20px horizontal
- `py-1` = 4px vertical
- `py-2` = 8px vertical
- `py-1.5` = 6px vertical

#### Margins

**Standard Margin Patterns**:
- `mb-1` - Between header and content (4px)
- `mb-2` - Between form fields (8px)
- `mb-4` - Between sections (16px)
- `mb-6` - Large section spacing (24px)
- `mr-2` - Between label and input (8px)
- `mt-1` - Error message spacing (4px)

**Common Margin Values**:
- `mb-1` = 4px bottom margin
- `mb-2` = 8px bottom margin
- `mb-4` = 16px bottom margin
- `mr-2` = 8px right margin
- `space-x-2` = 8px horizontal gap
- `space-y-2` = 8px vertical gap
- `gap-10` = 40px gap (both directions)

### Size Standards

#### Width

**Common Width Patterns**:
- `w-full` - Full width (100%)
- `w-32` - Fixed width: 128px (for labels)
- `w-1/2` - 50% width (for columns)
- `w-8` - Fixed width: 32px (for icon buttons)
- `w-[300px]` - Custom width: 300px
- `w-1` - Minimum width (used with flex-auto)

**Responsive Widths**:
- `w-full lg:w-[400px]` - Full width on mobile, 400px on large screens

#### Height

**Common Height Patterns**:
- `h-full` - Full height (100%)
- `h-7` - Standard input height: 28px
- `h-8` - Icon button height: 32px
- `h-6` - Small button height: 24px
- `h-16` - Header height: 64px
- `h-[200px]` - Minimum content height: 200px
- `h-1` - Minimum height (used with flex-auto)

### Border Radius

**Standard Border Radius**:
- `rounded-[4px]` - Standard border radius (4px) - **Most Common**
- `rounded` - Default border radius (4px)
- `rounded-t-md rounded-b-md` - Rounded top and bottom only
- `rounded-full` - Full circle (for search inputs, avatars)

**Usage**:
- Buttons: `rounded-[4px]`
- Inputs: `rounded` or `rounded-[4px]`
- Page containers: `rounded-t-md rounded-b-md`
- Search inputs: `rounded-full`

### Text Sizes

**Standard Text Sizes**:
- `text-xs` - 12px (labels, small buttons, compact text)
- `text-sm` - 14px (standard form inputs, buttons)
- `text-md` - 16px (page titles, headings)
- `text-base` - 16px (default text)
- `text-lg` - 18px (large headings)
- `text-xl` - 20px (extra large headings)
- `text-2xl` - 24px (close button icons)

**Usage**:
- Labels: `text-xs`
- Inputs: `text-xs` or `text-sm`
- Buttons: `text-xs` (icon buttons) or `text-sm` (text buttons)
- Page titles: `text-md font-semibold`
- Headings: `text-lg` or `text-xl`

### Font Weights

**Standard Font Weights**:
- `font-semibold` - Page titles, headings (600)
- `font-medium` - Subheadings (500)
- `font-bold` - Strong emphasis (700)

**Usage**:
- Page titles: `text-md font-semibold`
- Section headings: `text-sm font-semibold`
- Labels: No weight (default)

## Flexbox Patterns

### Horizontal Layouts

**Row with Items Centered**:
```tsx
<div className="flex items-center">
```

**Row with Space Between**:
```tsx
<div className="flex items-center justify-between">
```

**Row with Gap**:
```tsx
<div className="flex items-center space-x-2">
```

**Row with Remaining Space**:
```tsx
<div className="flex">
  <div className="w-[300px] flex-shrink-0">Fixed</div>
  <div className="flex-auto w-1">Remaining</div>
</div>
```

### Vertical Layouts

**Column Layout**:
```tsx
<div className="flex flex-col">
```

**Column with Gap**:
```tsx
<div className="flex flex-col space-y-2">
```

**Column with Remaining Space**:
```tsx
<div className="flex flex-col">
  <div className="h-[200px] flex-shrink-0">Fixed</div>
  <div className="flex-auto h-1">Remaining</div>
</div>
```

### Flex Wrap

**Wrap Layout with Gap**:
```tsx
<div className="flex flex-wrap gap-10">
```

**Common Use**: Form fields in two-column layout

## Overflow Patterns

**Hidden Overflow**:
```tsx
<div className="overflow-hidden">
```

**Scrollable Content**:
```tsx
<div className="overflow-auto">
```

**Vertical Scroll Only**:
```tsx
<div className="overflow-y-auto">
```

**Horizontal Scroll Only**:
```tsx
<div className="overflow-x-auto">
```

**Usage**:
- Page containers: `overflow-hidden`
- Content areas: `overflow-auto` or `overflow-y-auto`
- Tables/Grids: `overflow-auto`

## Border Patterns

**Standard Border**:
```tsx
<div className="border">
```

**Border with Direction**:
```tsx
<div className="border-b">  // Bottom border
<div className="border-r">  // Right border
<div className="border-t">  // Top border
<div className="border-l">  // Left border
```

**Border with Theme**:
```tsx
<div className={`border ${theme.inputBox}`}>
```

## Common Component Patterns

### Button Pattern

```tsx
<button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
  Button Text
</button>
```

### Input Pattern

```tsx
<input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`} />
```

### Label Pattern

```tsx
<label className={`w-32 text-xs ${theme.label} mr-2`}>
  Label Text
</label>
```

### Page Container Pattern

```tsx
<div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
```

### Header Toolbar Pattern

```tsx
<div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
```

### Content Area Pattern

```tsx
<div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
```

## Combining Tailwind with Theme

### Theme Classes First

**Good**:
```tsx
<div className={`px-3 py-2 ${theme.mainContentSection} border-b`}>
```

**Bad**:
```tsx
<div className={`px-3 py-2 border-b ${theme.mainContentSection}`}>
```

### Conditional Classes

```tsx
<div className={`px-3 py-2 ${theme.mainContentSection} ${isActive ? 'ring-2 ring-blue-500' : ''}`}>
```

### Multiple Theme Classes

```tsx
<button className={`px-3 py-1 ${theme.button_default} ${theme.title}`}>
```

## Responsive Patterns

### Mobile-First Approach

```tsx
// Base (mobile)
<div className="w-full">

// Large screens
<div className="w-full lg:w-[400px]">

// Hide on mobile, show on large
<div className="hidden lg:flex">
```

### Common Breakpoints

- `sm:` - 640px and up
- `md:` - 768px and up
- `lg:` - 1024px and up
- `xl:` - 1280px and up

## State Classes

### Hover States

**With Theme**:
```tsx
<button className={theme.button_default}>
  {/* Hover included in theme */}
</button>
```

**Custom Hover**:
```tsx
<button className="bg-blue-500 hover:bg-blue-600">
```

### Focus States

**Standard Input Focus**:
```tsx
<input className={`... focus:outline-none`} />
```

**Custom Focus**:
```tsx
<input className={`... focus:ring-2 focus:ring-blue-500`} />
```

### Active States

**Button Active**:
```tsx
<button className={`... active:scale-95`}>
```

### Disabled States

**Standard Disabled**:
```tsx
<button className={`... disabled:opacity-50 disabled:cursor-not-allowed`} disabled>
```

## Best Practices

1. **Use theme classes**: Prefer `theme.button_default` over hardcoded colors
2. **Consistent spacing**: Follow spacing standards (px-3 py-2, mb-1, etc.)
3. **Standard sizes**: Use standard heights (h-7 for inputs, h-8 h-6 for buttons)
4. **Border radius**: Use `rounded-[4px]` for consistency
5. **Text sizes**: Use `text-xs` for labels, `text-sm` for inputs/buttons
6. **Flexbox**: Never use `flex-1`, use `w-1 flex-auto` or `h-1 flex-auto`
7. **Overflow**: Use `overflow-hidden` on containers, `overflow-auto` on content
8. **Responsive**: Use mobile-first approach with breakpoint prefixes

## Common Mistakes to Avoid

### ❌ Don't Use flex-1

```tsx
// ❌ Bad
<div className="flex-1">

// ✅ Good
<div className="flex-auto w-1">  // Horizontal
<div className="flex-auto h-1">  // Vertical
```

### ❌ Don't Hardcode Colors

```tsx
// ❌ Bad
<div className="bg-white text-black">

// ✅ Good
<div className={theme.mainContentSection}>
```

### ❌ Don't Mix Inconsistent Spacing

```tsx
// ❌ Bad
<div className="px-4 py-3 mb-5">

// ✅ Good (follow standards)
<div className="px-3 py-2 mb-1">
```

### ❌ Don't Use Inconsistent Border Radius

```tsx
// ❌ Bad
<button className="rounded-lg">

// ✅ Good
<button className="rounded-[4px]">
```

## Quick Reference

| Pattern | Classes |
|---------|---------|
| Page container | `w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden` |
| Header toolbar | `flex items-center justify-between px-3 py-2 mb-1` |
| Content area | `w-full h-[200px] flex-auto overflow-hidden` |
| Standard button | `px-3 py-1.5 text-sm rounded-[4px]` |
| Icon button | `w-8 h-6 rounded-[4px] text-xs` |
| Standard input | `flex-auto w-32 h-7 px-2 text-xs border` |
| Label | `w-32 text-xs mr-2` |
| Horizontal remaining space | `flex-auto w-1` |
| Vertical remaining space | `flex-auto h-1` |

## Related Documents

- See `../layout/TailwindFlexBoxRemainSpace.md` for detailed flexbox patterns
- See `PageLayoutStandards.md` for page layout patterns
- See `ButtonStandards.md` for button patterns
- See `FormStandards.md` for form patterns
- See `ThemeUsageStandards.md` for theme system usage
