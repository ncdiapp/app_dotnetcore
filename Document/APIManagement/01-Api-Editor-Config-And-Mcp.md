# API Editor: config form and Is Used By MCP

**Status:** confirmed 2026-10-14.

Two changes, one save:

1. Where an editor shows Api Config Parameters as a JSON text box, replace that box with a form.
2. Add an **Is Used By MCP** checkbox. Store it in a new nullable column, not in the JSON.

The Excel Import editor is left as it is. API responses stay as they are. Agent search stays as it is.

## What stays in the database

Api Config Parameters remain one JSON string in `dbo.AppIntergrationSettingParameter.APIConfigParameters`. The form does not add columns for URL, headers, or query parameters.

Keep the existing JSON names, including the spelling:

- `ResponseObjectMapToEnvionmentVariable`
- `ResponseHeaderNeedToSetCookieNames`

On save, if the request contains an `APIConfigParameters` object, the converter serializes that object and overwrites the string. The object must still contain fields the form does not show (`SpParameters`, and any other existing keys).

**Is Used By MCP** is a separate column: `IsUsedByMcp`.

## 1. The config form

The user fills fields. The page turns those fields into the same JSON on save, and turns the JSON back into fields when the API is opened.

| Form field | Stored as | Control |
|---|---|---|
| API URL | `BaseUrl` + `Url` | One text box |
| Headers | `Headers` | Key/value rows |
| Query Params | `QueryParams` | Key/value rows |
| Path Params | `PathParams` | Key/value rows |
| Post Process Method | `PostProcessMethodName` | Text box. Empty saves `null`. |
| Response → Environment | `ResponseObjectMapToEnvionmentVariable` | Key/value rows. Empty saves `{}`. |
| Response Header → Cookie | `ResponseHeaderNeedToSetCookieNames` | A list of names. Empty saves `[]`. |

Shared rules:

- `CurrentUserSessionId` is always present in Headers. The row cannot be deleted. An empty value is stored as JSON `null`. A typed value is stored as a string.
- Other header, query, and path values are stored as strings. A number such as `"id": 2` becomes `"id": "2"` after save. The call result is the same.
- An empty query or path key is not written. An empty value keeps the key and stores `""`.
- Keys the form does not edit are copied through unchanged.
- There is no switch back to raw JSON.

**API URL** is the address of the external API. It is not the AppAI address `/webapi/DataIntegration/{ActionCode}`. That address stays where it is today (the test tab, or the read-only line on the 3rd Party editor).

| What the user typed | `BaseUrl` | `Url` |
|---|---|---|
| Empty | `""` | `""` |
| `https://shop.example.com/admin/orders/{{PathParams.id}}` | `https://shop.example.com` | `admin/orders/{{PathParams.id}}` |
| Not an http(s) URL, such as `{{Env.ApiRoot}}/orders` | `""` | the whole text |

`BaseUrl` is only the scheme, host, and port. A saved value such as `https://host/api` + `orders` becomes `https://host` + `api/orders` the next time it is saved. The call is the same.

A query string in the box (`?a=1`) is not stored in `Url`. Parsed pairs are merged into Query Params. A query string that cannot be parsed stays on `Url`.

`{{Env....}}` and `{{PathParams....}}` are kept as text.

3rd Party calls use `BaseUrl` as the HTTP base address (`ExchangeBL.Helper.CallAPIAsync`). That page requires an `http://` or `https://` URL before save and before a test call. Data Model and Search may leave API URL empty. Their calls use the AppAI address.

If a Path Param key is set, warn when `Url` does not contain `{{PathParams.thatKey}}`. Still allow save.

## What each editor does

| Editor | Route | Config UI |
|---|---|---|
| Data Model | `/app-data-model-api-editor` | Replace the JSON box with the form. Add the checkbox. |
| Search | `/app-data-presentation-api-editor` | Same, and point Reset Config at the form. Add the checkbox. |
| 3rd Party | `/third-party-api-editor` | Replace the JSON box with the form. Add the checkbox. |
| Simple Query | `/api-builder-editor` | Keep the current query grid. Add the checkbox. |
| Stored Procedure | `/stored-procedure-api-editor` | Keep the current parameter grid. Add the checkbox. |
| Excel Import | `/excel-import-data-update-api-editor` | Do not change this page. |

JSON Sample, JSON Schema, Post Payload, and API Call Test stay as they are.

### Data Model

File: `AppReact/src/components/integration/AppDataModelApiEditor.tsx`

Remove the JSON text box on the API Config Parameters tab. Show the form.

- API URL may be empty.
- Query Params must include `id`, and the value cannot be empty, for both Get and Post. The test URL still adds `?id=` from that value.
- A new API starts like today's default: empty API URL, `CurrentUserSessionId` empty, one query row `id` / `1`.

### Search

File: `AppReact/src/components/integration/AppDataPresentationApiEditor.tsx`

Search does not use the shared key/value form. The page shows only labeled inputs:

- `CurrentUserSessionId`
- one row per search criterion (`SysTableFiledPath`). The name is read-only. The box is the default value.

There is no API URL, Headers, Query Params, Path Params, Post Process Method, or response-mapping editor, and no Add button.

The page binds `ApiConfigEditor` (`CurrentUserSessionId` plus `Criteria`). On read, `FillApiConfigEditor` builds that object from `APIConfigParameters` and the search field list. On save, `ApplyApiConfigEditorToJson` writes the session header and query defaults back into the JSON string and leaves other keys as they are. Reset Config, and changing Data Search, rebuilds the criteria from the selected search (default values from the search fields) and clears sample JSON and schema.

### 3rd Party

File: `AppReact/src/components/integration/ThirdPartyApiEditor.tsx`

Same form. Every row has a name and a value box. API URL is required and must be `http://` or `https://` before save and before a test call.

Keep **Insert Environment Variable** and **Insert Path Parameter**. They insert `{{Env.Key}}` or `{{PathParams.Key}}` into the API URL box at the cursor. The path-parameter list is the Path Params rows on the form.

The read-only AppAI URL at the top of the page stays read-only.

### Simple Query

File: `AppReact/src/components/integration/AppJsonQueryApiEditor.tsx`

Do not add the form. The query grid, JSON query, and Execute Query stay as they are. Query Params still come from that grid.

On save, if Headers have no `CurrentUserSessionId`, add it with value `null`. Keep a value that is already there.

### Stored Procedure

File: `AppReact/src/components/integration/AppStoredProcedureApiEditor.tsx`

Do not add the form. The parameter grid and Input JSON stay as they are. Keep `IsStoredProcedureApi`, `SpName`, `SpSchema`, `SpEngine`, and `SpParameters`.

On save, add `CurrentUserSessionId`: `null` when that header is missing.

### Excel Import

File: `AppReact/src/components/integration/ExcelImportDataUpdateApiEditor.tsx`

No form, no checkbox, no new header rule.

## 2. Is Used By MCP

The checkbox means: this API operation is intended for a later MCP response. It does not change who can call the API, and it does not change what the API returns in this round.

It is not the same as `AppMcpExposedApi` (migration V047). That table lists Swagger operations and security groups. This checkbox is one column on the API operation row (`SettingParameterID`). Nothing in this round writes to `AppMcpExposedApi`.

The checkbox sits beside Http Method. Label: **Is Used By MCP**.

Add it on Data Model, Search, 3rd Party, Simple Query, and Stored Procedure. Not on Excel Import. Not as a column on the API list.

Checked when the stored value is `true`. Unchecked when it is `false` or `null`. A new API opens unchecked. Changing the box marks the page dirty and saves with the rest of the form. There is no separate save call.

### How the server reads and writes the column

**Do not replace the entity load and save with SQL.** `RetrieveOne` and `Save` already map about 25 columns, then deserialize `APIConfigParameters` and the other JSON fields, fill the provider and data source, and on save rename duplicate action codes and write audit columns. Callers across import, stored-procedure APIs, search, and transactions depend on that DTO. Rewriting it as one hand-written `SELECT`/`UPDATE` means keeping a second copy of every column. That is harder to maintain, not easier. New tables such as `AppOpenApiDocument` use SQL because they never had an entity. This table already has one.

Only the new column uses SQL, beside the existing entity calls.

**Column.** Add `IsUsedByMcp BIT NULL` to `dbo.AppIntergrationSettingParameter`. No default. Existing rows stay `NULL`. Do not edit the V001 script. New migration: `AppAI.Web/Migrations/V050__AppIntergrationSettingParameter_IsUsedByMcp.sql`. Confirm `V050` is free with `check-duplicate-versions.ps1` before adding the file. Do not change `llblgenproj`, `AppIntergrationSettingParameterEntity`, or the generated DTO field list.

**Where the value lives on the DTO.** Add `bool? IsUsedByMcp` on the hand-written partial `APP.Components.Dto/UserDefine/AppIntegration/AppIntergrationSettingParameterDto.cs`. `CopyDtoToEntity` lists fields one by one, so this extra property is not copied onto the entity and is not written by `SaveEntity`.

**Connection.** Same as `AppOpenApiDocumentBL` and `McpApiAccessBL`: `AppTenantAdapterBL.GetTenantConnectionInfo()`, then `new SqlConnection(connStr)`. Parameterized `SqlCommand` only. No string-built ids.

**Read and write.** `ReadUnmappedColumns` and `WriteUnmappedColumns` on `AppIntergrationSettingBL` load and save every column in the `UnmappedColumns` list. The list is the only place that names `IsUsedByMcp`. The next extra column on this table is another entry in that list, plus a migration. Removing a column is deleting that entry and dropping the column. `GetValue` returns null to leave that column unchanged.

Retrieve calls `ReadUnmappedColumns` after the entity is converted. Save calls `WriteUnmappedColumns` after `SaveEntity` succeeds. A null `IsUsedByMcp` is not written, so an old row stays `NULL` until an editor sends `true` or `false`.

A row that was `NULL` becomes `0` or `1` the first time someone saves it from an editor. Rows that are never opened stay `NULL`. `SaveEntity` does not clear the column, because the column is not on the entity.

Code that creates an API through this same save (`GenerateAppFormProviderGetApi`, `GenerateAppFormProviderPostApi`, search generation, `AppStoredProcedureApiBL`) sets `IsUsedByMcp` to `false`, not `true`, so the update above stores `0`.

**Later business logic**, not this round: treat `NULL` as false (`value == true` is the only true case). PLM does the same with `IsMcpApi.HasValue && IsMcpApi.Value`, then changes the JSON response in `SealMcpResponse`. Do not port that response change, do not filter `ApiProviderCatalogBL`, and do not add `UiHint`.

## 3. Build order

1. Add the migration and the partial DTO property. Read and update the column with SQL beside the existing retrieve and save. Save and reload one row with `true`, then `false`. Do not regenerate LLBLGen.
2. Add helpers: JSON to form fields, and form fields back to JSON (URL split, rows, keep unknown keys).
3. Add `ApiConfigParametersForm` and use it on Data Model, Search, and 3rd Party. Wire Reset Config, the test URL, and the 3rd Party insert buttons to the form.
4. Add the checkbox on the five editors. Simple Query and Stored Procedure only add the header key and the checkbox.
5. Do not change `ExcelImportDataUpdateApiEditor.tsx`, `Helper.CallAPIAsync`, or `ApiProviderCatalogBL`.

## 4. Tests

Config form:

1. Open a Data Model API whose JSON has numeric `id` 2, empty `BaseUrl` and `Url`, and `CurrentUserSessionId` null. The form shows an empty API URL, query row `id` / `2`, and an empty session header. After save, `id` is the string `"2"`, the session header is still `null`, unknown keys remain, and the test URL is still `/webapi/DataIntegration/{ActionCode}?id=2`.
2. If that JSON had no `CurrentUserSessionId`, the form adds the row. It cannot be removed. Save writes `null`.
3. Data Model save is blocked when `id` is missing or empty.
4. A 3rd Party URL `https://shop.example.com/admin/orders/{{PathParams.id}}` saves as `BaseUrl` = `https://shop.example.com` and `Url` = `admin/orders/{{PathParams.id}}`, and opens as the same single URL.
5. A 3rd Party URL that is empty or not http(s) cannot be saved.
6. Search shows only `CurrentUserSessionId` and one labeled default for each criterion. Reset Config rebuilds those rows from the search fields. Save writes them into `Headers` and `QueryParams`.
7. Insert Environment Variable and Insert Path Parameter insert into the API URL box.
8. A save from the form keeps `SpParameters` and any other unknown keys. The two response-mapping names are unchanged. Sample and schema editors behave as before.
9. The Excel Import page still shows and saves the JSON box.

Is Used By MCP:

1. After migration, an old row is `NULL` and opens unchecked. A row that is never saved stays `NULL`.
2. Check the box and save on Data Model, Search, 3rd Party, Stored Procedure, and Simple Query. Reopen: still checked. The column is `1`. Excel Import has no checkbox.
3. Clear the box and save. The column is `0`, not `NULL`.
4. A new API saved unchecked stores `0`.
5. Saving only the checkbox does not change `APIConfigParameters`.
6. `AppMcpExposedApi` is unchanged. Agent search still returns the same APIs.

## Implementation record

Shipped with the confirmed plan:

- Migration `V050__AppIntergrationSettingParameter_IsUsedByMcp.sql`.
- `IsUsedByMcp` on the hand-written DTO partial. `ReadUnmappedColumns` / `WriteUnmappedColumns` apply the `UnmappedColumns` list beside the existing entity retrieve and save. The entity load and save were not replaced.
- Config form on Data Model, Search, and 3rd Party. Checkbox on those three plus Simple Query and Stored Procedure. Excel Import was not changed.
- `AppReact/src/helper/apiConfigForm.test.ts` covers URL split, the session header, and unknown JSON keys.

## Decisions

Confirmed 2026-10-14:

- Split API URL into scheme + host + port, and the rest of the path.
- Store query, path, and ordinary header values as strings. `CurrentUserSessionId` is required and stores `null` when empty.
- `IsUsedByMcp` is `BIT NULL`. The editor saves `true` or `false`. Later logic treats null as false.
- Do not filter Agent search. The flag is for a later MCP vs non-MCP response. Do not port PLM `SealMcpResponse`.
- Do not change the Excel Import editor.
