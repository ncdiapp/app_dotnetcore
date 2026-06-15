# Tailwind Flexbox Remaining Space Pattern

- **CRITICAL**: Never use `flex-1` in Tailwind CSS. It does not work well in this codebase. Instead, use the following patterns based on the flex direction:

## Horizontal Remaining Space

**When parent is `flex` without `flex-col`**:
- Use: `w-1 flex-auto`
- This makes the element take up the remaining horizontal space in a row layout.
- Example: `<div className="flex-auto w-1 overflow-hidden">...</div>`

## Vertical Remaining Space

**When parent is `flex flex-col`**:
- Use: `h-1 flex-auto`
- This makes the element take up the remaining vertical space in a column layout.
- Example: `<div className="flex-auto h-1 overflow-hidden">...</div>`

## Determining the Correct Pattern

- Check the parent container's className:
  - If parent has `flex flex-col` or `flex-col`: use `h-1 flex-auto`
  - If parent has `flex` (without `flex-col`): use `w-1 flex-auto`
- When in doubt, check the layout direction:
  - Row layout (horizontal): `w-1 flex-auto`
  - Column layout (vertical): `h-1 flex-auto`

## Why This Pattern?

The `flex-1` utility in Tailwind can cause layout issues in complex nested flex containers. Using `flex-auto` with explicit width/height constraints (`w-1` or `h-1`) provides more predictable behavior and better control over remaining space distribution.

## References

See `src/components/transaction/metaDataViewDesign.tsx` for examples of both patterns used throughout the component.
