# Output folder

Generated deliverables land here after **Phase B**.

## Main import ([`../PROMPT.md`](../PROMPT.md))

```text
output/{searchTemplateId}/
  1_PlmSearch_ImportBlueprint.json
  README.md   (optional — Phase A notes)
```

Example: `output/23702/1_PlmSearch_ImportBlueprint.json`.

## Sibling View import ([`../PROMPT_SIBLING_VIEW.md`](../PROMPT_SIBLING_VIEW.md))

After an existing Search import, adding another PLM View:

```text
output/{searchTemplateId}/
  2_PlmSearch_SiblingView_{referenceViewId}.json     ← Option A (enrich DataSet + sibling View)
  1_PlmSearch_ImportBlueprint_V{referenceViewId}.json ← Option B (new Search Search_{Name}_V{ViewId})
  README_Sibling_{referenceViewId}.md                 (optional)
```

Architecture: [`../source/MULTI_VIEW_COVERAGE.md`](../source/MULTI_VIEW_COVERAGE.md)

Do not commit connection strings or tenant secrets.
