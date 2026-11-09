# Generic AI Agent Platform — Vector Store Architecture

**Status:** Implementation in progress (2026-11-09). Schema V075 (`Kind`/`SourceKey`). Agent lazy trains **only the current Entity**. Native SQL `VECTOR_DISTANCE` acceleration not yet filled.  
**Date:** 2026-11-09  
**Audience:** Senior developers, backend engineers  
**Related:**
- `SharedAgentEngine-Architecture.md` (§3.6 tool catalog Embedding seam, §12 RAG backlog)
- `SharedAgentEngine-Memory-Architecture.md` (episode VARBINARY + in-process cosine; reuse embedding client ideas)
- `TransactionForm-AiAgent-Plan.md` (Form Agent coerce / `FormHostWriteBL.ResolveLookup`)
- `Document/Design/RAG-Learning-Guide.md` (conceptual RAG)
- PLM reference: `C:\Dev\PLM3\PLM` (`LLMServiceBL`, `IVectorRepository`, `AITrainingBL.InsertEntity`, `2026_10_05_VectorDBMigration.sql`)

**Scope:** Tenant-DB vector tables, train/query lifecycle, in-memory cache when SQL lacks native vector search, and the first consumer — **Entity Lookup semantic resolution** for AI Agent form/search field writes. Image and other corpora are extension seams only in this revision.

**Non-goals (this revision):**
- A separate vector product (Qdrant, Pinecone, Azure AI Search, …)
- Replacing episode memory (`AppAgentEpisode`) or MCP Swagger semantic search
- Implementing Tool Catalog RAG (`AppAgentToolCatalog.Embedding`) — may later share the same store APIs
- Shipping image similarity business flows (table + API stubs only)

---

## 1. Why

AI Agents write Transaction Form / Search fields that bind to **Entity Lookup Items**. The stored value is the lookup **Id**; the UI shows **Display**. Users often supply a string that is only *semantically* close to a Display (e.g. `"中国CN"` vs `"China"`). Exact match in `FormHostWriteBL.ResolveLookup` is not enough.

PLM already stores vectors in SQL (`collection` / `embedding` / `imageEmbedding`) and trains entity collections as `entity-{id}` with `document` = Display and `metadata` = `{"Id":…}`. AppAI mirrors that **shape and lifecycle**, with differences called out in §8.

---

## 2. Decisions (locked)

| Topic | Decision |
|-------|----------|
| Storage location | **APP Tenant DB** tables (not a separate Vector DB catalog / connection string) |
| Physical vector column | **`VARBINARY(MAX)` is authoritative** on all tenants |
| SQL Server 2025+ | **Optional acceleration**: project/use native `vector` + `VECTOR_DISTANCE` when available |
| Older SQL | Load vectors from tables into **in-process memory cache**; cosine search in CLR |
| Model isolation | Every collection/row carries **`ModelProvider` + `EmbeddingModel`**; vectors are never compared across models |
| Multi-company column | **No `company_code`** — tenant database already isolates |
| First consumer | Unified **Entity Lookup semantic resolver**; Form Agent first; Search write path reserved |
| Resolve waterfall | **Exact → vector** (no PLM-style fuzzy middle layer in v1) |
| Hit policy | **Top-1 + similarity threshold** (per embedding model); below threshold → unresolved |
| Train on change | Mark/rebuild when entity lookup content changes (`ContentFingerprint` + write-path stale when available) |
| Train lazy | Agent request: **train only the Entity being resolved** if missing/stale. Full-tenant `EnsureAllReferencedEntities` is not on the request path (token cost). |
| Image table | Create **`AppVectorImageEmbedding` empty** in the same migration; no business callers yet |

---

## 3. Relationship to existing embedding code

| Existing | Role vs this design |
|----------|---------------------|
| `EmbeddingClient` (Agent Memory) | Reuse / extend for text embed (Gemini ~768, OpenAI ~1536). Lookup store must record the same `ModelProvider`/`EmbeddingModel` string family. |
| `AppAgentEpisode.Embedding` | Unrelated L3 memory; do not mix tables. Patterns (unit-normalize, `ToBytes`/`FromBytes`, per-model threshold) are reusable. |
| MCP `IEmbeddingProvider` / `SemanticSearchService` | Swagger endpoint RAG; separate index. May later call shared embed helpers only. |
| `AppAgentToolCatalog.Embedding` | Still reserved for tool RAG; future implementation should prefer `IAppVectorStore` rather than a third ad-hoc store. |

---

## 4. Schema (Tenant DB)

Naming uses an `AppVector*` prefix. Column types below are logical; migration may use `NVARCHAR` for JSON if `json` type is unavailable on older servers.

### 4.1 `AppVectorCollection`

| Column | Type | Notes |
|--------|------|--------|
| `Id` | int identity PK | |
| `Name` | nvarchar(200) | Display name, e.g. `entity-{EntityInfoID}` |
| `Kind` | nvarchar(40) | Corpus class: `entity` (v1), later `help` / `user` / `rag` / `image` |
| `SourceKey` | nvarchar(200) | Business key within Kind (EntityInfoID as string) |
| `Dimension` | int | 768 / 1536 / … |
| `ModelProvider` | nvarchar(80) | e.g. `openai`, `gemini` |
| `EmbeddingModel` | nvarchar(120) | e.g. `text-embedding-3-small`, `gemini-embedding-001` |
| `ContentFingerprint` | nvarchar(128) null | Hash of lookup Id+Display set (see §6) |
| `UpdatedAt` / `CreatedAt` | datetime2 | |

**Unique:** `(Kind, SourceKey, ModelProvider, EmbeddingModel)` where `SourceKey` is not null. Entity lookup stays one collection per EntityInfoID; help/users are later collections with other `Kind` values, not mixed into entity buckets.

### 4.2 `AppVectorEmbedding`

| Column | Type | Notes |
|--------|------|--------|
| `Id` | int identity PK | |
| `CollectionId` | int FK | → `AppVectorCollection` |
| `Document` | nvarchar(max) | Indexed text; for entities = `LookupItemDto.Display` |
| `VectorData` | **varbinary(max)** | Authoritative float32 (or agreed) bytes; **required** |
| `Dimension` | int | Must match collection / model |
| `Metadata` | nvarchar(max) / json | e.g. `{"Id":"2"}` — **Id always string** (supports int and string PKs) |
| `ModelProvider` | nvarchar(80) | Denormalized; must match collection |
| `EmbeddingModel` | nvarchar(120) | Denormalized; must match collection |
| `CreatedAt` | datetime2 | |

Optional (SQL 2025+ only, same migration branch or later alter):

| Column | Type | Notes |
|--------|------|--------|
| `VectorNative768` | `vector(768)` null | Acceleration copy when Dimension=768 |
| `VectorNative1536` | `vector(1536)` null | Acceleration copy when Dimension=1536 |

Native columns are **never** the only copy of the truth. Insert/update always writes `VectorData` first; native columns are filled when the server supports them.

### 4.3 `AppVectorImageEmbedding`

Same pattern as text embedding, plus image-specific binary columns (VAR BINARY authoritative), e.g. `ImageVectorData` + `ImageDimension`, optional native `image_vector*` later. **No writers/readers in this revision** beyond schema + repository stubs.

### 4.4 Migration policy

1. Always create the three tables with **VARBINARY** vector columns (works on supported AppAI SQL baselines).
2. At runtime (or a guarded migration step): if `SERVERPROPERTY('ProductVersion')` major ≥ 17 **and** native vector is enabled for the tenant, add/populate native columns and set query mode = `SqlNative`.
3. Else query mode = `InMemoryCache`.

Do **not** abort tenant migrations solely because the server is &lt; 2025 (unlike PLM’s VectorDB script).

---

## 5. Runtime architecture

```
                    ┌─────────────────────────────┐
  Embed text  ────► │ EmbeddingClient (extended)  │
                    └──────────────┬──────────────┘
                                   │ float[] + ModelProvider/Model
                                   ▼
┌──────────────────────────────────────────────────────────────┐
│ IAppVectorStore                                              │
│  EnsureEntityCollectionsAsync / UpsertDocuments / Search     │
│  DeleteCollection / GetFingerprint / ListMissingEntityNames  │
└───────────────┬────────────────────────────┬─────────────────┘
                │                            │
                ▼                            ▼
┌───────────────────────────┐  ┌─────────────────────────────────┐
│ SqlNativeVectorQuery      │  │ InMemoryVectorCache             │
│ VECTOR_DISTANCE on native │  │ Load VectorData by collection   │
│ columns when available    │  │ Cosine Top-K in process         │
└───────────────────────────┘  └─────────────────────────────────┘
                │                            │
                └────────────┬───────────────┘
                             ▼
              AppVectorCollection / AppVectorEmbedding
                         (Tenant DB)
```

**Rules:**

- Search and upsert **require** an explicit `ModelProvider` + `EmbeddingModel` (from tenant AI config used for the agent run).
- Cache key includes `(CollectionId or Name, ModelProvider, EmbeddingModel)`. Invalidate on upsert/delete/fingerprint change.
- Cache is optional acceleration/compatibility; cold start reloads from `VectorData`.

Suggested types (names illustrative):

- `IAppVectorStore`
- `EntityLookupSemanticResolver` (consumer)
- `EntityVectorTrainingBL` (train/lazy/rebuild)

---

## 6. Training

### 6.1 What is indexed (Entity Lookup)

For `EntityInfoID = E`, collection name `entity-{E}`:

| Field | Source |
|-------|--------|
| `Document` | `LookupItemDto.Display` |
| `Metadata` | `{"Id":"<string form of LookupItemDto.Id>"}` |
| Vectors | Embed(`Display`) with current provider/model |

`FirstField` / pipe segments are **not** separate documents in v1; exact match already covers them. If Display is empty, skip or use `Convert.ToString(Id)` (document that choice in implementation notes).

Load items via existing `AppEntityInfoBL.GetLookupItemList(entityInfoId, "")` (security rules unchanged).

### 6.2 ContentFingerprint

Compute a stable hash over sorted pairs `(IdString, Display)` for the lookup list (UTF-8, normalized newlines). Store on `AppVectorCollection`.

- If collection missing for `(Kind=entity, SourceKey, ModelProvider, EmbeddingModel)` → train.
- If fingerprint ≠ current → **rebuild** that collection (delete rows + re-embed).

### 6.3 When to train

| Trigger | Behavior |
|---------|----------|
| **Entity data change** | When fingerprint changes (detected on Ensure) and/or when a known write path invalidates the entity lookup cache, mark collection stale and rebuild for the current model. |
| **Agent lazy (blocking, current entity only)** | If the field’s Entity collection is missing or fingerprint-stale, train **that EntityInfoID only**, then search. `EnsureAllReferencedEntities` stays for a future admin/background job and is **not** called from Agent requests. |

### 6.4 Batch embed

Follow PLM practice: batch texts (e.g. 100) per embedding API call; bulk insert into `AppVectorEmbedding`.

### 6.5 Model change

If tenant switches embedding provider/model, old collections remain but are **not** used. Lazy/train creates a **new** collection key for the new model. Optional later: GC unused model collections.

---

## 7. Query and Entity Lookup resolution

### 7.1 Waterfall

```
userText (field update value)
    │
    ▼
Exact ResolveLookup  (Id / Display / FirstField / LastField / | segments)
    │ hit → return Id
    │ miss
    ▼
Lazy ensure (block: train this EntityInfoID if missing/stale)
    │
    ▼
Embed(userText) → Search collection entity-{EntityInfoID}
    Top-1, same ModelProvider/Model only
    │
    ▼
similarity >= threshold(model)?
    │ yes → parse Metadata.Id (string) → coerce to field value
    │ no  → LookupError / unresolved (do not guess)
```

Exact path stays in `FormHostWriteBL` (or shared helper). Vector path is only after exact miss.

### 7.2 Threshold

Per `EmbeddingModel`, configurable defaults (starting points, tune from logs):

| Model family | Starting similarity threshold (cosine, higher = closer) |
|--------------|---------------------------------------------------------|
| `openai:text-embedding-3-small` | ~0.45 (align with Memory recall starting point) |
| `gemini:gemini-embedding-001` | ~0.70 |

Store thresholds in code constants first; promote to tenant AI settings if needed. Log `score`, `document`, `metadata` on miss/hit for calibration.

**Distance vs similarity:** PLM SQL uses `VECTOR_DISTANCE` (lower better). In-memory path should use **cosine similarity** (higher better) consistently with `EmbeddingClient.Cosine` on unit vectors. Document conversion if using distance in SQL (`similarity ≈ 1 - distance` only when comparable; prefer one metric in the public API).

### 7.3 Consumers

| Consumer | Integration |
|----------|-------------|
| Transaction Form Agent | `FormHostWriteBL.ResolveLookup` / coerce after exact fail |
| Search / report criteria (later) | Same `EntityLookupSemanticResolver` when writing DDL criteria |
| Future image / RAG corpora | New collection name prefixes + `AppVectorImageEmbedding` |

### 7.4 Id types

`LookupItemDto.Id` is `object` (mostly int; also string PKs, etc.). Metadata always stringifies Id. Resolver returns string Id and lets existing coerce/`Convert` apply to the field.

---

## 8. Diff vs PLM

| Area | PLM | AppAI (this design) |
|------|-----|---------------------|
| Database | Separate `VectorDBConnectionString` catalog | **Tenant DB** |
| SQL requirement | Migration aborts if &lt; 2025 | VARBINARY always; native optional |
| In-memory NN | Not production | **Required** fallback |
| Model columns | Implicit via which vector* column is filled | Explicit **`ModelProvider` + `EmbeddingModel`** |
| `company_code` | Yes | **No** (tenant isolation) |
| Entity resolve in production Integration | Often **fuzzy cache**, not vectors | **Exact → vector** |
| Lazy train | Per entity `InsertEntity` on use | Agent path: **current Entity only** |
| Image | Production Vertex multimodal | Schema stub only |

---

## 9. Extension seams (later)

- Other `Kind` values (`help`, `user`, `rag`) on the same collection table.
- `AppVectorImageEmbedding` writers (product/image similarity).
- Tool-catalog RAG via the same store APIs.
- Optional background `EnsureAllReferencedEntities` (not on Agent request path).

---

## 10. Implementation sketch (not this document’s delivery)

When coding starts (separate change):

1. Tenant migration for the three tables (+ optional native columns detection helper).
2. `IAppVectorStore` + VARBINARY ser/de + cache + optional `VECTOR_DISTANCE`.
3. `EntityVectorTrainingBL` + fingerprint + Agent lazy **current Entity only**.
4. `EntityLookupSemanticResolver` + wire `FormHostWriteBL`.
5. Tests: ser/de, cosine, fingerprint rebuild, threshold reject, string Id metadata.
6. Update Form Agent plan “implemented” notes when shipped.

**Implemented (v1):** Tenant tables (VARBINARY) + `Kind`/`SourceKey` in V075; in-memory cosine search; Agent lazy trains the **current Entity only**; Form Agent coerce. Native vector columns remain a later acceleration. Search criteria write path is still reserved.

---

## 11. Open implementation details (non-blocking for design)

Resolved at coding time without changing locked decisions:

- Whether Display duplicates collapse to one document (PLM dictionary keyed by Display — last wins); prefer stable Id in metadata and unique document key `IdString` if Displays collide.
- Native column sync strategy (same transaction as VARBINARY write vs async backfill).
