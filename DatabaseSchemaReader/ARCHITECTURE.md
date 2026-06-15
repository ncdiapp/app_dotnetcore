# DatabaseSchemaReader Architecture & Data Flow

## Overview

DatabaseSchemaReader is a .NET library that provides a unified abstraction over ADO.NET's `DbConnection.GetSchema()` to read database schemas from multiple database platforms and generate SQL/code.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Client Application                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DatabaseFixture                                   │
│                    (Main Entry Point / Facade)                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  ReadAll()  │  │  Table()    │  │ AllTables() │  │ AllStoredProcedures │ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
              ┌────────────────────────┼────────────────────────┐
              ▼                        ▼                        ▼
┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐
│  SchemaReaderFactory │  │    SchemaConverter   │  │  DatabaseSchemaFixer │
│  (Provider Routing)  │  │  (DataTable→Object)  │  │  (Reference Linking) │
└──────────────────────┘  └──────────────────────┘  └──────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Provider-Specific Schema Readers                        │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌─────────────────┐  │
│  │ SqlServer     │ │ Oracle        │ │ MySql         │ │ PostgreSql      │  │
│  │ SchemaReader  │ │ SchemaReader  │ │ SchemaReader  │ │ SchemaReader    │  │
│  └───────────────┘ └───────────────┘ └───────────────┘ └─────────────────┘  │
│  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ ┌─────────────────┐  │
│  │ Db2           │ │ Sybase        │ │ SQLite        │ │ OleDb           │  │
│  │ SchemaReader  │ │ SchemaReader  │ │ SchemaReader  │ │ SchemaReader    │  │
│  └───────────────┘ └───────────────┘ └───────────────┘ └─────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ADO.NET DbConnection.GetSchema()                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Database Server                                 │
│         (SQL Server, Oracle, MySQL, PostgreSQL, DB2, SQLite, etc.)          │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Entry Points

| Component | Description |
|-----------|-------------|
| `DatabaseFixture` | Main facade. Creates schema readers, orchestrates reading, returns `DatabaseSchema` |
| `SchemaReader` | Low-level reader returning raw `DataTable` results |
| `SchemaExtendedReader` | Extended reader with provider-specific SQL queries |

### 2. Data Model (`DataSchema/`)

```
DatabaseSchema (root)
├── Tables: List<DatabaseTable>
│   ├── Columns: List<DatabaseColumn>
│   ├── PrimaryKey: DatabaseConstraint
│   ├── ForeignKeys: List<DatabaseConstraint>
│   ├── UniqueKeys: List<DatabaseConstraint>
│   ├── CheckConstraints: List<DatabaseConstraint>
│   ├── DefaultConstraints: List<DatabaseConstraint>
│   ├── Indexes: List<DatabaseIndex>
│   └── Triggers: List<DatabaseTrigger>
├── Views: List<DatabaseView>
│   └── Columns: List<DatabaseColumn>
├── StoredProcedures: List<DatabaseStoredProcedure>
│   └── Arguments: List<DatabaseArgument>
├── Functions: List<DatabaseFunction>
├── Sequences: List<DatabaseSequence>
├── Packages: List<DatabasePackage>  (Oracle)
├── DataTypes: List<DataType>
└── Users: List<DatabaseUser>
```

### 3. Provider Schema Readers (`ProviderSchemaReaders/`)

Each database has a specialized reader that overrides base methods to handle provider-specific schema queries:

| Reader | Database | Notes |
|--------|----------|-------|
| `SqlAzureOrSqlServerSchemaReader` | SQL Server / Azure | Handles identity, computed columns |
| `OracleSchemaReader` | Oracle | Packages, sequences, triggers |
| `MySqlSchemaReader` | MySQL | Engine types, auto-increment |
| `PostgreSqlSchemaReader` | PostgreSQL | Serial columns, schemas |
| `Db2SchemaReader` | IBM DB2 | System catalogs |
| `SybaseAseSchemaReader` | Sybase ASE | |
| `SybaseAsaSchemaReader` | Sybase ASA | |
| `SqlServerCeSchemaReader` | SQL Server CE | Limited schema support |

### 4. Conversion Layer (`Conversion/`)

Transforms ADO.NET `DataTable` results into typed objects:

```
DataTable (from GetSchema)
    │
    ▼
┌─────────────────────────────────────────────────┐
│              KeyMap Classes                      │
│  (Resolve column name differences by provider)  │
│  TableKeyMap, ColumnsKeyMap, IndexKeyMap, etc.  │
└─────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────┐
│              Converter Classes                   │
│  ColumnConverter, IndexConverter,               │
│  SchemaConstraintConverter, TriggerConverter    │
└─────────────────────────────────────────────────┘
    │
    ▼
DatabaseTable, DatabaseColumn, DatabaseIndex, etc.
```

### 5. SQL Generation (`SqlGen/`)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DdlGeneratorFactory                              │
│                    (Creates generators per SqlType)                      │
└─────────────────────────────────────────────────────────────────────────┘
                                   │
        ┌──────────────────────────┼──────────────────────────┐
        ▼                          ▼                          ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│  TableGenerator  │    │ MigrationGenerator│    │ ProcedureGenerator│
│  (CREATE TABLE)  │    │ (ALTER/ADD/DROP) │    │  (CRUD Sprocs)   │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

**Provider-specific implementations:**
- `SqlGen/SqlServer/` - SQL Server DDL
- `SqlGen/Oracle/` - Oracle DDL (sequences, triggers)
- `SqlGen/MySql/` - MySQL DDL (backticks, engine)
- `SqlGen/PostgreSql/` - PostgreSQL DDL (serial, schemas)
- `SqlGen/Db2/` - DB2 DDL
- `SqlGen/SqLite/` - SQLite DDL
- `SqlGen/SqlServerCe/` - SQL Server CE DDL

### 6. SqlWriter (DML Generation)

Generates parameterized CRUD SQL:

```csharp
SqlWriter(table, EmSqlType.SqlServer)
    │
    ├── SelectAllSql()      → SELECT with all columns
    ├── SelectByIdSql()     → SELECT with WHERE pk = @pk
    ├── SelectPageSql()     → Paged SELECT (ROW_NUMBER, LIMIT, etc.)
    ├── InsertSql()         → INSERT with SCOPE_IDENTITY / RETURNING
    ├── UpdateSql()         → UPDATE with WHERE pk = @pk
    └── DeleteSql()         → DELETE with WHERE pk = @pk
```

**Database-specific features:**
- Parameter prefix: `@` (SqlServer), `:` (Oracle), `?` (MySQL)
- Name escaping: `[name]` (SqlServer), `"name"` (Oracle), `` `name` `` (MySQL)
- Identity retrieval: `SCOPE_IDENTITY()`, `RETURNING`, `LAST_INSERT_ID()`
- Paging: `ROW_NUMBER()`, `LIMIT/OFFSET`, `ROWNUM`

### 7. Schema Comparison (`Compare/`)

```
DatabaseSchema (base)     DatabaseSchema (target)
         │                         │
         └──────────┬──────────────┘
                    ▼
           ┌────────────────┐
           │ CompareSchemas │
           └────────────────┘
                    │
    ┌───────────────┼───────────────┐
    ▼               ▼               ▼
CompareTables  CompareViews  CompareProcedures
    │
    ├── CompareColumns
    ├── CompareConstraints
    ├── CompareIndexes
    └── CompareTriggers
                    │
                    ▼
           ┌────────────────┐
           │ CompareResult  │
           │ (DDL Scripts)  │
           └────────────────┘
```

### 8. Code Generation (`CodeGen/`)

```
DatabaseSchema
      │
      ▼
┌─────────────┐
│ CodeWriter  │
└─────────────┘
      │
      ├── ClassWriter          → C# POCO classes
      ├── CodeFirstMappingWriter → EF Code First mappings
      ├── FluentMappingWriter  → NHibernate Fluent mappings
      ├── MappingWriter        → NHibernate XML mappings
      └── SprocWriter          → Stored procedure wrappers
```

---

## Data Flow Diagrams

### 1. Schema Reading Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. Client calls: new DatabaseFixture(connString, EmSqlType.SqlServer)       │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. SchemaReaderFactory.Create() selects SqlServerSchemaReader               │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. Client calls: dbReader.ReadAll()                                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
│ DataTypes()     │        │ AllTables()     │        │ Sequences()     │
│ → DataType list │        │ → Table list    │        │ → Sequence list │
└─────────────────┘        └─────────────────┘        └─────────────────┘
                                       │
                    ┌──────────────────┼──────────────────┐
                    ▼                  ▼                  ▼
           ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
           │ Columns      │   │ Constraints  │   │ Indexes      │
           │ (per table)  │   │ (PK,FK,UK)   │   │ (per table)  │
           └──────────────┘   └──────────────┘   └──────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 4. SchemaConverter transforms DataTables → DatabaseTable/Column/Index       │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 5. DatabaseSchemaFixer.UpdateReferences() links FKs to referenced tables    │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 6. Returns populated DatabaseSchema with all objects linked                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. SQL Generation Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. SqlWriter sqlWriter = new SqlWriter(table, EmSqlType.SqlServer)          │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. Configure: parameterPrefix='@', escapeStart='[', escapeEnd=']'           │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. Client calls: sqlWriter.InsertSql()                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
│ GetColumns()    │        │ Format columns  │        │ Identity check  │
│ (exclude auto)  │        │ with escaping   │        │ SCOPE_IDENTITY  │
└─────────────────┘        └─────────────────┘        └─────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 4. Returns: INSERT INTO [Orders] ([Col1], [Col2]) VALUES (@Col1, @Col2);    │
│             SELECT SCOPE_IDENTITY();                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3. Schema Comparison Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. CompareSchemas comparison = new CompareSchemas(baseSchema, targetSchema) │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. comparison.Execute()                                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
│ CompareTables   │        │ CompareViews    │        │ CompareProcedures│
│ (add/drop/alter)│        │ (add/drop)      │        │ (add/drop)       │
└─────────────────┘        └─────────────────┘        └─────────────────┘
         │
         ├── CompareColumns (added/removed/changed columns)
         ├── CompareConstraints (PK/FK/UK changes)
         └── CompareIndexes (index changes)
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. Returns migration script:                                                 │
│    ALTER TABLE [Orders] ADD [NewColumn] NVARCHAR(100);                       │
│    ALTER TABLE [Orders] DROP COLUMN [OldColumn];                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4. DDL Generation Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. DdlGeneratorFactory factory = new DdlGeneratorFactory(EmSqlType.Oracle)  │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 2. IMigrationGenerator migrations = factory.MigrationGenerator()             │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 3. migrations.AddTable(table)                                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         ▼                             ▼                             ▼
┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
│ OracleTable     │        │ OracleDataType  │        │ OracleConstraint│
│ Generator       │        │ Writer          │        │ Writer          │
└─────────────────┘        └─────────────────┘        └─────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 4. Returns:                                                                  │
│    CREATE TABLE "ORDERS" (                                                   │
│      "ORDER_ID" NUMBER(10) NOT NULL,                                         │
│      "CUSTOMER_ID" NUMBER(10),                                               │
│      CONSTRAINT "PK_ORDERS" PRIMARY KEY ("ORDER_ID")                         │
│    );                                                                        │
│    CREATE SEQUENCE "SEQ_ORDERS";                                             │
│    CREATE TRIGGER "TRG_ORDERS" BEFORE INSERT ON "ORDERS" ...                 │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Key Interfaces

```csharp
// Schema reading
interface IDatabaseReader : IDisposable
{
    string Owner { get; set; }
    DatabaseSchema DatabaseSchema { get; }
    DatabaseSchema ReadAll();
    IList<DatabaseTable> AllTables();
    DatabaseTable Table(string tableName);
}

// DDL generation
interface ITableGenerator
{
    string Write();                              // Full CREATE TABLE
    string WriteColumn(DatabaseColumn column);   // Single column DDL
}

interface IMigrationGenerator
{
    string AddTable(DatabaseTable table);
    string AddColumn(DatabaseTable table, DatabaseColumn column);
    string AlterColumn(DatabaseTable table, DatabaseColumn column, DatabaseColumn original);
    string DropColumn(DatabaseTable table, DatabaseColumn column);
    string DropTable(DatabaseTable table);
}

interface IProcedureGenerator
{
    string WriteSelect();   // SELECT sproc
    string WriteInsert();   // INSERT sproc
    string WriteUpdate();   // UPDATE sproc
    string WriteDelete();   // DELETE sproc
}
```

---

## Configuration

Provider names are resolved from `App.config`:

```xml
<appSettings>
  <add key="SqlServer" value="System.Data.SqlClient" />
  <add key="Oracle" value="Oracle.ManagedDataAccess.Client" />
  <add key="MySql" value="MySql.Data.MySqlClient" />
  <add key="PostgreSql" value="Npgsql" />
  <add key="Db2" value="IBM.Data.DB2" />
</appSettings>
```

If keys are missing, defaults are used (see `DatabaseSchema` static constructor).

---

## Thread Safety

- `DatabaseSchema._DictServerTypePorviderName` is a static readonly dictionary populated once in static constructor - thread-safe for reads
- `DatabaseFixture` instances are NOT thread-safe - create one per thread/request
- `SqlWriter` instances are NOT thread-safe - create one per operation
