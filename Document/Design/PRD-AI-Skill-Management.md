# PRD: AI Skill Management

## Overview

A CRUD management module that allows users to define, store, and manage AI skill prompts in the database. Skills serve as reusable system prompt templates that can be loaded by AI agents (e.g., DbGenie, future agents). Each skill has a primary markdown content body and can reference multiple supplementary markdown files.

---

## Database Schema

### Table: `AppAISkill`

```sql
CREATE TABLE [dbo].[AppAISkill] (
    [SkillId]       INT            IDENTITY(1,1) NOT NULL,
    [Name]          NVARCHAR(255)  NOT NULL,
    [Description]   NVARCHAR(1000) NULL,
    [SkillContent]  NVARCHAR(MAX)  NULL,          -- primary markdown prompt body
    [IsActive]      BIT            NOT NULL DEFAULT 1,
    [CreatedDate]   DATETIME       NOT NULL DEFAULT GETDATE(),
    [UpdatedDate]   DATETIME       NULL,
    CONSTRAINT [PK_AppAISkill] PRIMARY KEY ([SkillId])
);
```

### Table: `AppAISkillRef`

```sql
CREATE TABLE [dbo].[AppAISkillRef] (
    [RefId]         INT            IDENTITY(1,1) NOT NULL,
    [SkillId]       INT            NOT NULL,
    [FileName]      NVARCHAR(255)  NOT NULL,      -- display name / file label
    [FileContent]   NVARCHAR(MAX)  NULL,          -- markdown content of reference
    [SortOrder]     INT            NOT NULL DEFAULT 0,
    [CreatedDate]   DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_AppAISkillRef] PRIMARY KEY ([RefId]),
    CONSTRAINT [FK_AppAISkillRef_AppAISkill] FOREIGN KEY ([SkillId])
        REFERENCES [dbo].[AppAISkill]([SkillId])
);
```

---

## DTOs

### `AppAISkillDto` (in `APP.Components.Dto/UserDefine/AISkill/`)

```csharp
public class AppAISkillDto
{
    public int SkillId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string SkillContent { get; set; }      // markdown
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<AppAISkillRefDto> References { get; set; } = new List<AppAISkillRefDto>();
}

public class AppAISkillRefDto
{
    public int RefId { get; set; }
    public int SkillId { get; set; }
    public string FileName { get; set; }
    public string FileContent { get; set; }       // markdown
    public int SortOrder { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

---

## Business Logic

### `AppAISkillBL` (in `APP.BL/AppMgr/AISkill/AppAISkillBL.cs`)

All methods receive `dataSourceRegisterId` and use:
```csharp
var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);
```

#### Methods

```csharp
public static class AppAISkillBL
{
    // --- Skill CRUD ---

    public static List<AppAISkillDto> GetAllSkills(int dataSourceRegisterId);
    // SELECT SkillId, Name, Description, IsActive, CreatedDate, UpdatedDate
    // FROM AppAISkill ORDER BY Name

    public static AppAISkillDto GetSkillById(int dataSourceRegisterId, int skillId);
    // SELECT skill + JOIN refs ordered by SortOrder

    public static int CreateSkill(int dataSourceRegisterId, AppAISkillDto dto);
    // INSERT INTO AppAISkill; return SCOPE_IDENTITY()

    public static void UpdateSkill(int dataSourceRegisterId, AppAISkillDto dto);
    // UPDATE AppAISkill SET ... WHERE SkillId = @SkillId

    public static void DeleteSkill(int dataSourceRegisterId, int skillId);
    // DELETE AppAISkillRef WHERE SkillId = @SkillId
    // DELETE AppAISkill WHERE SkillId = @SkillId

    // --- Reference CRUD ---

    public static List<AppAISkillRefDto> GetRefsBySkillId(int dataSourceRegisterId, int skillId);

    public static int CreateRef(int dataSourceRegisterId, AppAISkillRefDto dto);
    // INSERT INTO AppAISkillRef; return SCOPE_IDENTITY()

    public static void UpdateRef(int dataSourceRegisterId, AppAISkillRefDto dto);
    // UPDATE AppAISkillRef SET FileName, FileContent, SortOrder WHERE RefId = @RefId

    public static void DeleteRef(int dataSourceRegisterId, int refId);
    // DELETE AppAISkillRef WHERE RefId = @RefId

    // --- Agent Use ---

    public static string GetComposedSkillPrompt(int dataSourceRegisterId, int skillId);
    // Returns SkillContent + appended reference FileContents (ordered by SortOrder)
    // Used by AI agents to load the full prompt
}
```

#### `GetComposedSkillPrompt` logic
```
prompt = skill.SkillContent
foreach ref in skill.References (ordered by SortOrder):
    prompt += "\n\n---\n\n" + ref.FileContent
return prompt
```

---

## Web API Controller

### `AISkillController` (in `PlmApplication/Server/WebApi/AISkillController.cs`)

Base route: `api/AISkill`

| Method | Route | Action |
|--------|-------|--------|
| GET | `/GetAll?dataSourceId={id}` | List all skills (no content, summary only) |
| GET | `/GetById?dataSourceId={id}&skillId={id}` | Skill + all refs |
| POST | `/Create?dataSourceId={id}` | Create skill, body: `AppAISkillDto` |
| POST | `/Update?dataSourceId={id}` | Update skill, body: `AppAISkillDto` |
| POST | `/Delete?dataSourceId={id}&skillId={id}` | Delete skill + refs |
| GET | `/GetRefs?dataSourceId={id}&skillId={id}` | Get refs for skill |
| POST | `/CreateRef?dataSourceId={id}` | Add ref, body: `AppAISkillRefDto` |
| POST | `/UpdateRef?dataSourceId={id}` | Update ref, body: `AppAISkillRefDto` |
| POST | `/DeleteRef?dataSourceId={id}&refId={id}` | Delete ref |
| GET | `/GetComposed?dataSourceId={id}&skillId={id}` | Get full composed prompt string |

---

## Frontend

### Service: `aiSkillSvc.ts` (in `PlmApplication/AppReact/src/webapi/`)

```typescript
export class AISkillSvc {
    static GetAll(dataSourceId: number): Promise<AppAISkillDto[]>
    static GetById(dataSourceId: number, skillId: number): Promise<AppAISkillDto>
    static Create(dataSourceId: number, dto: AppAISkillDto): Promise<number>
    static Update(dataSourceId: number, dto: AppAISkillDto): Promise<void>
    static Delete(dataSourceId: number, skillId: number): Promise<void>
    static CreateRef(dataSourceId: number, dto: AppAISkillRefDto): Promise<number>
    static UpdateRef(dataSourceId: number, dto: AppAISkillRefDto): Promise<void>
    static DeleteRef(dataSourceId: number, refId: number): Promise<void>
}
```

### Component: `AISkillManagement.tsx`

**Layout:** Two-panel (master-detail)

**Left panel — Skill List (FlexGrid)**
- Columns: Name, Description, Active, Created Date
- Toolbar: [+ New Skill] [Delete]
- Row click → loads skill into right panel editor
- Context menu: Edit, Delete

**Right panel — Skill Editor**

Tabs:
1. **General** — Name (text input), Description (textarea), IsActive (toggle)
2. **Skill Content** — full-width markdown textarea (monospace font, min 400px height) with a live preview toggle
3. **Reference Files** — list of attached markdown references

**Reference Files tab:**
- FlexGrid columns: FileName, SortOrder, (actions)
- [+ Add Reference] button → opens inline editor row or small modal
- Each row: click to open reference editor (FileName input + markdown textarea)
- Drag-to-reorder SortOrder (or up/down arrows)
- [Delete] per row

**Save/Cancel** buttons in editor footer. Saves skill + all refs in sequence (skill first, then refs).

### Types: `aiSkillTypes.ts`

```typescript
export interface AppAISkillDto {
    skillId: number;
    name: string;
    description: string;
    skillContent: string;      // markdown
    isActive: boolean;
    createdDate: string;
    updatedDate?: string;
    references: AppAISkillRefDto[];
}

export interface AppAISkillRefDto {
    refId: number;
    skillId: number;
    fileName: string;
    fileContent: string;       // markdown
    sortOrder: number;
    createdDate: string;
}
```

---

## UX Rules

- `skillContent` and `fileContent` editors: use `<textarea>` with `font-family: monospace`, `white-space: pre`, resize vertical
- Markdown preview: render with a simple `dangerouslySetInnerHTML` after basic markdown-to-html conversion (or use existing markdown lib if available)
- Unsaved changes warning before navigating away
- Delete skill: confirm dialog ("This will also delete all reference files. Continue?")
- Empty state in grid: "No skills found. Click + New Skill to get started."

---

## Implementation Order

1. **DB** — Run CREATE TABLE scripts for `AppAISkill` and `AppAISkillRef`
2. **DTOs** — Create `AppAISkillDto`, `AppAISkillRefDto`
3. **BL** — Implement `AppAISkillBL` with all CRUD methods
4. **API** — Implement `AISkillController`
5. **Frontend types** — `aiSkillTypes.ts`
6. **Frontend service** — `aiSkillSvc.ts`
7. **Frontend component** — `AISkillManagement.tsx`
8. **Route** — Add route entry in `routes.shared.tsx`

---

## Notes for AI Agent Integration

- `GetComposedSkillPrompt` is the key method AI agents call to load a skill
- DbGenie's `ChatWithAgentAsync` should accept an optional `SkillId` parameter and call `GetComposedSkillPrompt` instead of (or to supplement) the embedded `sqlskill.md`
- Future: skill selector UI in the DbGenie chat panel
