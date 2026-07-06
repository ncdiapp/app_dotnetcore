# Apparel PLM Migration Plan

**Objective:** Rebuild Legacy PLM on App-Builder

## Legacy PLM Main Features

The following features need to be rebuilt on App-Builder:

1. Multiple Databases
2. Data Source Management
3. Color Management
4. POM and POM Template Management
5. File and Image Management
6. Reference Management
7. Search View & Mass Update
8. User, Domain, Role Security
9. Print 
10. Notification

## Migration Steps

### 1. Database – Link Database or Import Tables

The PLM connects to the PLM database, plus additional ERP and other external databases.

We need to define which tables need to be imported into the App-Builder database, or whether we reuse the external databases directly. If PLM has specific logic to push data into an external database, we may need to write extra code to do it. Otherwise, we only need to use these external data as entity data sources.

### 2. Migrate Data Source Management

In App-Builder, create entity datasources.

**Notes:** Need to import PLM data sources into App-Builder, by database and type.

- For simple data sources, copy/paste the list values.
- For system table data sources, import the table data or reuse the external database.
- For relationships, re-configure in App-Builder with a simple list editor form.

### 3. Migrate Color Management

In App-Builder, create a color table, import color data from PLM, and configure the folder structure.

**Notes:**

- In PLM, we have a color library managed by multiple folders. In App-Builder, we can configure one color to belong to one folder.
- In the PLM Color editor, we have special functions such as swatch, color group, and similar group. **These may need extra coding in App-Builder.**

### 4. Migrate POM and POM Template Management

In App-Builder, create POM, PomTemplate, and PomTemplateDetail tables, and import data from PLM.

**Notes:** In PLM, we have CM/Inch display, but all values are saved in cm. We also have sizerun detail column mapping for the PLM template grid. **These may need extra coding.**

### 5. Migrate File and Image Management

**Notes:** In PLM, we have AI/PDF multiple child image features. **If we have to implement this, we need extra coding.**

### 6. Migrate Reference Management

In PLM, we have many system-defined blocks and grids with specific logic and flow. There are three parts:

- Product Management
- Vendor Management
- Vendor Request Management

#### Special Notes

**6.1 Vendor block – control security**

- **Need coding.**

**6.2 Size run block/grid**

Select size, auto-populate size details, and allow the user to select details afterward.

- Might be configurable.

**6.3 Spec Fit, Grading, QC block and grid**

- **Need a lot of coding** for data transfer, grading calculation, and POM/POM template importing.

**6.4 Color grid – add color from library**

- Mostly configurable.
- **Additional coding:** swatch column, duplicate checking, use as DDL source, colorway column name.

**6.5 BOM grids**

- Mostly configurable.
- **Additional coding:** special columns (e.g. current ref simple DCU) and colorway name columns.

**6.6 Matrix grid**

- Configurable.

**6.7 Vendor request**

- **Special flow** for request/approval by different types of users. May need extra coding.

**6.8 Copy tabs**

- **Extra coding** or find another solution.

> **Note:** For PLM templates and tabs, we may use sibling tables to put fields on many tables by logic.

### 7. Search View & Mass Update

- Configurable.
- Reference PLM to create search, view, and dataset.

### 8. User, Domain, Role Security

- Configurable.
