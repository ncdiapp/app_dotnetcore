# Two-Level Dropdown Standards (FIELD / Api Operation style)

## Overview

This document defines the **two-level dropdown** pattern for category → sub-items selection (e.g. Api Operation: integration name → API operations). The submenu must stay open when the user moves the mouse from the first level to the second level.

**Reference implementation**: `src/components/dbmgt/DataSetEditor.tsx` (Api Operation dropdown).

**Principles**:

- **Single wrapper, two siblings** — First level and second level are siblings inside one wrapper; no separate Portal for the submenu; no gap so the mouse never "leaves" between layers.
- **Close only on wrapper leave** — Start the close-delay timer only on the wrapper’s `onMouseLeave`; do not put `onMouseLeave` on first-level rows.
- **First level height independent** — Use `items-start` on the wrapper so the first column is not stretched by the second.

---

## 1. State and refs

| Name | Type | Purpose |
|------|------|---------|
| `showDropdown` | `boolean` | Dropdown open/closed. |
| `dropdownPosition` | `{ top: number; left: number } \| null` | From trigger `getBoundingClientRect()`. |
| `hoveredCategoryId` | `string \| number \| null` | Which first-level item is hovered (drives second-level content). |
| `submenuCloseTimerRef` | `useRef<ReturnType<typeof setTimeout> \| null>(null)` | Delay before clearing `hoveredCategoryId` when mouse leaves wrapper. |

**Constant**: e.g. `SUBMENU_CLOSE_DELAY_MS = 350`.

---

## 2. Trigger and position

- **Trigger**: A `ref` on the trigger element (e.g. fake input + chevron button).
- **On open**:  
  `const rect = triggerRef.current?.getBoundingClientRect();`  
  `if (rect) setDropdownPosition({ top: rect.bottom + 4, left: rect.left });`
- **Render**: Only when `showDropdown && dropdownPosition`.

---

## 3. Dropdown structure (one Portal)

- Use **one** `createPortal(..., document.body)`.
- Wrapper: `fixed z-[9999] flex flex-row items-start overflow-visible`, `theme.mainContentSection`, and `onMouseLeave` that starts the close-delay timer.
- **First level panel**: Sibling div, `min-w-[200px] max-h-96 overflow-y-auto border rounded-l shadow-lg py-1`, `theme.mainContentSection`.
- **Second level panel**: Sibling div when `hoveredCategoryId != null`, `min-w-[240px] max-w-[320px] max-h-96 overflow-y-auto border border-l-0 rounded-r shadow-lg py-1` (visually one block with first level).

### First-level row

- **Do not** add `onMouseLeave` on first-level rows (only `onMouseEnter`).
- `onMouseEnter`: clear `submenuCloseTimerRef.current` if set, then `setHoveredCategoryId(cat.id)`.
- Class: `w-full px-3 py-2 text-left text-sm flex items-center justify-between`, `theme.contextMenu`, hover: `theme.tab_active ?? 'bg-gray-100'`.
- Right arrow: `fa-solid fa-chevron-right text-xs ml-1 shrink-0`.

### Second-level item

- `<button type="button">` with `onClick` that applies selection, then `setShowDropdown(false)`, clear `dropdownPosition` and `hoveredCategoryId`.
- Class: `w-full px-3 py-2 text-left text-sm truncate`, `theme.contextMenu`, `hover:opacity-90`.

### Example (structure only)

```tsx
{showDropdown && dropdownPosition && createPortal(
  <div
    className={`fixed z-[9999] flex flex-row items-start overflow-visible ${theme.mainContentSection}`}
    style={{ top: dropdownPosition.top, left: dropdownPosition.left }}
    onMouseLeave={() => {
      submenuCloseTimerRef.current = setTimeout(() => {
        setHoveredCategoryId(null);
        submenuCloseTimerRef.current = null;
      }, SUBMENU_CLOSE_DELAY_MS);
    }}
  >
    <div className={`min-w-[200px] max-h-96 overflow-y-auto border rounded-l shadow-lg py-1 ${theme.mainContentSection}`}>
      {categories.map((cat) => (
        <div
          key={cat.id}
          className={`... ${theme.contextMenu} ${hoveredCategoryId === cat.id ? (theme.tab_active ?? 'bg-gray-100') : ''}`}
          onMouseEnter={() => {
            if (submenuCloseTimerRef.current) {
              clearTimeout(submenuCloseTimerRef.current);
              submenuCloseTimerRef.current = null;
            }
            setHoveredCategoryId(cat.id);
          }}
        >
          <span className="truncate">{cat.name}</span>
          <i className="fa-solid fa-chevron-right text-xs ml-1 shrink-0" />
        </div>
      ))}
    </div>
    {hoveredCategoryId != null && (
      <div className={`min-w-[240px] max-w-[320px] max-h-96 overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}>
        {getItemsForCategory(hoveredCategoryId).map((item) => (
          <button type="button" className={`w-full px-3 py-2 text-left text-sm truncate ${theme.contextMenu} hover:opacity-90`} onClick={...}>
            {item.label}
          </button>
        ))}
      </div>
    )}
  </div>,
  document.body
)}
```

---

## 4. Closing

- **Outside click**: Document `click` listener when dropdown is open; exclude clicks inside a ref that wraps trigger + dropdown (e.g. `dropdownRef.current?.contains(target)`). On outside click: set `showDropdown` false, clear `dropdownPosition` and `hoveredCategoryId`.
- **Mouse leave**: Only the wrapper’s `onMouseLeave` starts the delay timer to clear `hoveredCategoryId`.
- **Select item**: On second-level button click, apply value, then `setShowDropdown(false)`, clear position and `hoveredCategoryId`.

---

## 5. Checklist

- [ ] One Portal, one wrapper `div` containing first level + second level as siblings (no second Portal for submenu).
- [ ] Wrapper has `flex flex-row items-start` and `onMouseLeave` that starts the close-delay timer; first-level rows have **only** `onMouseEnter` (no `onMouseLeave`).
- [ ] Position from trigger `getBoundingClientRect()`; dropdown rendered with `createPortal(..., document.body)`.
- [ ] First level panel: `rounded-l`; second level: `border-l-0 rounded-r` so they look like one block.
- [ ] Document click listener closes dropdown when clicking outside (using ref that includes trigger + content).
- [ ] Submenu close delay constant (e.g. 350ms); clear timer on `onMouseEnter` of first-level rows and in cleanup.
