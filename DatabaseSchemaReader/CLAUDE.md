# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DatabaseSchemaReader is a .NET Framework 4.8 library for reading database schemas and generating SQL/code. It wraps ADO.NET's `DbConnection.GetSchema` method and normalizes the inconsistent provider implementations into a unified schema object model.

**Namespace:** `DatabaseSchemaMrg`

**Supported databases:** SQL Server, Oracle, MySQL, PostgreSQL, DB2, SQLite, SQL Server CE, Sybase, Ingres, VistaDb

## Build

This is an old-style .csproj (non-SDK). Build via Visual Studio or MSBuild:
```bash
msbuild DatabaseSchemaReader.csproj /p:Configuration=Debug
```

Output: `bin/Debug/DatabaseSchemaReader.dll`

**CRITICAL:** This project uses old-style .csproj. New `.cs` files MUST be manually registered in the `.csproj` file under `<Compile Include="..." />`. Files not registered are silently ignored during compilation.

## Architecture

### Core Classes

| Class | Purpose |
|-------|---------|
| `DatabaseFixture` | Main entry point. Reads full schema via `ReadAll()` or individual objects |
| `DatabaseSchema` | Container for tables, views, stored procedures, functions, sequences |
| `SchemaReader` | Low-level ADO.NET schema reading (returns DataTables) |
| `SchemaExtendedReader` | Extended schema reading with provider-specific queries |
| `SqlWriter` | Generates parameterized SELECT/INSERT/UPDATE/DELETE SQL |
| `DdlGeneratorFactory` | Creates DDL generators (CREATE TABLE, migrations) per SQL type |
| `CompareSchemas` | Schema comparison and diff script generation |
| `CodeWriter` | Generates C# entity classes and ORM mappings |

### Key Enums

`EmSqlType` defines supported database types:
- `SqlServer` (1), `Oracle` (2), `MySql` (3), `PostgreSql` (4), `Db2` (5), `SQLite` (6), `SqlServerCe` (7)

### Provider Configuration

Database providers are configured in `App.config` with keys matching `EmSqlType` enum names:
```xml
<add key="SqlServer" value="System.Data.SqlClient" />
<add key="Oracle" value="Oracle.ManagedDataAccess.Client" />
<add key="MySql" value="MySql.Data.MySqlClient" />
```

### Directory Structure

```
├── DataSchema/          # Schema object model (DatabaseTable, DatabaseColumn, etc.)
├── ProviderSchemaReaders/ # Provider-specific schema readers
├── SqlGen/              # SQL generation per database type
│   ├── SqlServer/
│   ├── Oracle/
│   ├── MySql/
│   ├── PostgreSql/
│   └── ...
├── Conversion/          # DataTable to object converters
└── Utilities/           # Helper classes
```

## Usage Examples

### Read Schema
```csharp
var reader = new DatabaseFixture(connectionString, EmSqlType.SqlServer);
var schema = reader.ReadAll();  // Full schema
var table = reader.Table("Orders");  // Single table
```

### Generate SQL
```csharp
var sqlWriter = new SqlWriter(table, EmSqlType.SqlServer);
var select = sqlWriter.SelectAllSql();
var insert = sqlWriter.InsertSql();
var paged = sqlWriter.SelectPageSql();  // Paged queries
```

### Compare Schemas
```csharp
var comparison = new CompareSchemas(baseSchema, targetSchema);
var migrationScript = comparison.Execute();
```

### Generate DDL
```csharp
var factory = new DdlGeneratorFactory(EmSqlType.Oracle);
var migrations = factory.MigrationGenerator();
var script = migrations.AddTable(table);
```

## Code Conventions

- Parameter prefixes vary by database: `@` (SQL Server), `:` (Oracle/PostgreSQL), `?` (MySQL)
- Name escaping varies: `[name]` (SQL Server), `"name"` (Oracle), `` `name` `` (MySQL)
- Use `EmSqlType` enum rather than provider name strings where possible
- `DatabaseSchema.GetSqlTypeByProvideName()` converts provider names to `EmSqlType`
