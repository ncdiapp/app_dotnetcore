# Phase B — RunId `pilot1`

## Confirmed decisions

| Item | Choice |
|------|--------|
| SizeRun | **S-A** — `isVisibleInPLM = 1` (20 / 152) |
| BodyPart Code | **B-A** — empty → `BP_{id}`; duplicate Code → `Code_{BodyPartID}` |
| BodyPart scope | All **540** |
| GradeRule | **G-B** — one RuleSet per BodyType |

## Run order (connected to `TenantDB_PLM27`)

```bat
sqlcmd -S PC3B\MSSQLSERVER01 -d TenantDB_PLM27 -U sa -P *** -C -i 1_Tchp_Import_SizeRun.sql
sqlcmd -S PC3B\MSSQLSERVER01 -d TenantDB_PLM27 -U sa -P *** -C -i 2_Tchp_Import_BodyPart.sql
sqlcmd -S PC3B\MSSQLSERVER01 -d TenantDB_PLM27 -U sa -P *** -C -i 3_Tchp_Import_PomTemplate.sql
sqlcmd -S PC3B\MSSQLSERVER01 -d TenantDB_PLM27 -U sa -P *** -C -i 4_Tchp_Import_GradeRules.sql
sqlcmd -S PC3B\MSSQLSERVER01 -d TenantDB_PLM27 -U sa -P *** -C -i 5_Tchp_Import_Validate.sql
```

Uses same-server three-part names: `[SourceERP].dbo.*`, `[plm_live_20260602].dbo.*`.

## Notes

- `DefaultBaseSizeId` set only when that SizeRunRotate exists in imported (visible) sizes; Templates 18/19/21 keep NULL (base size 100 not visible).
- ASTM seed RuleSets (Id 1–2) are left unchanged; custom RuleSets use BodyTypeID 17–21.
