# ImportPlmPomAndGrading — output

Generated Preview / import SQL lands here as:

```text
output/{runId}/
  0_Preview_Report.md
  0_Preview_Counts.sql
  1_Tchp_Import_SizeRun.sql      # after Phase A confirm
  2_Tchp_Import_BodyPart.sql
  3_Tchp_Import_PomTemplate.sql
  4_Tchp_Import_GradeRules.sql
  5_Tchp_Import_Validate.sql
  README.md
```

Do not commit connection strings or passwords.

See `../PROMPT.md` for Gate 0, locked decisions (G-B, M-B, active-only, PLM source not `Plm_*`), and run order.
