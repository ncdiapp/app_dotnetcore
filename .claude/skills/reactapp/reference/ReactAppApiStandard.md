# React App API / WebAPI Service Standard

## Overview

This document defines how to implement and call backend APIs from the React app. Services live in `PlmApplication/AppReact/src/webapi/`. The backend is ASP.NET Web API under `PlmApplication/Server/WebApi/`.

## Project Context

- **React app**: `PlmApplication/AppReact/`
- **WebAPI services**: `PlmApplication/AppReact/src/webapi/*svc.ts`
- **Backend API**: `PlmApplication/Server/WebApi/` (same API for the React app)
- **Base URL**: `/appai` (see `src/webapi/endpoints.ts`)

---

## Service Rules

### 1. Method names

Keep PascalCase for API method names (e.g. `RetrieveAppProjectOrWorkFlows`, `SaveProjectSettingExDto`). Match the backend controller action names.

### 2. Query parameters in URL

- Include **all** query parameters in the URL.
- Use `${param || ''}` so `null`/`undefined` become empty string.

```typescript
const response = await fetch(
  `${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType=${projectOrWorflowType || ''}&isPredefined=${isPredefined || ''}&isHierarchy=${isHierarchy || ''}`,
  { headers: getHeaders() }
);
```

### 3. GET requests

- No `method` (defaults to GET).
- Query params in URL only; no body.

### 4. POST / PUT with body

- Use `method: 'POST'` or `method: 'PUT'`.
- Send body as JSON: `body: JSON.stringify(data)`.
- Use `getHeaders()` for headers.

```typescript
const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectSettingExDto`, {
  method: 'POST',
  headers: getHeaders(),
  body: JSON.stringify(data),
});
```

### 5. Headers

Always use:

```typescript
import { getHeaders } from '../helper/apiServiceHelper';
// ...
headers: getHeaders()
```

### 6. Error handling

Check `response.ok` and throw a clear error:

```typescript
if (!response.ok) throw new Error('Failed to [action description]');
return response.json();
```

### 7. Return type

Methods that return API data should return `Promise<any>` and `return response.json()`.

### 8. Parameter types

- Use `any` when a parameter can be multiple types (e.g. `string | number | null`).
- Use union types only for two options (e.g. `string | number`).
- Request/response bodies: `any` is acceptable.

### 9. Endpoint URL pattern

```text
`${endpoints.BASE_URL}/webapi/[Controller]/[Action]`
```

Use the exact controller and action names (case-sensitive).

### 10. Service module structure

```typescript
import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class MyService {
  async GetSomething(id: any): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/MyController/GetSomething?id=${id || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to get');
    return response.json();
  }
}

export const myService = new MyService();
```

---

## Checklist

- [ ] Method name PascalCase, matches backend action
- [ ] All query parameters in URL; use `${param || ''}` for null/undefined
- [ ] POST/PUT use `body: JSON.stringify(data)` and correct `method`
- [ ] `headers: getHeaders()`
- [ ] `if (!response.ok) throw new Error(...)` then `return response.json()`
- [ ] Return type `Promise<any>` where appropriate
