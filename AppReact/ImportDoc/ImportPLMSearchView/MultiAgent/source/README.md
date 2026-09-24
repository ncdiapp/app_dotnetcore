# Official SOURCE for Multi-Agent Search / MassUpdate children

Copied from `AppReact/ImportDoc/ImportPLMSearchView/source/` for Agent Management seeding.

- **Do not** invent replacements at runtime. If a required file is missing, stop and restore this folder.
- Probe `.sql` files: substitute `@SearchTemplateId` / `@MassUpdateViewId` / wanted id lists, then `execute_sql` with the correct `dataSourceId`.
- `*.example.json`: schema contracts for Phase B. Match these shapes; do not emit stub objects.
- `plmSearchImportConfig.example.json` in this pack includes `plmDataSourceId` / `appDataSourceId` (parent copy is Cursor-IDE connection-string oriented).

See `../README.md` for which files each SkillKey requires.
