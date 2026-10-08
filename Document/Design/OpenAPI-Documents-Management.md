# OpenAPI Documents Management — 初步设计

状态：列表、路由、存储和 OpenAPI Document Generator 已按 PLM Swagger 生成方式整理。表结构、选择界面和鉴权方案仍待下一步确认。

## 目的

在 API Management 中管理多份 OpenAPI Document。每份 Document 由用户选出的多个 API 组成，生成一次后存入数据库。外部和内部 AI 通过固定 URL 读取已保存的 JSON，调用时不重新生成。

示例：

- 从 3rd Party API Provider 里选出查天气的 API，生成一份 App Weather API。
- 从 App API Provider 里选出与 PLM 相关的 Stored Procedure API、Report & View API、Data Model API，合成一份 Document。

可以再次打开同一份 Document，重新生成并更新已保存的 JSON。

## 已定名称

| 用途 | 名称 |
|---|---|
| 侧栏菜单（API Management 下） | OpenAPI Documents |
| 页面标题 | OpenAPI Documents Management |
| 一条记录 | OpenAPI Document |
| 生成并保存的内容 | OpenAPI Document（JSON） |

侧栏与现有两项并列：

- 3rd Party API Provider
- App API Provider
- OpenAPI Documents

## 已定地址与路由

对外读取已发布的 JSON：

```
GET /openapi-doc/{doc-code}
```

`doc-code` 是 Document 的 Code。只允许小写字母、数字和 `-`，全局唯一，创建后不改。开发时完整地址是 `http://localhost:52740/openapi-doc/app-weather`。发布后是调用方访问这台服务器时用的同一根地址加 `/openapi-doc/{doc-code}`，不写 `/appai`。

仅 Status = Published 时返回已保存的 JSON，未登录的外部 AI 也可以读。Draft 不返回正文。这个 URL 只公开文档。文档里的 API 地址是 `{本次请求的根}/webapi/DataIntegration/{ActionCode}`，调用仍要登录或 Integration Token。

页面路由：

| 页面 | 路由 |
|---|---|
| 列表 | `/openapi-doc-management` |
| 编辑 | `/openapi-doc-editor/{id}` |

## 存储

数据库是唯一来源。对外 URL 从数据库读出已保存的 OpenAPI JSON。Download 只是导出，不作为运行时来源。

重新生成只在用户执行 Regenerate 时发生，并更新该记录。Save 只保存名称、代码、成员等，不重新生成 JSON。

## 列表页

标题：OpenAPI Documents Management。版式与 3rd Party API Provider 列表相同：顶栏标题和按钮，下面一张表。

顶栏按钮：Refresh、Create。

列：

| 列 | 说明 |
|---|---|
| Actions | 行菜单 |
| Id | |
| Name | 例如 App Weather API |
| Code | URL 中的 `doc-code`，创建后不改 |
| Version | OpenAPI `info.version` |
| API Count | 包含的 API 个数 |
| Status | Draft / Published |
| Public URL | 已发布时为 `/openapi-doc/{doc-code}` |
| Last Generated | 上次生成 JSON 的时间 |
| Description | |
| Created By | `AppCreatedByID`，界面用用户 DataMap 显示用户名 |
| Created Date | `AppCreatedDate` |
| Modified By | `AppModifiedByID`，界面用用户 DataMap 显示用户名 |
| Modified Date | `AppModifiedDate` |

审计四列与 Database Management 中 “Created By & Modified By” 自动添加的列一致，Entity 使用同名字段：

| 字段 | 类型 |
|---|---|
| `AppCreatedByID` | int |
| `AppCreatedDate` | datetime |
| `AppModifiedByID` | int |
| `AppModifiedDate` | datetime |

行菜单（初步）：Open、Regenerate、Copy URL、Download、Publish / Unpublish、Delete。双击行等于 Open。Regenerate 更新 `AppModifiedByID` 和 `AppModifiedDate`。Last Generated 用来区分“只保存了成员或名称”和“重新生成了 JSON”。

## 编辑页（初步）

路由 `/openapi-doc-editor/{id}`。标题用 Name。

属性：Name、Code（保存后只读）、Version、Description、Status、Public URL（只读，可复制）。

已选 API 区域的列和按钮、Add APIs 的筛选方式，等待下一步设计。

初步按钮：Add APIs、Remove、Save、Regenerate、Download、Copy URL。

Save 只保存 Document 和成员。Regenerate 调用下面的 Generator，把 JSON 写回该记录，并更新 Last Generated、API Count、`AppModifiedByID`、`AppModifiedDate`。已发布的 URL 立刻返回这份新 JSON。每次 Regenerate 把 Version 的最后一段加 1（`1.0.0` 变为 `1.0.1`）；Version 为空时先写成 `1.0.0` 再加。Publish 不重新生成，也不改 Version；没有已保存 JSON 时不允许 Publish。

一份 Document 可以同时包含 3rd Party API 和 App API。

第一版除了管理页面和对外 URL，还要把已发布的 Document 交给内部 Agent 使用。Agent 读的是这份已保存的 JSON，不在调用时重新生成。

## PLM 的 Swagger 是怎么生成的

参考代码在 `C:\Dev\PLM3\PLM\PlmAGApplication\Server\WebApi\`：

- `SwaggerGenerator.cs`：拼出 OpenAPI 3.0.0 JSON
- `SwaggerController.cs`：全部 Data Exchange API
- `McpSwaggerController.cs`：只含 `IsMcpApi = true` 的 API

每次 `GET /webapi/Swagger/json` 或 `GET /webapi/McpSwagger/json` 都重新生成，不把 JSON 存进数据库。

生成步骤：

1. 新建文档：`openapi: 3.0.0`，`info.title` / `info.version`，`servers[0].url` 为当前站点加 `/webapi`。
2. 读出 API 配置列表。Swagger 用全部 `PdmWebApidataExchangeSetting`。McpSwagger 只用 `APIConfigParameterDTO.IsMcpApi = true`。
3. 每条 API 按 `ApiScope` 做 request / response schema，再 `AddDynamicAction`。
4. 一条 API 失败只跳过该条，不让整份文档失败。
5. 按操作出现顺序写 `tags`，并把 `Published API` 放在最前。

一条 API 变成一个 operation：

| OpenAPI 字段 | PLM 来源 |
|---|---|
| path | 无 UrlPattern 时 `/{ControllerSegment}/{ActionCode}`，默认段名 `DataExchange`。有 UrlPattern 时用 `/{UrlPattern}` |
| method | `HttpMethd`，空则 GET |
| operationId | 默认 `{segment}_{ActionCode}`；发布路径用 UrlPattern 去掉 `/`、`-`、`.` 后的字符串 |
| summary | ActionCode |
| description | ActionDescription；McpSwagger 再附上 Prompt |
| tags | 内部路径用 `DataExchange`；发布路径用 `Published API` |
| parameters | query / path 参数 |
| requestBody | POST/PUT 的 request schema，`$ref` 到 `components.schemas` |
| responses | 200 加 response schema；另外固定 400、500 |

Schema 按 `EmApiScope`：

| Scope | Request | Response |
|---|---|---|
| ReferenceTab | Tab 布局（POST/PUT） | Tab 布局；Delete 为 string |
| JsonQuery | `JsonSampleData` 推断 | 优先 `JsonSchema`，否则 `PostProcessScript` 样本，否则通用 object |
| SearchView | `JsonSampleData`；没有则为空 object | Search 列定义 |
| DataSourceEntity | Update/Delete 用实体列或样本 | Update/Delete 固定结果；按行读取用实体列 |
| SystemBuiltInApi | POST/PUT 的 `JsonSampleData` | `JsonSchema`，否则样本，否则 `additionalProperties: true` |

发布在 PLM 里是每条 API 自己的 `UrlPattern`，不是一份独立文档。运行时把 `/webapi/{UrlPattern}` 转到 `/webapi/DataExchange/{ActionCode}`。

两套 UI 对发布路径的处理不同：

- Swagger：内部路径和发布路径各写一条 operation。
- McpSwagger：已有 UrlPattern 时只写发布路径，tag 为 `Published API`，避免同一操作出现两次。

## 我们的 Generator 采用什么、不采用什么

采用：

- 输出 OpenAPI 3.0.0。
- 每个选中的 API 变成一个 operation：path、method、operationId、summary、description、parameters、requestBody、responses。
- Schema 放在 `components.schemas`，名字为 `{ActionCode}_Request`、`{ActionCode}_Response`。
- Schema 从该 API 已保存的样本、JsonSchema、参数定义推断，不在生成时调用外部接口。
- 一条成员失败就跳过，并在生成结果里返回跳过原因。
- 文档里每个 API 只出现一条可调用 path。对应 McpSwagger 只保留发布路径的做法。

不采用：

- 不在每次 GET 时重新生成。JSON 在 Regenerate 时写入数据库，对外 URL 只读这份 JSON。
- 不做“全部 API 一份文档”。成员由用户选择。
- 不用每条 API 的 UrlPattern 当作发布。发布的是整份 Document，地址是 `/openapi-doc/{doc-code}`。
- 不扫描 Controller。成员只来自已保存的 3rd Party API 和 App API。
- 不把诊断用的 `x-meta` 写入保存的 JSON。

## OpenAPI Document Generator

输入是一份 OpenAPI Document 和它的成员列表。输出是 OpenAPI JSON，写回该 Document。

文档头：

| OpenAPI | 来源 |
|---|---|
| `info.title` | Name |
| `info.version` | Version，空则 `1.0.0` |
| `info.description` | Description |
| `servers[0].url` | 发出这份 JSON 的这次请求的根地址 + `/webapi`。不读 `BASE_URL`，不读 ApplicationURL。path 是 `/DataIntegration/{ActionCode}` |

成员来源是现有 `AppIntergrationSettingParameter`。3rd Party API 属于某个 Integration Setting。App API Provider 使用 Integration Setting Id = 1，类型包括 Stored Procedure、SQL Json Query、Data Model、Report & View。

每个成员生成一个 operation：

| OpenAPI 字段 | 规则 |
|---|---|
| path | 一律 `/DataIntegration/{ActionCode}`，对应 `DataIntegrationController`。3rd Party API 已经包装成我们自己的 API，对外 path 不用 Provider 的 Url，也不单独写 BaseUrl |
| method | `HttpMethd`，空则 GET |
| operationId | `{ActionCode}`，同文档内重复时加来源前缀 |
| summary | ActionCode |
| description | ActionDescription |
| tags | API Type。3rd Party 再用 Provider Name 做第二组 tag |
| parameters | 按下面各类型分别取。不从 3rd Party 的外部 URL 拆 path 参数 |
| requestBody | POST/PUT：schema `$ref` `#/components/schemas/{ActionCode}_Request` |
| responses.200 | schema `$ref` `#/components/schemas/{ActionCode}_Response` |
| responses.400 / 500 | 固定 Bad Request、Internal Server Error |

同一 path + method 只保留一条。后加入的成员若冲突，生成时记为跳过。

每种 API 都按 App 自己的运行时来写 schema，不按 PLM 的 ApiScope 套。

| 成员 | 怎么认 | Request | Response |
|---|---|---|---|
| SQL Json Query | `IsSimpleQuery`。运行时执行 `JsonQuery`。编辑器不维护 `JsonSchema` | GET/DELETE：query 参数来自 `SimpleQueryParameterNameList`，去掉开头的 `@` 后作为参数名。SQL 里要有同名的 `@参数`。POST/PUT：body 用 `JsonSampleData`。运行时只有 SQL 含 `@json` 时，才把整个 body 传给 `@json` | GET/DELETE：取结果第一行里名为 `JSON` 的列，内容由 SQL 自己的 FOR JSON 决定。没有保存响应样本，schema 写成不限定结构的 JSON。POST/PUT：运行时返回一段文本，不是查询结果 |
| Report & View | `TranscationFieId` 指向 AppSearch。运行时只有 GET 会执行 Search | GET：query 参数来自 `APIConfigParameters.QueryParams`。键是 Search 条件的 `SysTableFiledPath`，有值才套进条件，类型按 string。POST 在编辑器里可以发 `JsonSampleData`，但运行时不会执行 Search | GET：JSON 数组。每一行的属性名是默认 View 列的 `SysTableFiledPath`；子 View 嵌在 `{Display}_{Id}` 下；树行有 `Children`。生成时读这个 Search 的默认 View 来写 schema。`JsonSchema` / `JsonSampleData` 只是某次 Send Request 的副产品，Reset Config 会清掉，不作为来源 |
| Data Model | `TranscationId` 指向 AppTransaction。运行时按 MasterDetail 或 List 分别处理，不对应 PLM DataSourceEntity | GET：query 参数来自 `APIConfigParameters.QueryParams`，新建时默认 `id`。POST：body 用 `JsonSampleData`（编辑器里的 Post Payload，可由 GET 结果再生成） | 编辑器不保存响应样本。schema 用通用 object |
| Stored Procedure | `APIConfigParameters.IsStoredProcedureApi`。运行时 `ExecuteStoredProcedureApi`，不对应 PLM scope | POST body 固定为 `{ "args": { 参数名: 值 } }`。参数来自 `SpParameters` 的 IN / INOUT。GET 把 query string 收进同一个 `args` | 用已保存的 `JsonSampleData` 推断；没有则为通用 object |
| 3rd Party API | `IntergrationSettingId` 不是 App API Provider（Id = 1）。运行时 GET 用 query 覆盖 `QueryParams`，再由包装层去调外部 URL | GET/DELETE：query 参数来自 `APIConfigParameters.QueryParams`。POST/PUT：body 用 `JsonSampleData`（编辑器里的 Payload Data，JSON 类型） | GET 用 `JsonSampleData`。POST/PUT 用 `PostResponseDto.ResponseJsonData`。没有样本则为通用 object |

Excel Import Data Update（`ExcelDataImportDataSetId`）若被选入：POST body 用 `JsonSampleData`，列为导入源列；响应是导入结果字符串。它不对应任何 PLM scope。

已保存的 JSON 样本需要转成 schema 时：object 的每个属性按值的类型写；数组写成 `array`；解析失败则 `{ "type": "object", "additionalProperties": true }`。SQL Json Query 的 GET 响应和 Report & View 的 GET 响应不用这条，规则见上表。

生成结果除 JSON 外返回：成功条数、跳过列表（ActionCode + 原因）。编辑页在 Regenerate 后显示跳过列表。API Count 只计写入 paths 的条数。

## 待补充

- 表结构、Entity，以及 Document 与 `AppIntergrationSettingParameter` 的成员关系怎么存。迁移号避开已占用的 V046、V047
- Add APIs 的界面和筛选（Provider、API Type、Application）
- 内部 Agent 具体接在 Generic Agent 的工具上，还是作为 MCP Gateway 的一个 ApiSource
- 文档里如何写调用 DataIntegration 所需的鉴权（会话或 `IntergrationAccessToken`）。读文档的 URL 本身不鉴权
- Data Model 的响应目前没有保存样本。第一版用通用 object

