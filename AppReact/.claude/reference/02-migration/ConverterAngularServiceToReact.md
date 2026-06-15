# Converting Angular Services to React/TypeScript Services

## Overview

This document provides comprehensive rules and guidelines for converting AngularJS services to React/TypeScript services. Follow these rules when migrating API service methods from Angular to React.

## Location Reference

**AngularJS Services Location**: `C:\DevApp\App\PlmApplication\Scripts1x\Services\`

**React Services Location**: `C:\DevApp\app-react\src\webapi\`

---

## Core Conversion Rules

### 1. Method Name Preservation

**Rule**: Keep the exact method name from Angular (including PascalCase naming convention).

**Angular Example**:
```javascript
RetrieveAppProjectOrWorkFlows: function (projectOrWorflowType, isPredefined, isHierarchy) {
    // ...
}
```

**React/TypeScript Example**:
```typescript
async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any> {
    // ...
}
```

**Important**: Do NOT convert PascalCase to camelCase. Keep the exact Angular method names.

---

### 2. Query Parameters in URL

**Rule**: All query parameters must be included in the URL. If a parameter value is `null` or `undefined`, use an empty string `''` as the parameter value in the URL.

#### Pattern for GET Requests with Query Parameters

**Angular Example**:
```javascript
RetrieveAppProjectOrWorkFlows: function (projectOrWorflowType, isPredefined, isHierarchy) {
    return $http.get(domainAndApplicationpath + '/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType='
        + projectOrWorflowType + '&isPredefined=' + isPredefined + '&isHierarchy=' + isHierarchy, {
        headers: angular.getHttpHeader()
    })
}
```

**React/TypeScript Example**:
```typescript
async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType=${projectOrWorflowType || ''}&isPredefined=${isPredefined || ''}&isHierarchy=${isHierarchy || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app project or workflows');
    return response.json();
}
```

**Key Points**:
- Always include all query parameters in the URL
- Use `${param || ''}` pattern to convert `null`/`undefined` to empty string
- Never omit parameters from the URL, even if they are null

#### Single Parameter Example

**Angular**:
```javascript
RetrieveOneAppProjectOrWorkFlowExDto: function (proejctId) {
    return $http.get(domainAndApplicationpath + '/webapi/ProjectWorkFlow/RetrieveOneAppProjectOrWorkFlowExDto?proejctId=' + proejctId, {
        headers: angular.getHttpHeader()
    })
}
```

**React/TypeScript**:
```typescript
async RetrieveOneAppProjectOrWorkFlowExDto(proejctId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveOneAppProjectOrWorkFlowExDto?proejctId=${proejctId || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project or workflow');
    return response.json();
}
```

#### Multiple Parameters Example

**Angular**:
```javascript
UpdateOneTaskOwnerDeliverPhase: function (taskId, emAppTaskOwnerDeliverPhase) {
    return $http.get(domainAndApplicationpath + '/webapi/ProjectWorkFlow/UpdateOneTaskOwnerDeliverPhase?taskId=' + taskId + '&emAppTaskOwnerDeliverPhase=' + emAppTaskOwnerDeliverPhase, {
        headers: angular.getHttpHeader()
    })
}
```

**React/TypeScript**:
```typescript
async UpdateOneTaskOwnerDeliverPhase(taskId: any, emAppTaskOwnerDeliverPhase: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/UpdateOneTaskOwnerDeliverPhase?taskId=${taskId || ''}&emAppTaskOwnerDeliverPhase=${emAppTaskOwnerDeliverPhase || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to update task owner deliver phase');
    return response.json();
}
```

---

### 3. POST Requests with Body Data

**Rule**: POST requests send data in the request body as JSON. No query parameters needed unless explicitly in the Angular version.

**Angular Example**:
```javascript
SaveProjectSettingExDto: function (data) {
    return $http.post(domainAndApplicationpath + '/webapi/ProjectWorkFlow/SaveProjectSettingExDto', data, {
        headers: angular.getHttpHeader()
    })
}
```

**React/TypeScript Example**:
```typescript
async SaveProjectSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectSettingExDto`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project setting');
    return response.json();
}
```

**Key Points**:
- Use `method: 'POST'`
- Use `body: JSON.stringify(data)` to send data
- No query parameters unless the Angular version has them

---

### 4. HTTP Methods

**Rule**: Match the HTTP method from Angular exactly.

| Angular Method | React/TypeScript Method |
|---------------|------------------------|
| `$http.get()` | `fetch()` with no `method` specified (defaults to GET) |
| `$http.post()` | `fetch()` with `method: 'POST'` |
| `$http.put()` | `fetch()` with `method: 'PUT'` |
| `$http.delete()` | `fetch()` with `method: 'DELETE'` |

**Note**: Angular uses `$http.get()` for DELETE operations in some cases. Check the Angular code to determine the correct method.

---

### 5. Headers

**Rule**: Always use `getHeaders()` from `apiServiceHelper` for all requests.

**Import**:
```typescript
import { getHeaders } from '../helper/apiServiceHelper';
```

**Usage**:
```typescript
headers: getHeaders()
```

---

### 6. Error Handling

**Rule**: Always check `response.ok` and throw descriptive errors.

**Pattern**:
```typescript
if (!response.ok) throw new Error('Failed to [action description]');
```

**Example**:
```typescript
if (!response.ok) throw new Error('Failed to retrieve app project or workflows');
```

---

### 7. Return Types

**Rule**: All methods return `Promise<any>`.

**Pattern**:
```typescript
async methodName(...params): Promise<any> {
    // ...
    return response.json();
}
```

---

### 8. Parameter Types

**Rule**: Use appropriate TypeScript types for parameters. **If a parameter has more than 2 types, use `any` instead.**

**Common Patterns**:
- Simple types: `string`, `number`, `boolean`
- Two types: `string | number`, `string | null`
- **Three or more types: Use `any`** (e.g., `string | number | null` → `any`, `string | number | boolean | null` → `any`)
- Data objects: `any`

**Examples**:
```typescript
// Single ID parameter (3 types - use any)
async method(id: any): Promise<any>

// Multiple parameters (3+ types - use any)
async method(param1: any, param2: any): Promise<any>

// Two types only - can use union
async method(id: string | number): Promise<any>

// Data object
async method(data: any): Promise<any>
```

**Important**: When a parameter can be `string | number | null` (3 types) or `string | number | boolean | null` (4 types), use `any` instead of the union type.

---

### 9. Endpoint URLs

**Rule**: Use `endpoints.BASE_URL` and match the exact endpoint path from Angular.

**Pattern**:
```typescript
`${endpoints.BASE_URL}/webapi/[Controller]/[Action]`
```

**Example**:
```typescript
`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows`
```

**Important**: 
- Match the exact case from Angular (e.g., `ProjectWorkFlow` not `ProjectWorkflow`)
- Include the full path including `/webapi/`

---

### 10. Service Class Structure

**Rule**: Follow the standard service class pattern.

**Template**:
```typescript
import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class ServiceName {
    async MethodName(param1: type, param2: type): Promise<any> {
        const response = await fetch(`${endpoints.BASE_URL}/webapi/Controller/Action?param1=${param1 || ''}&param2=${param2 || ''}`, {
            headers: getHeaders()
        });
        if (!response.ok) throw new Error('Failed to [action]');
        return response.json();
    }
}

export const serviceName = new ServiceName();
```

---

## Conversion Checklist

When converting an Angular service method, verify:

- [ ] Method name matches Angular exactly (PascalCase preserved)
- [ ] All query parameters are in the URL
- [ ] Null/undefined values use empty string `''` in URL
- [ ] HTTP method matches Angular (`GET`, `POST`, `PUT`, `DELETE`)
- [ ] POST/PUT requests use `body: JSON.stringify(data)`
- [ ] Headers use `getHeaders()`
- [ ] Error handling with descriptive messages
- [ ] Return type is `Promise<any>`
- [ ] Parameter types are appropriate (use `any` for 3+ types, union types for 2 types)
- [ ] Endpoint URL matches Angular exactly
- [ ] Import statements are correct

---

## Examples

### Example 1: GET Request with Multiple Query Parameters

**Angular**:
```javascript
RetrieveAppProjectOrWorkFlows: function (projectOrWorflowType, isPredefined, isHierarchy) {
    return $http.get(domainAndApplicationpath + '/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType='
        + projectOrWorflowType + '&isPredefined=' + isPredefined + '&isHierarchy=' + isHierarchy, {
        headers: angular.getHttpHeader()
    })
        .then(function (result) {
            return result.data;
        }).catch(function (err) { return angular.errorHandler(err); } ) ;
}
```

**React/TypeScript**:
```typescript
async RetrieveAppProjectOrWorkFlows(projectOrWorflowType: any, isPredefined: any, isHierarchy: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/RetrieveAppProjectOrWorkFlows?projectOrWorflowType=${projectOrWorflowType || ''}&isPredefined=${isPredefined || ''}&isHierarchy=${isHierarchy || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app project or workflows');
    return response.json();
}
```

### Example 2: POST Request with Body

**Angular**:
```javascript
SaveProjectSettingExDto: function (data) {
    return $http.post(domainAndApplicationpath + '/webapi/ProjectWorkFlow/SaveProjectSettingExDto', data, {
        headers: angular.getHttpHeader()
    })
        .then(function (result) {
            return result.data;
        }).catch(function (err) { return angular.errorHandler(err); } ) ;
}
```

**React/TypeScript**:
```typescript
async SaveProjectSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/SaveProjectSettingExDto`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save project setting');
    return response.json();
}
```

### Example 3: GET Request with Single Parameter

**Angular**:
```javascript
DeleteProjectWorkFlow: function (proejctId) {
    return $http.get(domainAndApplicationpath + '/webapi/ProjectWorkFlow/DeleteProjectWorkFlow?proejctId=' + proejctId, {
        headers: angular.getHttpHeader()
    })
        .then(function (result) {
            return result.data;
        }).catch(function (err) { return angular.errorHandler(err); } ) ;
}
```

**React/TypeScript**:
```typescript
async DeleteProjectWorkFlow(proejctId: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ProjectWorkFlow/DeleteProjectWorkFlow?proejctId=${proejctId || ''}`, {
        headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete project workflow');
    return response.json();
}
```

---

## Common Pitfalls to Avoid

1. **Don't convert PascalCase to camelCase** - Keep Angular method names exactly as they are
2. **Don't omit null parameters** - Always include them in URL with empty string
3. **Don't forget `JSON.stringify()`** - Required for POST/PUT body data
4. **Don't change endpoint paths** - Match Angular exactly, including case
5. **Don't forget error handling** - Always check `response.ok`
6. **Don't use different HTTP methods** - Match Angular exactly
7. **Don't hardcode pipe-separated strings** - For dictionary response APIs, build entity code arrays programmatically and use constants for keys

---

## Reference Files

**Example React Service**: `src/webapi/projectWorkFlowSvc.ts`

**Example Angular Service**: `C:\DevApp\App\PlmApplication\Scripts1x\Services\projectWorkFlowSvc.js`

**Helper File**: `src/helper/apiServiceHelper.ts`

**Endpoints File**: `src/webapi/endpoints.ts`

---

## Special Pattern: Dictionary Response APIs (getMassEntitiesLookupItem)

### Pattern Description

Some APIs return a dictionary/object where keys are entity codes and values are arrays. When converting Angular code that uses these APIs, follow this specific pattern.

### Conversion Rule

**Rule**: When calling APIs that accept pipe-separated entity codes and return a dictionary, build the entity code list programmatically, then access the response dictionary using the entity code keys.

### Angular Pattern

**Angular Example**:
```javascript
adminSvc.getMassEntitiesLookupItem('AppSecurityRegDomain|AppCurrency').then(function (massEntityData) {
    $scope.dataModel.domainCV = new wijmo.collections.CollectionView(massEntityData['AppSecurityRegDomain']);
    $scope.dataModel.currencyCV = new wijmo.collections.CollectionView(massEntityData['AppCurrency']);
});
```

### React/TypeScript Pattern

**React/TypeScript Example**:
```typescript
const entityCode_AppSecurityRegDomain = 'AppSecurityRegDomain';
const entityCode_AppCurrency = 'AppCurrency';

const entityCodeList: string[] = [];
entityCodeList.push(entityCode_AppSecurityRegDomain);
entityCodeList.push(entityCode_AppCurrency);

const entityCodesAggregation = entityCodeList.join('|');

const dictEntityCodeAndListItems = await adminSvc.getMassEntitiesLookupItem(entityCodesAggregation);

setDataModel(prev => ({
    ...prev,
    domainCV: new CollectionView(dictEntityCodeAndListItems[entityCode_AppSecurityRegDomain] || []),
    currencyCV: new CollectionView(dictEntityCodeAndListItems[entityCode_AppCurrency] || [])
}));
```

### Key Points

1. **Define Entity Code Constants**: Create constants for each entity code (makes code maintainable and reusable)
2. **Build Array Programmatically**: Use an array and push entity codes (allows easy addition/removal)
3. **Join with Pipe**: Use `array.join('|')` to create the pipe-separated string
4. **Access Dictionary by Key**: Use bracket notation `dict[key]` to access the response dictionary
5. **Provide Fallback**: Always use `|| []` fallback when accessing dictionary values for CollectionView
6. **Use Async/Await**: Convert Angular promises to async/await pattern

### When to Use This Pattern

Use this pattern when:
- The API accepts pipe-separated entity codes (e.g., `'Entity1|Entity2|Entity3'`)
- The API returns a dictionary/object with entity codes as keys
- You need to access multiple entity lookup lists from a single API call
- The entity codes are used multiple times in the code (constants prevent typos)

### Example: Multiple Entity Codes

**Angular**:
```javascript
adminSvc.getMassEntitiesLookupItem('Entity1|Entity2|Entity3').then(function (data) {
    $scope.dataModel.cv1 = new wijmo.collections.CollectionView(data['Entity1']);
    $scope.dataModel.cv2 = new wijmo.collections.CollectionView(data['Entity2']);
    $scope.dataModel.cv3 = new wijmo.collections.CollectionView(data['Entity3']);
});
```

**React/TypeScript**:
```typescript
const entityCode_Entity1 = 'Entity1';
const entityCode_Entity2 = 'Entity2';
const entityCode_Entity3 = 'Entity3';

const entityCodeList: string[] = [];
entityCodeList.push(entityCode_Entity1);
entityCodeList.push(entityCode_Entity2);
entityCodeList.push(entityCode_Entity3);

const entityCodesAggregation = entityCodeList.join('|');

const dictEntityCodeAndListItems = await adminSvc.getMassEntitiesLookupItem(entityCodesAggregation);

setDataModel(prev => ({
    ...prev,
    cv1: new CollectionView(dictEntityCodeAndListItems[entityCode_Entity1] || []),
    cv2: new CollectionView(dictEntityCodeAndListItems[entityCode_Entity2] || []),
    cv3: new CollectionView(dictEntityCodeAndListItems[entityCode_Entity3] || [])
}));
```

### Reference Implementation

See `src/components/project/ProjectMgt.tsx` (lines 97-112) for a complete working example.

---

## Related Documents

- [AngularJsReferenceGuide.md](../01-reference-guides/AngularJsReferenceGuide.md) - For locating AngularJS code
- [DevelopmentPrompt.md](./DevelopmentPrompt.md) - General development guidelines
- [ConverterAngularJsPage.md](./ConverterAngularJsPage.md) - For converting Angular pages/components

---

*Last Updated: 2026-01-16*

---

## Quick Reference: Dictionary Response API Pattern

**When you see Angular code like this:**
```javascript
adminSvc.getMassEntitiesLookupItem('Entity1|Entity2').then(function (data) {
    $scope.dataModel.cv1 = new wijmo.collections.CollectionView(data['Entity1']);
    $scope.dataModel.cv2 = new wijmo.collections.CollectionView(data['Entity2']);
});
```

**Convert to React like this:**
```typescript
const entityCode_Entity1 = 'Entity1';
const entityCode_Entity2 = 'Entity2';

const entityCodeList: string[] = [];
entityCodeList.push(entityCode_Entity1);
entityCodeList.push(entityCode_Entity2);

const entityCodesAggregation = entityCodeList.join('|');

const dictEntityCodeAndListItems = await adminSvc.getMassEntitiesLookupItem(entityCodesAggregation);

setDataModel(prev => ({
    ...prev,
    cv1: new CollectionView(dictEntityCodeAndListItems[entityCode_Entity1] || []),
    cv2: new CollectionView(dictEntityCodeAndListItems[entityCode_Entity2] || [])
}));
```

**Key Rule**: Build entity code list programmatically, use constants for keys, always provide `|| []` fallback for CollectionView.
