# Data Model Template Editor – React vs Angular UI Diff

## Summary
- **Angular** uses the full "Data Model Template Editor" (TransactionGroupEditor.cshtml + transactionGroupEditorCtrl.js), which is **Search-based**: template = AppSearchExDto with Type=DataModelTemplate, plus link targets on the default view for "Template Main Items" / "Template Shared Items".
- **React (current)** implemented the **simple** "Data Model Group Editor" (TransactionGroupEditor - Old): Group Name, Description, Group Items (Sort, Data Model Item, Is Header) using AppTransactionGroupExDto.

## UI / Feature Diff (Angular → React)

| Area | Angular (full) | React (before) | Action |
|------|----------------|----------------|--------|
| **Title** | "Data Model Template Editor" | "Data Model Group Editor" | Use Angular title |
| **Toolbar** | Refresh, Save, Advanced Options, Save As, Add To Main Menu, Test Run | Refresh, Save | Add 4 buttons (placeholders or full) |
| **Main fields** | Name, Dataset (dropdown + edit), Description | Group Name, Description | Use Name + Dataset + Description; load/save as Search |
| **Layout** | Tabs: Template Items \| Template View Fields \| Template Filters | Single section "Group Items" | Add tabs; tab 1 = Template Items, 2 = View Fields, 3 = Filters |
| **Template Items tab** | Template Main Items (Add Data Model, Add Search; rows: Sort, Data Model/Search, Display Name, View Field Mapping, Data Model PK); Template Shared Items (same) | Group Items grid (Sort, Data Model Item, Is Header) | Replace with Main/Shared sections + link-target rows |
| **Template View Fields tab** | Views grid (Create View, Edit, Delete, Edit Navigation Menus); default view selection | (none) | Reuse SearchEditor views logic or placeholder |
| **Template Filters tab** | Filters grid (Add Filter, Remove, mapping, etc.) | (none) | Reuse SearchEditor filters logic or placeholder |
| **Load** | retrieveOneAppTransactionGroupExDto → AppSearchExDto (same as search) | retrieveOneAppTransactionGroupExDto → group + AppTransactionGroupItemList | Keep load; treat as Search (Name, DataSetId, Description, SearchViewId) |
| **Save** | saveAppSearchExDto + saveOneSearchViewLinkTargetList for link targets | saveAppTransactionGroupExDto | Use saveAppSearchExDto for template; add link-target save when implementing Template Items |

## Backend
- **RetrieveAllAppTransactionGroupDto(applicationId)** → List&lt;AppSearchDto&gt;
- **RetrieveOneAppTransactionGroupExDto(groupId)** → AppSearchExDto (same as RetrieveOneAppSearchExDto)
- **Save**: Full editor uses AppSearchViewConfig.SaveAppSearchExDto (searchSvc.saveAppSearchExDto). Link targets: saveOneSearchViewLinkTargetList (searchSvc).
- **SaveAppTransactionGroupExDto** (AppTransactionGroupExDto): used by the **simple** Old editor only.

## Implementation order
1. Align title, toolbar, and main form (Name, Dataset, Description) with Angular; load/save via retrieveOneAppTransactionGroupExDto + saveAppSearchExDto.
2. Add tabs (Template Items, Template View Fields, Template Filters).
3. Implement Template Items tab (Template Main Items + Template Shared Items, Add Data Model / Add Search, link target APIs).
4. Implement Template View Fields and Template Filters (reuse or mirror SearchEditor).
