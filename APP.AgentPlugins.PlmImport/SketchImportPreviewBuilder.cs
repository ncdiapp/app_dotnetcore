using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Pure PLM-read + tenant AppFile overlap counts for sketch import preview.
/// No ServerContext / AppFile writes — Host owns those.
/// </summary>
public static class SketchImportPreviewBuilder
{
    public static PlmSketchImportPreviewDto Build(string plmConnectionString, string tenantConnectionString)
    {
        var preview = new PlmSketchImportPreviewDto();
        if (string.IsNullOrWhiteSpace(plmConnectionString))
        {
            preview.IsSuccess = false;
            preview.ErrorMessage = "plmConnectionString is required.";
            return preview;
        }
        if (string.IsNullOrWhiteSpace(tenantConnectionString))
        {
            preview.IsSuccess = false;
            preview.ErrorMessage = "tenantConnectionString is required.";
            return preview;
        }

        var sourceIds = new List<int>();

        using (var plmConn = new SqlConnection(plmConnectionString.Trim()))
        {
            plmConn.Open();
            EnsurePlmSketchSchema(plmConn);

            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    SketchID,
    Extension,
    ImageType,
    CASE WHEN SketchImage IS NOT NULL OR Thumbnail IS NOT NULL OR OriginalImage IS NOT NULL THEN 1 ELSE 0 END AS HasBinary
FROM dbo.tblSketch
ORDER BY SketchID;";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int sketchId = reader.GetInt32(0);
                        preview.SourceSketchCount++;

                        bool hasBinary = reader.GetInt32(3) == 1;
                        if (!hasBinary)
                        {
                            preview.MissingBinaryCount++;
                            continue;
                        }

                        sourceIds.Add(sketchId);
                        preview.SourceWithBinaryCount++;
                        string extension = NormalizeExtension(
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader["ImageType"]));
                        if (IsImageExtension(extension))
                            preview.ImageCount++;
                        else
                            preview.FileCount++;
                    }
                }
            }
        }

        preview.ExistingAppFileCount = CountExistingAppFiles(tenantConnectionString.Trim(), sourceIds);
        preview.ReadyToImportCount = Math.Max(0, preview.SourceWithBinaryCount - preview.ExistingAppFileCount);
        if (preview.MissingBinaryCount > 0)
            preview.Warnings.Add($"{preview.MissingBinaryCount} tblSketch row(s) have no SketchImage, Thumbnail, or OriginalImage binary.");
        if (preview.ExistingAppFileCount > 0)
            preview.Warnings.Add($"{preview.ExistingAppFileCount} AppFile row(s) already use the same FileID and will be skipped.");

        preview.IsSuccess = true;
        return preview;
    }

    private static void EnsurePlmSketchSchema(SqlConnection conn)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSketch';";
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                throw new InvalidOperationException("PLM table dbo.tblSketch was not found.");
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'tblSketch'
  AND COLUMN_NAME IN ('SketchID', 'SketchCode', 'SketchImage', 'Thumbnail', 'OriginalImage', 'Extension');";
            if (Convert.ToInt32(cmd.ExecuteScalar()) < 6)
                throw new InvalidOperationException("PLM dbo.tblSketch is missing one or more required columns.");
        }
    }

    private static int CountExistingAppFiles(string tenantConnectionString, List<int> sourceIds)
    {
        if (sourceIds.Count == 0) return 0;

        int count = 0;
        using (var conn = new SqlConnection(tenantConnectionString))
        {
            conn.Open();
            for (int offset = 0; offset < sourceIds.Count; offset += 1000)
            {
                int take = Math.Min(1000, sourceIds.Count - offset);
                using (var cmd = conn.CreateCommand())
                {
                    var names = new List<string>();
                    for (int i = 0; i < take; i++)
                    {
                        string name = "@p" + i;
                        names.Add(name);
                        cmd.Parameters.AddWithValue(name, sourceIds[offset + i]);
                    }

                    cmd.CommandText = $"SELECT COUNT(*) FROM dbo.AppFile WHERE FileID IN ({string.Join(",", names)});";
                    count += Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        return count;
    }

    private static bool IsImageExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        switch (extension.Trim().ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
            case ".gif":
            case ".png":
            case ".bmp":
            case ".tif":
            case ".tiff":
                return true;
            default:
                return false;
        }
    }

    private static string NormalizeExtension(string extension, int? imageType)
    {
        string ext = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim();
        if (string.IsNullOrWhiteSpace(ext) && imageType.HasValue)
        {
            switch ((EmAppDocumentType)imageType.Value)
            {
                case EmAppDocumentType.JPG: ext = ".jpg"; break;
                case EmAppDocumentType.GIF: ext = ".gif"; break;
                case EmAppDocumentType.BMP: ext = ".bmp"; break;
                case EmAppDocumentType.TIF: ext = ".tif"; break;
                case EmAppDocumentType.PNG: ext = ".png"; break;
            }
        }

        if (string.IsNullOrWhiteSpace(ext))
            return string.Empty;

        if (!ext.StartsWith(".", StringComparison.Ordinal))
            ext = "." + ext;

        return ext.Length <= 50 ? ext : ext.Substring(0, 50);
    }
}
