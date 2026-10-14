# API editor layout

**Status:** confirmed 2026-10-14. Built in the four editors. Simple Query and Excel Import were not changed.

This replaces the page layout of the API editors. It does not change what is stored. Config is still `APIConfigParameters`. The MCP checkbox stays where it is.

Simple Query and Excel Import stay as they are.

## Why

The editors show the same job in different shapes. 3rd Party is already a left editor and a right result. Search and Data Model hide the result behind an API Call Test tab, so the config panel and the result cannot be seen together. Data Model still shows the full key/value form. Search already shows only session and criteria.

One shell makes the pages easier to learn. Each API type only changes what the Form tab contains.

## Shell

Top bar stays: title, operation fields, **Is Used By MCP**, and the action buttons (Send Request, Refresh, Save, and any type-specific button such as Reset Config).

Under that, one read-only **API Url** line. This is the AppAI address `/webapi/DataIntegration/{ActionCode}`, including query values taken from the Form when the method is Get. On 3rd Party, the external address stays inside the Form. It is not this line.

Under the URL, two columns that fill the remaining height:

| Left | Right |
|---|---|
| API Config, and Payload when the method needs a body | **Result** |

The old API Config Parameters / API Call Test tabs on Search and Data Model go away. Send Request writes into the Result column. The user does not switch pages to see the response.

## Left side

Two levels. The outer level follows the HTTP method. The inner level is only inside API Config.

### Outer

| Method | What the user sees |
|---|---|
| Get | A header **API Config**. No Payload tab. |
| Post, Put | Two tabs: **API Config** and **Payload**. |

Payload is the request body (`JsonSampleData`, and on 3rd Party the payload type already on that page). Re-generate Payload on Data Model stays on the Payload tab.

### Inner, under API Config

Two tabs:

| Tab | What it shows |
|---|---|
| **Form** | The fields for this API type. |
| **JSON** | The same settings as `APIConfigParameters` text. |

Names are **Form** and **JSON**.

### Form and JSON convert both ways

Editing either side updates the other.

- A change on the Form rewrites the JSON text immediately.
- A change on the JSON tab is applied to the Form when the text is valid JSON (on each successful parse, and again when leaving the tab). Invalid JSON shows the error and leaves the Form as it was.
- Keys the Form does not edit are kept. Parsing does not drop `SpName`, `SpSchema`, `SpEngine`, or any other unknown key.
- Empty `CurrentUserSessionId` is JSON `null`. A typed value is a string.
- Query and header values the Form edits are strings.

Search still loads and saves through `FillApiConfigEditor` and `ApplyApiConfigEditorToJson`. The JSON tab is the same string. On the client, Form and JSON stay in sync before save. Save sends that string (and, for Search, `ApiConfigEditor`) as it does now.

Search Form rows are fixed by the search definition. A query key in the JSON that is not a criterion stays in the JSON and does not add a Form row. Editing a criterion on the Form updates that query value.

## What each Form tab contains

### 3rd Party

File: `AppReact/src/components/integration/ThirdPartyApiEditor.tsx`

Keep every current field: API URL, headers, query params, path params, post process method, response mapping, cookie names. Each row keeps a name and a value. Insert Environment Variable and Insert Path Parameter stay on the Form tab and still insert into the API URL box.

Get: API Config only. Post/Put: API Config and Payload. Payload keeps the current type selector and editor.

### Search

File: `AppReact/src/components/integration/AppDataPresentationApiEditor.tsx`

Same shell. Remove the API Config Parameters / API Call Test tabs.

Form tab stays as it is now: `CurrentUserSessionId` plus one read-only criterion name and its default value. No Add button.

Search does not get a Payload tab.

The method dropdown lists Get and Post, but only Get runs the search. `DataIntegrationController` Get calls `PrepareJsonData_SearchResultGetApi` with the query string. Post calls `ExecuteApiOperationSaveCommand`, which has no search branch and returns an empty string. Criteria are query parameters, not a body. This layout does not add a body editor for a call the server does not run. The dropdown stays, so an existing Post row can still be opened.

### Data Model (Transaction)

File: `AppReact/src/components/integration/AppDataModelApiEditor.tsx`

Same shell. Remove the API Config Parameters / API Call Test tabs.

Form tab shows only two rows:

- `CurrentUserSessionId`
- `Id` (the query parameter `id`, value required)

No API URL, headers list, path params, post process method, or response-mapping rows, and no Add button. Other JSON keys stay in the stored string and show on the JSON tab. They are not edited on the Form.

Post/Put: Payload tab, including Re-generate Payload. Get: API Config only.

This is a change from the earlier full form on this page. Storage rules in `01-Api-Editor-Config-And-Mcp.md` still apply: `id` is a string query value, and an empty session value is JSON `null`.

### Stored Procedure

File: `AppReact/src/components/integration/AppStoredProcedureApiEditor.tsx`

The stored procedure API is always Post, so the left side has **API Config** and **Payload**.

`CurrentUserSessionId` is not a procedure parameter and it is not part of the call body. The Form shows only that field. It is stored at `Headers.CurrentUserSessionId`. An empty value is `null`.

The parameter grid is the payload. `DefaultValue` is merged with the request body when the procedure runs. The grid and the input JSON are two views of those values, so both sit under Payload.

| Place | SP content |
|---|---|
| API Config → Form | `CurrentUserSessionId` only. |
| API Config → JSON | `APIConfigParameters`, including `SpName`, `SpSchema`, `SpEngine`, and `SpParameters`. |
| Payload → Parameters | The parameter grid. Reset Parameters stays here and does not clear the session value. |
| Payload → JSON | Input JSON, the body sent by Send Request. It stays two-way with the parameter default values. |
| Result | API Response Data. |

The read-only API Url line replaces the separate API Url panel above the columns.

## What this round does not move

- Simple Query (`AppJsonQueryApiEditor.tsx`). The SQL editor is the page. The test area is not the same as a config form.
- Excel Import. Unchanged, as already decided.
- Database columns, MCP behavior, and the call pipeline.

## Implementation

1. Add one shared layout component: top fields, API Url line, left outer tabs, Form/JSON inner tabs, right Result. Each editor passes its Form, its Payload (or none), and its Result.
2. Point 3rd Party, Search, Data Model, and Stored Procedure at that layout. Delete the Config/Test split on Search and Data Model.
3. Data Model Form: only session and `Id`. Keep unknown JSON keys.
4. Keep Form and JSON in sync both ways. Invalid JSON does not clear the Form.
5. Stored Procedure: Form is only the session header. The parameter grid and input JSON are sub-tabs under Payload.
6. Search has no Payload tab.
7. Keep both columns on `h-1 flex-auto min-h-0` so the panels use the remaining height.

No migration. No LLBL change.

## Test, after it is built

1. 3rd Party Get: left side is API Config with Form and JSON. No Payload. Result is on the right. Post adds a Payload tab and keeps the payload type control.
2. Search: no Config/Test tabs and no Payload tab. Form is session plus criteria. Editing JSON updates those fields, and editing the Form updates JSON. Result is on the right. Reset Config still rebuilds criteria.
3. Data Model: Form is only `CurrentUserSessionId` and `Id`. Post shows Payload and Re-generate Payload. Get does not. Save still writes `id` in Query Params and keeps other JSON keys. JSON edits update session and `id` only.
4. Stored Procedure: Form is only `CurrentUserSessionId`. Payload has Parameters and JSON. Those two stay in sync. Reset Parameters still reloads the procedure parameters and keeps the session value.
5. Simple Query and Excel Import look the same as before.

## Decisions

1. The JSON tab is editable. Form and JSON convert both ways.
2. Inner tab names are **Form** and **JSON**.
3. On Stored Procedure, Form is only `CurrentUserSessionId`. The parameter grid is a sub-tab of Payload, next to the input JSON, because those defaults are the call body.
4. Simple Query and Excel Import do not change.
5. Data Model Form is only `CurrentUserSessionId` and `Id`.
6. Search has no Payload tab. The server runs a search only on Get. Post does not execute the search.
7. The right column title is **Result**.
