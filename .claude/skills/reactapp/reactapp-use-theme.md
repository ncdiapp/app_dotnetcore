# reactapp-use-theme

Use when styling React app UI. All colors and surface styles must come from the **theme system**; never hardcode colors (no `bg-blue-400`, etc.).

**Full reference**: `.claude/react-app/reference/03-ui/standards/ThemeUsageStandards.md`

## Rules (summary)

- **Hook**: `const { theme } = useTheme();` from `../../redux/hooks/useTheme`.
- **Use `theme.*` for**: layout (`theme.mainContentSection`, `theme.mainHeader`), buttons (`theme.button_default`, `theme.button_secondary`), inputs (`theme.inputBox`), labels (`theme.label`), titles (`theme.title`), tabs, modals, context menu (`theme.contextMenu`), sidebar menu.
- **Examples**:
  - Page/section: `className={theme.mainContentSection}`
  - Button: `` className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} ``
  - Input: `` className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`} ``
  - Label: `` className={`w-32 text-xs ${theme.label} mr-2`} ``

Never use raw Tailwind color classes for app chrome; use theme tokens so light/dark and branding stay consistent. See ThemeUsageStandards.md for full list and examples.
