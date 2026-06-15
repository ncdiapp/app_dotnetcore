# React App Conventions

## Debug logging

Use **`appHelper.debugLog()`** instead of `console.log()` for debug output. It can be toggled globally (e.g. via `window.__DEBUG_ENABLED__` or `appHelper.setDebugEnabled()`).

```typescript
import appHelper from '../../helper/appHelper';

appHelper.debugLog('message', { data });
```

- Do not use `console.log()` for debug logs in the React app.

---

## Enum values

Use the **`useEnumValues('EmAppEnumName')`** hook for application enums (from `AppEnums.cs`).

```tsx
import { useEnumValues } from '../../hooks/useEnumDictionary';

const emAppMyEnum = useEnumValues('EmAppMyEnum');
if (emAppMyEnum?.Select !== undefined) {
  // use emAppMyEnum.Select, etc.
}
```

---

## JSON display (textarea / config)

Use **`JSON.stringify(value, null, 2)`** for pretty-printed JSON in UI (memo, config fields).

---

## Imports

- Do not use `.tsx` / `.ts` extension in imports (e.g. `from './MyComponent'` not `from './MyComponent.tsx'`).

---

## General

- Unused variables/parameters: prefix with `_` (ESLint allows this).
- Services: `*svc.ts` naming under `src/webapi/`.
- Feature-based organization under `src/components/` and `src/redux/features/`.
