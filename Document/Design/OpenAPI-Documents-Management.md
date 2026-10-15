# OpenAPI Documents Management

状态：管理页、存储、Generator、Swagger 分组和 MCP GET 外壳已实施（2026-10-15）。读文档的 URL 现在不鉴权。`McpUsageNotes` 以及 Pivot / Chart 参数还没写入 Generator。内部 Agent 接线放到后期。

## 目的

在 API Management 里管理多份 OpenAPI Document。每份由用户选出的 API 组成，生成后存进数据库。外部通过固定 URL 读取已保存的 JSON，读取时不重新生成。

侧栏在 API Management 下，与 3rd Party API Provider、App API Provider 并列，菜单名是 OpenAPI Documents。

## 地址

已发布的 JSON：

```
GET /openapi-doc/{doc-code}
```

`doc-code` 只允许小写字母、数字和 `-`，全局唯一，可以改。保存时若为空，用 Name 生成；若不合法，先收成合法形式再保存。只有 Status = Published 才返回正文。Draft 不返回。开发地址形如 `http://localhost:52740/openapi-doc/app-weather`，路径里没有 `/appai`。

文档里的 API 地址是 `{这次请求的根}/webapi/DataIntegration/{ActionCode}`。调用这些 API 仍要带登录会话。

| 页面 | 路由 |
|---|---|
| 列表 | `/openapi-doc-management` |
| 编辑 | `/openapi-doc-editor/{id}` |

Swagger 页面是 `/appai/openapi-ui`。脚本来自项目里的 Swashbuckle，不从 CDN 加载。

## 存储

数据库是唯一来源。Download 只是导出。

`AppOpenApiDocument`：Id、Name、Code、Version、Description、`IsUsedByMcp`（`BIT NULL`，无默认值）、Status（Draft / Published）、OpenApiJson、ApiCount、LastGenerated，以及 `AppCreatedByID`、`AppCreatedDate`、`AppModifiedByID`、`AppModifiedDate`。

`AppOpenApiDocumentMember`：Id、DocumentId、ActionCode。不建外键，不存 API 的 Id。同一 Document 内 ActionCode 不重复。Save 整份替换成员列表。Regenerate 按 ActionCode 找当前 API；找不到就跳过并记下原因。

这三列在 V051：Document.`IsUsedByMcp`，以及 API 上的 `McpUiHint`、`McpUsageNotes`。已有行保持 `NULL`。

## 列表和编辑

列表列：Actions（Edit、Delete）、Id、Name、Code、Version、API Count、Status、Is Used By MCP（`NULL` 显示为未勾选）、Public URL、Last Generated、Description、Created By、Created Date、Modified By、Modified Date。双击行等于 Edit。

编辑页顶栏：Name、只读 Version，然后是 Refresh、Save & Generate OpenAPI Doc、Download JSON、View Swagger。后两个在还没有 JSON 时禁用。

属性第一行：Name、Code、Description、Is Used By MCP、Is Published。Is Published 在最右边，不另起一行。Published URL 在下一行，只在库里已发布时显示，旁边是 Copy、Download。

左边是已选 API：ActionCode、Http Method、来源（App API 写 API Type，3rd Party 写 Provider Name）、Description。Add APIs 有两个 Tab：App API Provider、3rd Party Provider。Excel Import 不出现。右边是生成后的 JSON。跳过的 API 显示在表下方。

Save & Generate OpenAPI Doc 同时保存名称、Code、成员、Is Published，并生成 JSON。Version 最后一段加 1（空则先写成 `1.0.0` 再加）。已发布的 URL 立刻返回这份新 JSON。

Document 勾选 Is Used By MCP 时，Add APIs 只列出 API 上 `IsUsedByMcp = true` 的行。保存时再删掉已经不是 `true` 的成员，并在编辑页列出被删的 ActionCode。取消勾选后，当时的成员保留。生成时若 API 已取消勾选，跳过，原因是 “Not used by MCP”。

## Generator

输出 OpenAPI 3.0.0。每个成员一个 operation，path 一律 `/DataIntegration/{ActionCode}`。`operationId` 和 summary 用 ActionCode。schema 放在 `components.schemas`，名为 `{ActionCode}_Request`、`{ActionCode}_Response`。一条失败只跳过该条。同一 path + method 只留一条。

库里的 JSON 不写死主机。读出或 Download 时，用这次请求的根地址加 `/webapi` 填 `servers[0].url`。

每个 operation 只挂一个 tag，避免 Swagger 把同一 API 画两次。

- 3rd Party：tag 是 Provider Name，下面直接是 API。
- App API：tag 是 `App API Provider » {API Type}`。Swagger 页面收成三层：Provider、Type、API。

不按 PLM 的 ApiScope 套。不把同一 API 写成内部路径和发布路径两条。不用 `additionalProperties: true`。Swagger 因此不会生成 `additionalProp1`。对象列出已知属性，并设 `additionalProperties: false`。没有属性时是空 object。

样本转 schema 时，按值的类型写属性。Swagger 的 Example Value 用 schema 上的 `example`。没有 `example` 时，Swagger 会用类型名（例如 `"string"`）填示例，所以有已保存响应时必须把那份 JSON 写成 `example`。

### 响应存在哪

| API | GET / DELETE 的响应 | POST / PUT 的 Payload | POST / PUT 的响应 |
|---|---|---|---|
| SQL Json Query | `JsonSampleData` | `JsonSampleData` | `PostResponseDto.ResponseJsonData` |
| Report & View（Search） | `JsonSampleData` | `JsonSampleData` | `PostResponseDto.ResponseJsonData` |
| Data Model | `JsonSampleData` | `JsonSampleData` | `PostResponseDto.ResponseJsonData` |
| Stored Procedure | `JsonSampleData` | 参数网格 / 入参 JSON | `JsonSampleData` |
| 3rd Party | `JsonSampleData` | `JsonSampleData` | `PostResponseDto.ResponseJsonData` |

测试调用成功后写回上表对应字段，重新打开仍然显示。Data Model 的 Re-generate Payload 只改 `JsonSampleData`，不改 Result。

### 每种 API 的 schema

| 成员 | 怎么认 | Request | Response |
|---|---|---|---|
| SQL Json Query | `IsSimpleQuery` | GET/DELETE 的 query 来自 `SimpleQueryParameterNameList`，去掉开头的 `@`。POST/PUT body 用 `JsonSampleData`。SQL 含 `@json` 时才把 body 传给 `@json` | GET/DELETE：有已保存响应时，用它写 schema 和 example。没有时，从 `FOR JSON` 前的 SELECT 列名写 string 属性；有 `WITHOUT_ARRAY_WRAPPER` 时是 object，否则是数组。POST/PUT 运行时返回一段文本 |
| Report & View | `TranscationFieId` 指向 AppSearch。运行时只有 GET 会执行 Search | GET 的 query 来自 `QueryParams`，键是条件的 `SysTableFiledPath`。空值用该字段的 DefaultValue，并写入参数的 default 和 example | GET 是行数组。属性名是默认 View `Columns` 的 `SysTableFiledPath`。子 View 在 `{Display去空格}_{Id}` 下，值是数组。树行另有 `TreeNodeId`、`TreeNodeDisplay`、`Children`。example 用已保存的 GET 响应。没有样本时 MCP 的 `data` 为 `[]` |
| Data Model | `TranscationId` 指向 AppTransaction | GET/DELETE 的 query 来自 `QueryParams`，没有时 GET 默认 `id`。POST/PUT body 优先用 `JsonSampleData`，没有时用 Transaction 字段 | GET/DELETE 按 Transaction 字段写属性、类型和 nullable。POST/PUT 响应优先用已保存的 `ResponseJsonData`，没有时用同一套字段 |
| Stored Procedure | `APIConfigParameters.IsStoredProcedureApi` | body 是参数对象本身，例如 `{ "tabId": 3992 }`，不再包一层 `args`。参数来自 `SpParameters` 的 IN / INOUT，名字去掉 `@`。`int` 为 integer，`decimal` / `float` 为 number，`bit` 为 boolean，其余为 string。Default Value 写入 example；空或 `null` 则 nullable 且 example 为 null。GET 的 query 是这些同名参数 | 优先用已保存的 `JsonSampleData`。没有样本时：普通 API 是行数组；有 OUTPUT 时是 `{ rows, outputParameters }`。MCP 是 `ok`、`IsSuccess`、`dataSourceId`、`procedure`、`columnNames`、`rowCount`、`truncated`、`rows`、`outputParameters` |
| 3rd Party | `IntergrationSettingId` 不是 1 | GET/DELETE 的 query 来自 `QueryParams`。POST/PUT body 用 `JsonSampleData` | GET 用 `JsonSampleData`。POST/PUT 用 `ResponseJsonData`。没有样本时是空 object |

Excel Import 不进入选择器，也不生成 operation。

运行时：普通 Stored Procedure API 只返回行数组（有输出参数时带 `outputParameters`，失败时 `{ error }`）。`IsUsedByMcp = true` 的 SP 仍返回完整 ExecuteJson 外壳。Agent 自己的 ExecuteJson 不改。

## MCP

一份 Document 只存一份 JSON。公开地址仍是 `GET /openapi-doc/{doc-code}`。不另做第二份，也不另开 `/openapi-doc/{doc-code}/mcp`。

API 上只有 `IsUsedByMcp = true` 算 MCP。`null` 和 `false` 都不算。

API 三列都在 `AppIntergrationSettingParameter`，用现有 `UnmappedColumns` 读写，不进 `APIConfigParameters`。`GetValue` 为 null 时不更新该列。空字符串写成 NULL，用来清空。

| 列 | 类型 | 含义 |
|---|---|---|
| `IsUsedByMcp` | `BIT NULL` | 只有 `true` 进入勾选了 MCP 的 Document |
| `McpUiHint` | `NVARCHAR(100) NULL` | 空，或 `SelectorGrid`、`DateRangeFilter`、`RecordCard`、`FlexGrid`、`ActionMenu`、`Form`、`ChartView`、`PivotTable`、`DataAnalysis` |
| `McpUsageNotes` | `NVARCHAR(MAX) NULL` | 给模型看的用法说明 |

五个编辑页（Data Model、Search、3rd Party、SQL Json Query、Stored Procedure）在勾选 Is Used By MCP 后显示 Hint 和 Notes。标签宽度与上面的标签相同，输入框单行等高。勾选框在表头行最右。Stored Procedure 的这两项在上方边框内。Excel Import 不加。取消勾选后隐藏，已存的值保留。

### GET 外壳

只在该 API 的 `IsUsedByMcp = true` 时，Search 和 Data Model 的 GET 包一层。`null` 和 `false` 保持原来的 JSON。正文为空时不包。

```json
{
  "fields": [
    { "name": "BodyPartId", "controlType": "Numeric", "entityId": null }
  ],
  "data": {}
}
```

`controlType` 是 `EmAppControlType` 的名字。`entityId` 只在大于 0 时写出，否则 null。行里的值不改。

| API | `fields[].name` | `data` |
|---|---|---|
| Search | 默认 View 列的 `SysTableFiledPath`。子 View 用 `{Display去空格}_{Id}.{SysTableFiledPath}` | 原来的行数组，含 `Children` |
| Data Model | 与 GET JSON 的字段路径相同。根字段用自己的路径。Sibling 前缀是单元 `JsonPath`。子表前缀是子单元 `JsonPath` | 原来的 Master Detail 或 List 对象 |

Data Model 的 POST / PUT 用来保存。Re-generate Payload 再调 GET，并带 `forPayload=1`。这次 GET 不当作 MCP，返回普通表单 JSON。这个参数不写入 OpenAPI。Send Request 和不带这个参数的调用仍返回外壳。

OpenAPI：MCP 的 Search / Data Model GET 写成这个外壳。`fields` 的 example 用真实列名和控件类型，不用 `"string"`。`data` 的 example 用已保存的 GET 响应；没有响应时 Search 的 `data` 为 `[]`。Search 列属性带 `x-ui-hint` 和 `x-entityid`（有实体时）。Data Model 的 `data` 按字段写类型和 `x-ui-hint`。POST / PUT 的 request body 仍是表单 JSON，不是外壳。

### 还没做

只在 Document 的 `IsUsedByMcp` 为 true 时才做，现在 Generator 还没写：

- `McpUsageNotes` 追加到 `description`，与 ActionDescription 之间空一行。
- `McpUiHint = PivotTable` 时增加 `pivot_row_field`、`pivot_column_field`、`pivot_measure_field`、`pivot_aggregation`（默认 `count`，取值 `sum` 或 `count`）。
- `McpUiHint = ChartView` 时增加 `chart_x_label_field`、`chart_y_value_field`、`chart_group_by_field`。
- GET / DELETE 放 query，POST / PUT 放 request body。其它 Hint 不加参数。

不写 `x-mcp-route`。不搬 PLM 的 `SealMcpResponse`。

## 调用鉴权

读 Document：`GET /openapi-doc/{doc-code}` 现在不鉴权。以后再闭口。

调用文档里的 API：`DataIntegration` 认请求头 `CurrentUserSessionId`（或同名 Cookie）。`IntergrationAccessToken` 只用于 `/mcp`，这条 API 还不认它。

OpenAPI 里写一个 apiKey scheme，名字 `CurrentUserSessionId`，`in: header`。每个 operation 都带上。文档里不写真实 session。Swagger 的 Execute 从页面地址的 hash 带上当前登录会话，不写进公开 URL。
