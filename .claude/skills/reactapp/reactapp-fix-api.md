# reactapp-fix-api

Use when **adding or fixing API calls** in the React app: adding a new WebAPI service method, fixing API errors, or adding code in components to call the backend. Covers service module rules and how to call APIs from components.

**Full reference**: `.claude/react-app/reference/ReactAppApiStandard.md`

## Rules (summary)

- **Method names**: PascalCase, match backend controller action names.
- **Query parameters**: Always in URL; use `${param || ''}` for null/undefined.
- **GET**: No body; params in URL only.
- **POST/PUT**: `method: 'POST'` or `'PUT'`, `body: JSON.stringify(data)`, `headers: getHeaders()`.
- **Headers**: Always `getHeaders()` from `../helper/apiServiceHelper`.
- **Errors**: `if (!response.ok) throw new Error('...'); return response.json();`
- **Base URL**: `endpoints.BASE_URL` (e.g. `/appai`), path `/webapi/[Controller]/[Action]`.
- **Parameter types**: Use `any` when a param can be multiple types; return `Promise<any>` for API methods.
- **Services**: Live in `PlmApplication/AppReact/src/webapi/*svc.ts`; naming `*svc.ts`.

See ReactAppApiStandard.md for full patterns and checklist.
