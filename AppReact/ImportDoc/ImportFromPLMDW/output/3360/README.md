# Template 3360 → shared 3351 APP tables (APPEND)

**Do not** run Phase D Insert of `4_PlmDw_ImportBlueprint.json` from this folder — it would create duplicate `Tab_4256` / `Tab_4257` transactions. Use **3351** Blueprint for Transaction Group / Transactions / Form / Search.

## Shared physical names (intentionally same as 3351)

| Role | APP table |
|------|-----------|
| Fabrics/Trims tab | `Plm_Fabrics___Trims` |
| Labels/Packaging tab | `Plm_Labels___Packaging` |
| Fabric / Trim / Label / Packaging BOM | `Plm_Fabric_BOM_prod`, `Plm_Trim_BOM_prod`, `Plm_Label_BOM_prod`, `Plm_Packaging_BOM_prod` |
| Grandchildren | `Plm_*GrandColorway` |

3360 DW sources mapped into those tables:

| APP | 3360 DW |
|-----|---------|
| `Fabrics___Trims` | `PLM_DW_Tab_Fabrics___Trims_20_4256` |
| `Labels___Packaging` | `PLM_DW_Tab_Labels___Packaging_20_4257` |
| `Fabric_BOM_prod` | `PLM_DW_Grid_Fabric_BOM_prod_20_Colorways_3183` |
| `Trim_BOM_prod` | `PLM_DW_Grid_Trim_BOM_prod_20_Colorways_3169` |
| `Label_BOM_prod` | `PLM_DW_Grid_Label_BOM_prod_20_Colorways_3170` |

`transactionIntegrationId` for 20-colorway BOM grids points at **3351** tabs (`Tab_4229`, `Tab_4230`) so Blueprint metadata stays aligned; skip Execute for this folder.

## Run order (tenant DB)

After `output/3351` steps 1–5 (+ Phase D Update as needed):

```text
1. output/3360/1_PlmDw_Tables.sql     -- ADD any 20-only host columns (isColorway11…20, etc.)
2. output/3360/2_PlmDw_FieldMapping.sql  -- rewires FieldMapping Dw* for shared AppTables to 20-colorway DW
3. output/3360/3_PlmDw_ImportFromDW.sql  -- @PlmTemplateId=3360, @ImportMode=APPEND
4. SKIP Phase D Insert  (or Execute Update only if 3351 Blueprint needs new fields)
5. output/3360/5_PlmDw_ImportBomColorwayGrandchild.sql  -- UNPIVOT 20 slots into same GrandColorway tables
6. optional cleanup
```

**Important:** Step 2 overwrites `Plm_FieldMapping` DW pointers for shared tables. Before re-importing **3351** data, re-run `output/3351/2_PlmDw_FieldMapping.sql`.

Config source: `source/dwTabImportConfig.3360.append.json`. Regenerate with `source/_gen_3360_append.ps1`.
