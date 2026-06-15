# Development Prompt for React Application Migration

## Project Context

You are working on migrating an AngularJS application to React. The React application is located at:
- **React App Path**: `C:\DevApp\app-react`

The original AngularJS application serves as the **reference implementation** and is located at:
- **AngularJS Solution Path**: `C:\DevApp\App\PlmApplication`

## Reference Implementation Rules

### 1. Always Reference the Original AngularJS Implementation and Server-Side Projects

When implementing or debugging React components, **ALWAYS** refer to the corresponding implementation in the original solution:

**Solution Root**: `C:\DevApp\App\`
**Main Solution File**: `C:\DevApp\App\PLMS  Solution.sln`

#### Web Application (AngularJS Frontend)
- **AngularJS Controllers**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\`
- **AngularJS Services**: `C:\DevApp\App\PlmApplication\Scripts1x\Services\`
- **AngularJS Routes**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtRoute.js`
- **Server Controllers (MVC)**: `C:\DevApp\App\PlmApplication\Server\Controllers\`
- **Server Views (Razor)**: `C:\DevApp\App\PlmApplication\Server\Views\`
- **Server WebAPI**: `C:\DevApp\App\PlmApplication\Server\WebApi\`

#### Business Logic Layer
- **Business Logic**: `C:\DevApp\App\APP.BL\`
  - Key classes: `AppTransactionBL`, `AppMasterDetailFormDataLoadBL`, `AppTransactionFormulaBL`, `AppTransactionCommandBL`, `AppFormBL`, `AppSearchBL`
  - Integration: `C:\DevApp\App\APP.BL\Integration\`
  - Email: `C:\DevApp\App\APP.BL\Email\`
  - Third-party: `C:\DevApp\App\APP.BL\ThirdPartIT\`

#### DTOs and Data Transfer Objects
- **DTO Project**: `C:\DevApp\App\APP.Components.Dto\`
  - **Entity DTOs**: `C:\DevApp\App\APP.Components.Dto\EntityDto\` - Basic DTOs
  - **Extended DTOs**: `C:\DevApp\App\APP.Components.Dto\EntityExdto\` - Extended DTOs with additional properties
  - **User-Defined DTOs**: `C:\DevApp\App\APP.Components.Dto\UserDefine\`
    - Form Data: `C:\DevApp\App\APP.Components.Dto\UserDefine\AppFormData\` (e.g., `AppMasterDetailDto`, `AppTransactionStructureDto`)
    - Integration: `C:\DevApp\App\APP.Components.Dto\UserDefine\AppIntegration\`
    - Search: `C:\DevApp\App\APP.Components.Dto\UserDefine\Search\`
    - Workflow: `C:\DevApp\App\APP.Components.Dto\UserDefine\ProjectWorkFlow\`
  - **Validation DTOs**: `C:\DevApp\App\APP.Components.Dto\Validation\`
  - **Database Schema**: `C:\DevApp\App\APP.Components.Dto\DatabaseSchema\`
  - **Enums**: `C:\DevApp\App\APP.Components.Dto\AppEnums.cs`

#### Framework and Utilities
- **Framework**: `C:\DevApp\App\APP.Framework\` - Helpers, utilities, validation, cache providers

#### Entity Converters
- **Entity Converter**: `C:\DevApp\App\APP.Components.EntityConverter\` - Entity to DTO conversion

#### Database Layer
- **Database Layer**: `C:\DevApp\App\Com.Visual2000.LBL\` - Database-specific implementations and ORM

### 2. Key Differences to Understand

**AngularJS (Original)**:
- Uses server-side rendering with Razor views (`*.cshtml`)
- Server passes `AppTransactionExDto` model to views with properties like `TransactionOrganizedType`
- Client-side AngularJS controllers receive pre-rendered HTML
- Data flows: Server Model → Razor View → AngularJS Controller → API Calls

**React (New)**:
- Pure client-side SPA (Single Page Application)
- No server-side rendering
- All data loaded via API calls
- Data flows: React Component → API Calls → State Management → Render

### 3. Migration Strategy

When implementing a React component:

1. **Find the AngularJS equivalent**:
   - Search for the controller in `C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\`
   - Find the corresponding service in `C:\DevApp\App\PlmApplication\Scripts1x\Services\`
   - Check the server controller action in `C:\DevApp\App\PlmApplication\Server\Controllers\`
   - Review the Razor view in `C:\DevApp\App\PlmApplication\Server\Views\`

2. **Understand the business logic**:
   - Check the WebAPI controller in `C:\DevApp\App\PlmApplication\Server\WebApi\`
   - Find the business logic class in `C:\DevApp\App\APP.BL\` (e.g., `AppTransactionBL`, `AppMasterDetailFormDataLoadBL`)
   - Understand what the business logic does and how it processes data
   - Check for validation, calculations, or transformations

3. **Understand the data structures**:
   - Find DTOs in `C:\DevApp\App\APP.Components.Dto\EntityDto\` or `EntityExdto\`
   - Check user-defined DTOs in `C:\DevApp\App\APP.Components.Dto\UserDefine\`
   - Review the DTO properties to understand the data model
   - Check enums in `C:\DevApp\App\APP.Components.Dto\AppEnums.cs`

4. **Understand the data flow**:
   - What server model properties are used?
   - What API endpoints are called?
   - What business logic processes the data?
   - What data structures are returned?
   - How is the UI rendered conditionally?

5. **Map to React patterns**:
   - AngularJS controllers → React functional components with hooks
   - AngularJS services → React API service files in `src/webapi/`
   - AngularJS `$scope` → React state (useState, useReducer, or Redux)
   - AngularJS `$http` → React fetch/axios calls
   - AngularJS routes → React Router in `src/routes.tsx`
   - Business logic classes → Understand the logic, replicate in React or ensure API calls match

6. **Handle missing data**:
   - If AngularJS gets data from server-side model, React must get it via API
   - If API doesn't return needed data, either:
     - Add it to the API response (preferred)
     - Make additional API calls
     - Use reasonable defaults based on context
   - Check business logic classes to understand what data transformations occur

## Example: FormMasterDetail Component

### AngularJS Implementation Reference

**Controller**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Form\formMasterDetailCtrl.js`
- Initializes with `$stateParams.Id` (transactionId) and `$stateParams.param1` (rootPrimaryKeyValue)
- Calls `loadFormStructure()` then `getFormData()` if editing
- Uses `_FormStructureData` ref to store form structure
- Sets `$scope.controllerModel.isMasterDetailForm = true`

**Service**: `C:\DevApp\App\PlmApplication\Scripts1x\Services\appTransactionSvc.js`
- `getFormStructure(transactionId)` → `/webapi/AppTransaction/GetFormStructure`
- `getFormData(transactionId, rootPrimaryKeyValue, ...)` → `/webapi/AppTransaction/GetFormData`

**Server Controller**: `C:\DevApp\App\PlmApplication\Server\Controllers\FormMgtController.cs`
- `TransactionForm()` action returns `AppTransactionExDto` with `TransactionOrganizedType`
- This property is NOT in API responses, only in server-side model

**Server View**: `C:\DevApp\App\PlmApplication\Server\Views\FormMgt\TransactionForm.cshtml`
- Checks `Model.TransactionOrganizedType` to render correct partial view
- Renders `_MasterDetailEditLayoutForm.cshtml` if type is MasterDetail (1)

### React Implementation

**Component**: `src/components/formMgt/FormMasterDetail.tsx`
- Uses `useParams()` to get route parameters
- Calls `appTransactionService.getFormStructure()` and `getFormData()`
- Since `TransactionOrganizedType` is not in API responses, defaults to MasterDetail (1) when form structure exists
- This matches the component's purpose (FormMasterDetail = MasterDetail form)

## Development Workflow

### When Implementing a New Feature:

1. **Search the AngularJS codebase first**:
   ```
   Search in: C:\DevApp\App\PlmApplication
   Look for: Controller name, service method, or view file
   ```

2. **Understand the original implementation**:
   - What data does it need?
   - What API calls does it make?
   - What UI components does it use?
   - What business logic does it implement?

3. **Check for server-side dependencies**:
   - Does it rely on server-side model properties?
   - Are those properties available via API?
   - If not, how should we handle it?

4. **Implement in React**:
   - Create/update component in `src/components/`
   - Add/update API service in `src/webapi/`
   - Add/update route in `src/routes.tsx` if needed
   - Use Redux for global state if needed

5. **Test and compare**:
   - Test with same data/parameters as AngularJS
   - Compare behavior and UI
   - Ensure all features work correctly

### When Debugging an Issue:

1. **Compare with AngularJS behavior**:
   - How does AngularJS handle this case?
   - What data does AngularJS have that React doesn't?
   - What's different in the data flow?

2. **Check API responses**:
   - Use browser DevTools to inspect API calls
   - Compare response structure with AngularJS expectations
   - Check if any properties are missing

3. **Review server-side logic**:
   - Check server controller actions
   - Review server views for conditional logic
   - Understand what the server provides vs. what APIs return

## Important File Locations

### React Application
- **Components**: `src/components/`
- **API Services**: `src/webapi/`
- **Routes**: `src/routes.tsx`
- **Redux Store**: `src/redux/store.ts`
- **Redux Features**: `src/redux/features/`
- **Types**: `src/types/`
- **Helpers**: `src/helper/`

### AngularJS Reference (Original)
- **Solution Root**: `C:\DevApp\App\`
- **Solution File**: `C:\DevApp\App\PLMS  Solution.sln`
- **Web App Controllers**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\`
- **Web App Services**: `C:\DevApp\App\PlmApplication\Scripts1x\Services\`
- **Web App Routes**: `C:\DevApp\App\PlmApplication\Scripts1x\mgtRoute.js`
- **Server Controllers**: `C:\DevApp\App\PlmApplication\Server\Controllers\`
- **Server Views**: `C:\DevApp\App\PlmApplication\Server\Views\`
- **Server WebAPI**: `C:\DevApp\App\PlmApplication\Server\WebApi\`
- **Business Logic**: `C:\DevApp\App\APP.BL\`
- **DTOs**: `C:\DevApp\App\APP.Components.Dto\`
- **Framework**: `C:\DevApp\App\APP.Framework\`
- **Entity Converters**: `C:\DevApp\App\APP.Components.EntityConverter\`
- **Database Layer**: `C:\DevApp\App\Com.Visual2000.LBL\`

## Common Patterns

### Data Loading Pattern

**AngularJS**:
```javascript
loadFormStructure(function (formStructureData) {
    if (rootPrimaryKeyValue) {
        callGetFormDataWS(formStructureData);
    } else {
        loadNewFormData(formStructureData);
    }
});
```

**React Equivalent**:
```typescript
useEffect(() => {
    const loadData = async () => {
        const formStructure = await loadFormStructure();
        if (rootPrimaryKeyValue) {
            await loadFormData(formStructure);
        } else {
            prepareNewFormData(formStructure);
        }
    };
    loadData();
}, [transactionId, rootPrimaryKeyValue]);
```

### State Management Pattern

**AngularJS**:
```javascript
$scope.dataModel.currentFormData = formData;
$scope.dataModel.currentFormStructure = formStructure;
```

**React Equivalent**:
```typescript
const [dataModel, setDataModel] = useState({
    currentFormData: null,
    currentFormStructure: null
});

setDataModel(prev => ({
    ...prev,
    currentFormData: formData,
    currentFormStructure: formStructure
}));
```

## Best Practices

1. **Always check the AngularJS implementation first** before writing new React code
2. **Document differences** when React implementation differs from AngularJS
3. **Preserve business logic** - don't change how features work, only how they're implemented
4. **Test with same data** - use identical parameters and data to ensure parity
5. **Handle missing data gracefully** - if server-side model properties aren't in APIs, use reasonable defaults
6. **Maintain API compatibility** - don't change API contracts unless necessary
7. **Reference the flow document** - see `Prompt/05-Specific-Component-Rule/FormMasterDetailFlow.md` for detailed flow analysis
8. **Use `appHelper.debugLog()` for all debug logging** - Never use `console.log()` for debug purposes. Always use `appHelper.debugLog()` instead, which can be controlled via global debug flag. See `Prompt/05-Specific-Component-Rule/DEBUG_LOG_USAGE.md` for details.

## Quick Reference Commands

When working on this project, you can reference files using:
- React app: `c:\DevApp\app-react\src\...`
- AngularJS reference: `C:\DevApp\App\PlmApplication\...`

Search patterns:
- Find AngularJS controller: Search for controller name in `Scripts1x/mgtCtrl/`
- Find AngularJS service: Search for service method in `Scripts1x/Services/`
- Find server action: Search for action name in `Server/Controllers/`
- Find server view: Search for view name in `Server/Views/`

## Notes

- The AngularJS solution uses server-side rendering, so some properties (like `TransactionOrganizedType`) are available in server models but not in API responses
- React is a pure SPA, so all data must come from API calls
- When in doubt, check what the AngularJS implementation does and replicate that behavior in React
- Use the AngularJS codebase as the source of truth for business logic and data structures
