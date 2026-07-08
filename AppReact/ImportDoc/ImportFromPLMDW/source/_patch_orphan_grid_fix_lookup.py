from pathlib import Path

path = Path(r"c:\DevAppAI\AppAIClean\APP.BL\DataMigration\PlmMigration\PlmMigrationBL.TemplatePostProcess.cs")
text = path.read_text(encoding="utf-8")

old = """                cmd.CommandText = @\"
SELECT t.TransactionID, t.FormID, t.IntegrationId,
    (SELECT COUNT(1) FROM dbo.AppFormLayoutItem li WHERE li.FormID = t.FormID) AS LayoutItemCount,
    f.LayoutType
FROM dbo.AppTransaction t
LEFT JOIN dbo.AppForm f ON f.FormID = t.FormID
WHERE t.IntegrationId LIKE 'Tab[_]%';\";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string integrationId = reader.IsDBNull(2) ? null : reader.GetString(2);
                        if (string.IsNullOrWhiteSpace(integrationId) || !integrationId.StartsWith(\"Tab_\", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!int.TryParse(integrationId.Substring(4), out int tabId))
                            continue;

                        dict[tabId] = new TabTransactionFormStatus
                        {
                            TransactionId = reader.GetInt32(0),
                            FormId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            LayoutItemCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            LayoutType = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
                        };
                    }
                }"""

new = """                cmd.CommandText = @\"
SELECT t.TransactionID, t.FormID, t.IntegrationId,
    (SELECT COUNT(1) FROM dbo.AppFormLayoutItem li WHERE li.FormID = t.FormID) AS LayoutItemCount,
    f.LayoutType
FROM dbo.AppTransaction t
LEFT JOIN dbo.AppForm f ON f.FormID = t.FormID
WHERE t.IntegrationId LIKE 'Tab[_]%'
   OR t.IntegrationId LIKE 'Grid[_]%';\";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string integrationId = reader.IsDBNull(2) ? null : reader.GetString(2);
                        if (string.IsNullOrWhiteSpace(integrationId))
                            continue;

                        int tabId;
                        if (integrationId.StartsWith(\"Tab_\", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(integrationId.Substring(4), out tabId))
                                continue;
                        }
                        else if (integrationId.StartsWith(\"Grid_\", StringComparison.OrdinalIgnoreCase))
                        {
                            // Orphan DW grids are planned with TabId = -PlmGridId.
                            if (!int.TryParse(integrationId.Substring(5), out int gridId) || gridId <= 0)
                                continue;
                            tabId = -gridId;
                        }
                        else
                        {
                            continue;
                        }

                        dict[tabId] = new TabTransactionFormStatus
                        {
                            TransactionId = reader.GetInt32(0),
                            FormId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            LayoutItemCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            LayoutType = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4)
                        };
                    }
                }"""

if old not in text:
    raise SystemExit("OLD NOT FOUND")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("OK updated TemplatePostProcess")
