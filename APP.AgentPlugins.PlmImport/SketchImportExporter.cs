using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Phase B2: read PLM tblSketch and write binaries to a Host-provided staging folder.
/// Host inserts AppFile from the manifest (no APP.BL in this assembly).
/// </summary>
public static class SketchImportExporter
{
    /// <param name="skipFileIds">AppFile FileIDs that already exist — skipped without writing staging files.</param>
    /// <param name="progress">percent 0–99 during export; may throw OperationCanceledException.</param>
    public static PlmSketchStagingManifestDto Export(
        string plmConnectionString,
        string stagingRootAbsolute,
        int[] skipFileIds,
        Action<int, string> progress)
    {
        var manifest = new PlmSketchStagingManifestDto
        {
            StagingRoot = stagingRootAbsolute
        };

        if (string.IsNullOrWhiteSpace(plmConnectionString))
        {
            manifest.IsSuccess = false;
            manifest.ErrorMessage = "plmConnectionString is required.";
            return manifest;
        }
        if (string.IsNullOrWhiteSpace(stagingRootAbsolute))
        {
            manifest.IsSuccess = false;
            manifest.ErrorMessage = "stagingRootAbsolute is required.";
            return manifest;
        }

        var skip = new HashSet<int>(skipFileIds ?? Array.Empty<int>());
        Directory.CreateDirectory(stagingRootAbsolute);

        try
        {
            using (var plmConn = new SqlConnection(plmConnectionString.Trim()))
            {
                plmConn.Open();
                EnsurePlmSketchSchema(plmConn);
                int total = CountSourceSketches(plmConn);
                manifest.SourceSketchCount = total;

                using (var cmd = plmConn.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = @"
SELECT
    SketchID,
    SketchCode,
    SketchImage,
    Thumbnail,
    ImageType,
    OriginalImage,
    CreatedDate,
    ModifyDate,
    Description,
    Extension
FROM dbo.tblSketch
ORDER BY SketchID;";

                    using (var reader = cmd.ExecuteReader())
                    {
                        int processed = 0;
                        while (reader.Read())
                        {
                            processed++;
                            if (processed == 1 || processed % 50 == 0)
                            {
                                int percent = total == 0 ? 99 : Math.Min(99, (int)Math.Round(processed * 50.0 / total));
                                progress?.Invoke(percent, $"Exporting sketch {processed} of {total} from PLM...");
                            }

                            int sketchId = reader.GetInt32(0);
                            if (skip.Contains(sketchId))
                            {
                                manifest.SkippedExistingCount++;
                                continue;
                            }

                            byte[] sketchImage = reader.IsDBNull(2) ? null : (byte[])reader["SketchImage"];
                            byte[] thumbnail = reader.IsDBNull(3) ? null : (byte[])reader["Thumbnail"];
                            byte[] originalImage = reader.IsDBNull(5) ? null : (byte[])reader["OriginalImage"];
                            if (sketchImage == null && thumbnail == null && originalImage == null)
                            {
                                manifest.SkippedMissingBinaryCount++;
                                continue;
                            }

                            string extension = NormalizeExtension(
                                reader.IsDBNull(9) ? null : reader.GetString(9),
                                reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader["ImageType"]));
                            int fileType = (int)MapDocumentType(extension);
                            bool isImage = IsImageExtension(extension);

                            string itemDir = Path.Combine(stagingRootAbsolute, sketchId.ToString());
                            Directory.CreateDirectory(itemDir);

                            var item = new PlmSketchStagingItemDto
                            {
                                SketchId = sketchId,
                                SketchCode = Truncate(reader.IsDBNull(1) ? null : reader.GetString(1), 200),
                                Description = Truncate(reader.IsDBNull(8) ? null : reader.GetString(8), 100),
                                Extension = extension,
                                FileType = fileType,
                                IsImage = isImage,
                                CreatedDate = reader.IsDBNull(6) ? (DateTime?)null : Convert.ToDateTime(reader["CreatedDate"]),
                                ModifyDate = reader.IsDBNull(7) ? (DateTime?)null : Convert.ToDateTime(reader["ModifyDate"])
                            };

                            if (isImage)
                            {
                                if (originalImage != null)
                                    item.OriginalStagingPath = WriteBytes(itemDir, "original.bin", originalImage);
                                if (thumbnail != null)
                                    item.ThumbnailStagingPath = WriteBytes(itemDir, "thumbnail.bin", thumbnail);
                                if (sketchImage != null)
                                    item.RegularStagingPath = WriteBytes(itemDir, "regular.bin", sketchImage);
                            }
                            else
                            {
                                byte[] content = originalImage ?? sketchImage ?? thumbnail;
                                item.ContentStagingPath = WriteBytes(itemDir, "content.bin", content);
                            }

                            manifest.Items.Add(item);
                            manifest.ExportedCount++;
                        }
                    }
                }
            }

            manifest.IsSuccess = true;
            progress?.Invoke(50, $"PLM export complete. {manifest.ExportedCount} file(s) staged.");
            return manifest;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            manifest.IsSuccess = false;
            manifest.ErrorMessage = ex.Message;
            return manifest;
        }
    }

    private static string WriteBytes(string directory, string fileName, byte[] bytes)
    {
        string path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, bytes);
        return path;
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

    private static int CountSourceSketches(SqlConnection conn)
    {
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM dbo.tblSketch;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
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

    private static EmAppDocumentType MapDocumentType(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return EmAppDocumentType.Unknown;
        switch (extension.Trim().ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg": return EmAppDocumentType.JPG;
            case ".gif": return EmAppDocumentType.GIF;
            case ".bmp": return EmAppDocumentType.BMP;
            case ".tif":
            case ".tiff": return EmAppDocumentType.TIF;
            case ".png": return EmAppDocumentType.PNG;
            case ".pdf": return EmAppDocumentType.PDF;
            case ".doc":
            case ".docx": return EmAppDocumentType.WORD;
            case ".xls":
            case ".xlsx": return EmAppDocumentType.EXCEL;
            default: return EmAppDocumentType.Unknown;
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

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
