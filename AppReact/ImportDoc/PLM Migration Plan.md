# Apparel PLM Migration Plan

## Objective: Rebuild Legacy PLM on App-Builder

### Legacy PLM Main Features

Features that need to be rebuilt on App-Builder:

- Multiple Databases
- Data Source Management
- Color Management
- POM and POM Template Management
- File and Image Management
- Reference management
- Search View & Mass update
- User, Domain, Role Security

---

## Migration Steps

### 1. Database — Link Database or Import Tables

**Notes:** The PLM connects to the PLM database, and additional ERP and other external databases.

We need to define which tables need to be imported into App-Builder database, or whether we reuse the external databases directly. If PLM has specific logic to push data into an external database, we may need to write extra code to do it. Otherwise, we only need to use these external data as entity data sources.

### 2. Migrate Data Source Management

In App-Builder, create entity datasources.

**Notes:** Need to import PLM data source into App-Builder, by database and type.

- For simple data source, we just copy/paste the list value.
- For system table data source, we import the table data or reuse the external database.
- For relationship, we re-config it in App-Builder with a simple list editor form.

### 3. Migrate Color Management

In App-Builder, create color table, import color data from PLM. Configure folder structure.

**Notes:** In PLM, we have color library, managed by multiple folders. In App Builder, we can config one color belongs to one folder. In PLM Color editor, we have some special functions, like swatch, color group, similar group. It may need extra coding in App-Builder.

### 4. Migrate POM and POM Template Management

In App-Builder, create POM, PomTemplate, PomTemplateDetail tables, import data from PLM.

**Notes:** In PLM, we have CM/Inch display, but all saved in cm. We also have sizerun detail columns mapping for PLM template grid. These may need extra coding.

### 5. Migrate File and Image Management

**Notes:** In PLM, we have AI/PDF multiple child image feature. If we have to implement this, we need extra coding.

### 6. Migrate Reference Management

In PLM, we have a lot of system defined blocks and grid, which have specific logic and flow. We have 3 parts:

- Product Management
- Vendor Management
- Vendor Request Management

#### Special Notes

**6.1 Vendor block, control security**

- Need coding.

**6.2 Size run block/grid**

Select size, auto populate size details, and user can select details after.

- Might be configurable.

**6.3 Spec Fit, Grading, QC block and grid**

- Need a lot of coding, for data transfer, grading calculation, POM and POM template importing.

**Color Grid — add color from library**

- Mostly configurable.
- Additional coding: swatch column, duplicated checking, use as DDL source, colorway column name.

**BOM Grids**

- Mostly configurable.
- Additional coding: special columns (example current ref simple dcu), and colorway name columns.

**Matrix Grid**

- Configurable.

**Vendor Request**

- Special flow to request/approval, by different type of user. May need extra coding.

**Copy Tabs**

- Extra coding or find other solution.

> **Note:** For PLM templates and tabs, we may use sibling tables to put fields on many tables by logic.

### 7. Search View & Mass Update

- Configurable.
- Reference to PLM, create search, view, dataset.

### 8. User, Domain, Role Security

- Configurable.
