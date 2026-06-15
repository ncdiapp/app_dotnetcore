# Form Standards

## Overview

This document defines the standard form field styles, layouts, and patterns used throughout the React application.

## Form Field Structure

### Standard Form Field Pattern

```tsx
<div className="flex items-center py-1">
  <label className={`w-32 text-xs ${theme.label} mr-2`}>
    Field Label
  </label>
  <input
    type="text"
    value={value}
    onChange={e => handleChange(e.target.value)}
    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
  />
</div>
```

## Form Components

### Label

**Standard Pattern**:
```tsx
<label className={`w-32 text-xs ${theme.label} mr-2`}>
  Field Label
</label>
```

**Key Classes**:
- `w-32` - Fixed width: 128px (for consistent alignment)
- `text-xs` - Text size: 12px
- `theme.label` - Theme text color
- `mr-2` - Margin right: 8px (spacing between label and input)

**Alternative Pattern (Block Layout)**:
```tsx
<label className={`block text-xs mb-1 ${theme.label}`}>
  Field Label
</label>
```

**Key Classes**:
- `block` - Block-level element
- `mb-1` - Margin bottom: 4px

### Text Input

**Standard Pattern**:
```tsx
<input
  type="text"
  autoComplete="off"
  value={value}
  onChange={e => handleChange(e.target.value)}
  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
/>
```

**Key Classes**:
- `flex-auto w-32` - Flexible width with minimum 128px
- `h-7` - Height: 28px
- `px-2` - Padding: 8px horizontal
- `text-xs` - Text size: 12px
- `border` - Border: 1px solid
- `theme.inputBox` - Theme background, text, and border colors
- `focus:outline-none` - Remove default focus outline

**Alternative Pattern (Full Width)**:
```tsx
<input
  type="text"
  value={value}
  onChange={e => handleChange(e.target.value)}
  className={`w-full px-2 py-1 text-sm border rounded ${theme.inputBox} focus:outline-none`}
/>
```

**Key Classes**:
- `w-full` - Full width
- `px-2 py-1` - Padding: 8px horizontal, 4px vertical
- `text-sm` - Text size: 14px
- `rounded` - Border radius: 4px

### Textarea

**Standard Pattern**:
```tsx
<textarea
  value={value}
  onChange={e => handleChange(e.target.value)}
  className={`flex-auto h-1 px-2 py-1 text-sm border rounded ${theme.inputBox} resize-none focus:outline-none`}
  rows={4}
/>
```

**Key Classes**:
- `flex-auto h-1` - Flexible height (see TailwindFlexBoxRemainSpace.md)
- `resize-none` - Disable resize handle
- `rows={4}` - Default rows

### Select/Dropdown (Wijmo ComboBox)

**Standard Pattern**:
```tsx
<ComboBox
  itemsSource={items}
  selectedValue={selectedValue}
  displayMemberPath="Display"
  selectedValuePath="Id"
  isEditable={false}
  placeholder="Select..."
  className={`flex-auto w-32 h-7 ${theme.inputBox}`}
/>
```

**Key Classes**:
- Use Wijmo ComboBox component
- Apply `theme.inputBox` for styling
- See Wijmo ComboBox selection rule in ConverterAngularJsPage.md

### Checkbox

**Standard Pattern**:
```tsx
<div className="flex items-center py-1">
  <input
    type="checkbox"
    checked={isChecked}
    onChange={e => handleChange(e.target.checked)}
    className="w-4 h-4 mr-2"
  />
  <label className={`text-xs ${theme.label}`}>
    Checkbox Label
  </label>
</div>
```

## Form Layouts

### Horizontal Form Layout (Two Columns)

```tsx
<div className="flex flex-wrap gap-10 w-full">
  {/* Left Column */}
  <div className="w-1/2 max-w-[500px] flex-auto">
    {/* Form fields */}
  </div>
  
  {/* Right Column */}
  <div className="w-1/2 max-w-[500px] flex-auto">
    {/* Form fields */}
  </div>
</div>
```

**Key Classes**:
- `flex flex-wrap` - Flexible wrap layout
- `gap-10` - 40px gap between items
- `w-1/2` - 50% width
- `max-w-[500px]` - Maximum width: 500px
- `flex-auto` - Flexible sizing

### Vertical Form Layout

```tsx
<div className="flex flex-col gap-4">
  {/* Form fields stacked vertically */}
</div>
```

**Key Classes**:
- `flex flex-col` - Vertical layout
- `gap-4` - 16px gap between fields

### Inline Form Layout

```tsx
<div className="flex items-center py-1">
  <label className={`w-32 text-xs ${theme.label} mr-2`}>Label</label>
  <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} />
</div>
```

**Key Classes**:
- `flex items-center` - Horizontal layout with vertical centering
- `py-1` - 4px vertical padding (spacing between rows)

## Form Spacing

### Field Spacing

- **Between Fields (Inline)**: `py-1` (4px vertical padding)
- **Between Fields (Block)**: `mb-1` or `mb-2` (4px or 8px margin bottom)
- **Between Columns**: `gap-10` (40px gap in flex-wrap layout)

### Form Container Padding

- **Standard Form**: `px-5 py-5` (20px all around)
- **Compact Form**: `px-3 py-3` (12px all around)

## Form Validation

### API Validation Result (Standard)

When handling save/API responses that return a validation result, use the project standard to display validation messages:

```tsx
if (data?.ValidationResult) {
  showValidationMessages(data.ValidationResult, true);
}
```

- **First argument**: `data.ValidationResult` from the API response.
- **Second argument**: `true` to show messages in the Messages panel (standard).

Use this in save/update handlers after receiving a response; do not hardcode a separate success or error message for validation—rely on the API result and `showValidationMessages` for consistency.

**Example (save handler)**:

```tsx
const data = await someSvc.saveSomething(payload);
if (data?.ValidationResult?.IsValid !== false && data?.Id != null) {
  // success path: update local state, reload, etc.
} else {
  if (data?.ValidationResult) showValidationMessages(data.ValidationResult, true);
  const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage ?? i.LocalizedMessage).filter(Boolean) ?? [];
  showError(msgs.length ? msgs.join('; ') : 'Save failed');
}
```

### Error Display (inline)

```tsx
{error && (
  <div className={`text-xs text-red-600 mt-1`}>
    {error}
  </div>
)}
```

**Key Classes**:
- `text-xs` - Small text
- `text-red-600` - Error color
- `mt-1` - Margin top: 4px

### Required Field Indicator

```tsx
<label className={`w-32 text-xs ${theme.label} mr-2`}>
  Field Label <span className="text-red-600">*</span>
</label>
```

## Form Examples

### Standard Form Page

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

  {/* Form Content */}
  <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
    <div className="h-full w-full overflow-auto px-5 py-5">
      <div className="flex flex-wrap gap-10 w-full">
        {/* Left Column */}
        <div className="w-1/2 max-w-[500px] flex-auto">
          <div className="flex items-center py-1">
            <label className={`w-32 text-xs ${theme.label} mr-2`}>Field 1</label>
            <input
              className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            />
          </div>
          {/* More fields */}
        </div>
        
        {/* Right Column */}
        <div className="w-1/2 max-w-[500px] flex-auto">
          {/* More fields */}
        </div>
      </div>
    </div>
  </div>
</div>
```

### Compact Inline Form

```tsx
<div className="flex flex-col gap-2">
  <div className="flex items-center py-1">
    <label className={`w-32 text-xs ${theme.label} mr-2`}>Name</label>
    <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} />
  </div>
  <div className="flex items-center py-1">
    <label className={`w-32 text-xs ${theme.label} mr-2`}>Email</label>
    <input className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} />
  </div>
</div>
```

## Best Practices

1. **Consistent Label Width**: Use `w-32` (128px) for labels in inline layouts for alignment.

2. **Theme Usage**: Always use `theme.inputBox` and `theme.label` instead of hardcoded colors.

3. **Accessibility**: Include proper `label` elements and `autoComplete` attributes.

4. **Spacing**: Use consistent spacing (`py-1` for inline, `gap-10` for columns).

5. **Input Height**: Standard height is `h-7` (28px) for consistency.

6. **Text Size**: Use `text-xs` (12px) for compact forms, `text-sm` (14px) for standard forms.

7. **Focus States**: Always include `focus:outline-none` and consider custom focus styles.

8. **Disabled State**: Apply disabled styling when fields are disabled:
   ```tsx
   className={`... ${theme.inputBox} disabled:opacity-50 disabled:cursor-not-allowed`}
   disabled={isDisabled}
   ```

## Related Documents

- See `PageLayoutStandards.md` for form page layout
- See `ButtonStandards.md` for form action buttons
- See `ThemeUsageStandards.md` for theme system usage
- See `../02-migration/ConverterAngularJsPage.md` for Wijmo ComboBox rules
