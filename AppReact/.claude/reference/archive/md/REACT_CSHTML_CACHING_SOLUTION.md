# React App Solution for CSHTML Server-Side Caching

## Problem Statement

The AngularJS app uses CSHTML pages that:
1. Call server-side BL classes directly (e.g., `AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable()`)
2. Get `AppTransactionExDto` with all form structure data (including `TransactionOrganizedType`)
3. Server caches the **rendered CSHTML view** (the HTML output) per `transactionId`
4. Cached views are reused for all users until server restart

**Key Insight**: The performance bottleneck is **CSHTML rendering** (Razor syntax processing, loops, HTML generation), NOT the DTO retrieval. The BL methods that get the DTO are fast enough.

For React (SPA), we need to:
- Get the same `AppTransactionExDto` data that CSHTML uses
- **No need for server-side caching** because:
  - React doesn't render CSHTML (no rendering performance issue)
  - BL methods are fast (DTO retrieval is not the bottleneck)
  - We just need to call the API to get the DTO

## Solution: Simple API Endpoint (No Caching Needed)

### Concept

**Why No Caching?**
- **AngularJS**: CSHTML rendering (with loops, Razor syntax) is slow → Cache the rendered HTML
- **React**: No CSHTML rendering → No performance issue → No caching needed
- **BL Methods**: Fast enough for both AngularJS and React → No caching needed

**Solution**: Create a simple API endpoint that:
1. Calls the same BL method that CSHTML uses
2. Returns `AppTransactionExDto` as JSON
3. **No caching** - BL methods are fast, and React doesn't have rendering overhead

### Implementation

#### 1. Server-Side: Use Your Existing DynamicLayoutController

**Location**: `{SolutionRoot}\PlmApplication\Server\WebApi\DynamicLayoutController.cs`

**Your existing implementation is perfect!** No changes needed. It already:
- Calls `AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable()` (same as CSHTML)
- Returns `AppTransactionExDto` as JSON
- Handles all the same cases (preload, print, normal form)

```csharp
[HttpGet]
public AppTransactionExDto TransactionForm(int transactionId, int? transGroupId, string rootPkId, string isPrint, int? opennedFormAutoExecuteCommandId = null, string isPreview = "")
{
    base.InitializeSecurity();

    AppTransactionExDto appTransactionExDto = null;

    // -1: Preload MVC Views
    if (transactionId == -1)
    {
        int? fileTransactionId = AppSetupBL.GetIntValue(EmApplicationSettings.SystemDefinedFileTransactionId);
        if (fileTransactionId.HasValue)
        {
            transactionId = fileTransactionId.Value;
            appTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            appTransactionExDto.IsLoadingPrintForm = false;
            appTransactionExDto.IsAllowAccess = true;
        }
    }
    else
    {
        if (!string.IsNullOrWhiteSpace(isPrint) && isPrint.ToLower() == "true")
        {
            bool isConfigTestRun = !string.IsNullOrWhiteSpace(isPreview) && isPreview.ToLower() == "true";
            appTransactionExDto = AppMasterDetailFormPrintBL.PrepareFormMasterDetailPrintData(transactionId, rootPkId, false, opennedFormAutoExecuteCommandId, isConfigTestRun);
            appTransactionExDto.IsLoadingPrintForm = true;
        }
        else
        {
            // Same BL method that CSHTML uses - fast enough, no caching needed
            appTransactionExDto = AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable(transactionId, rootPkId);
            appTransactionExDto.IsLoadingPrintForm = false;
        }
    }

    appTransactionExDto.TransactionHeader = new List<AppTransactionExDto>();
    appTransactionExDto.TransactionCrossHeader = new List<AppTransactionExDto>();

    AppFormExDto AppFormExDto = appTransactionExDto.ForeignAppFormExDto;
    if (AppFormExDto == null && appTransactionExDto.TransactionOrganizedType.HasValue
        && (appTransactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail))
    {
        AppFormExDto = appTransactionExDto.ForeignAppFormExDto = AppFormBL.RetrieveTransactionAppFormExDto(appTransactionExDto);
    }

    return appTransactionExDto;
}
```

**URL Pattern**: 
- `${endpoints.BASE_URL}/webapi/DynamicLayout/TransactionForm?transactionId={transactionId}&rootPkId={rootPkId}&...`

**Key Points**:
- ✅ Uses the **same BL method** as CSHTML: `AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable()`
- ✅ Returns **AppTransactionExDto** which includes:
  - `TransactionOrganizedType` ✅
  - `ForeignAppFormExDto` (form layout) ✅
  - All form structure data ✅
  - Security info (`IsAllowAccess`, field visibility) ✅
- ✅ **No caching needed** - BL methods are fast, React doesn't render CSHTML

#### 2. React-Side: API Service

**Location**: `src/webapi/dynamiclayoutsvc.ts`

**Service Implementation**:
```typescript
import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

class DynamicLayoutService {
  /**
   * Gets transaction form structure.
   * This endpoint returns AppTransactionExDto with all form structure and security data.
   * 
   * @param transactionId - The transaction ID
   * @param rootPkId - Optional root primary key ID
   * @param isPrint - Optional print flag ("true" or "false")
   * @param opennedFormAutoExecuteCommandId - Optional auto-execute command ID
   * @param isPreview - Optional preview flag
   * @returns Promise<AppTransactionExDto> - The transaction form structure with security
   */
  async getTransactionForm(
    transactionId: number,
    rootPkId?: string,
    isPrint?: string,
    opennedFormAutoExecuteCommandId?: number,
    isPreview?: string
  ): Promise<any> {
    const params = new URLSearchParams();
    params.append('transactionId', transactionId.toString());
    
    if (rootPkId) {
      params.append('rootPkId', rootPkId);
    }
    
    if (isPrint) {
      params.append('isPrint', isPrint);
    }
    
    if (opennedFormAutoExecuteCommandId !== undefined) {
      params.append('opennedFormAutoExecuteCommandId', opennedFormAutoExecuteCommandId.toString());
    }
    
    if (isPreview) {
      params.append('isPreview', isPreview);
    }

    const url = `${endpoints.buildEndpointUrl('/webapi/DynamicLayout/TransactionForm')}?${params.toString()}`;
    
    const response = await fetch(url, {
      headers: getHeaders()
    });
    
    if (!response.ok) {
      throw new Error(`Failed to get transaction form structure: ${response.statusText}`);
    }
    
    return response.json();
  }
}

export const dynamicLayoutService = new DynamicLayoutService();
```

**Usage**:
```typescript
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';

// Get transaction form structure
const transactionExDto = await dynamicLayoutService.getTransactionForm(
  transactionId,
  rootPkId
);

// transactionExDto contains:
// - TransactionOrganizedType
// - ForeignAppFormExDto (form layout)
// - IsAllowAccess (user-specific security)
// - Field visibility/read-only flags (user-specific)
```

#### 3. React Component: Use the Service

**Location**: `src/components/formMgt/FormMasterDetail.tsx`

**Update `loadFormStructure` function**:
```typescript
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';

const loadFormStructure = async (): Promise<any> => {
  if (!transactionId) return null;

  try {
    // Get AppTransactionExDto from DynamicLayoutController
    // This is the same data that CSHTML uses, but as JSON instead of rendered HTML
    const transactionExDto = await dynamicLayoutService.getTransactionForm(transactionId);
    
    // Extract form structure from AppTransactionExDto
    const formStructure = {
      TransactionId: transactionExDto.Id,
      TransactionOrganizedType: transactionExDto.TransactionOrganizedType, // ✅ Now available!
      ForeignAppFormExDto: transactionExDto.ForeignAppFormExDto,
      RootUnitId: transactionExDto.RootUnitId,
      IsAllowAccess: transactionExDto.IsAllowAccess, // ✅ User-specific security
      // ... map other properties from AppTransactionExDto to form structure format
    };
    
    formStructureDataRef.current = formStructure;
    setDataModel((prev: any) => ({
      ...prev,
      currentFormStructure: formStructure,
      // Also store the full transactionExDto for reference
      transactionExDto: transactionExDto
    }));
    
    return formStructure;
  } catch (error) {
    console.error('Error loading form structure:', error);
    showError((error as Error).message);
    return null;
  }
};
```

### Benefits

1. ✅ **Same Data**: Gets the same `AppTransactionExDto` that CSHTML uses
2. ✅ **No Code Duplication**: Reuses existing BL logic
3. ✅ **Simple**: No complex caching logic needed
4. ✅ **Complete Data**: Gets `TransactionOrganizedType` and all form structure
5. ✅ **Security-Safe**: Each user gets their own security-filtered data
6. ✅ **Performance**: BL methods are fast, React doesn't have CSHTML rendering overhead

### Why No Caching?

**AngularJS (CSHTML)**:
- CSHTML rendering is slow (Razor syntax, loops, HTML generation)
- Solution: Cache the rendered HTML output
- DTO retrieval is fast (not the bottleneck)

**React (SPA)**:
- No CSHTML rendering (no performance issue)
- DTO retrieval is fast (same as AngularJS)
- Solution: Just call the API, no caching needed

### Comparison

| Aspect | AngularJS (CSHTML) | React (SPA) |
|--------|-------------------|-------------|
| **Rendering** | Server-side CSHTML (slow) | Client-side React (fast) |
| **Caching** | Cache rendered HTML | No caching needed |
| **DTO Retrieval** | Fast (not cached) | Fast (not cached) |
| **Performance Issue** | CSHTML rendering | None (no CSHTML) |

### Implementation Checklist

- [x] You already have `DynamicLayoutController.TransactionForm` endpoint ✅
- [x] Create `dynamiclayoutsvc.ts` React service ✅
- [ ] Update `FormMasterDetail.tsx` to use `dynamicLayoutService.getTransactionForm()`
- [ ] Map `AppTransactionExDto` properties to form structure format
- [ ] Test that `TransactionOrganizedType` is now available
- [ ] Test security (different users see different field visibility)

### Notes

- **No Server-Side Caching**: BL methods are fast, React doesn't render CSHTML
- **Same BL Method**: Uses `GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable` (same as CSHTML)
- **Security**: Each user gets their own security-filtered data (not cached)
- **Performance**: No performance issue because React doesn't have CSHTML rendering overhead
