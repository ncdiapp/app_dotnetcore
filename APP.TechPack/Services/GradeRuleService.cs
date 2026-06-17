using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using APP.TechPack.Engine;

namespace APP.TechPack.Services;

public static class GradeRuleService
{
    private static readonly IGradingEngine _engine = new GradingEngine();

    /// <summary>
    /// Applies a grade rule set to every non-fixed POM spec line in a style spec.
    /// Upserts TchpGradeValue rows for the active dimension's size run.
    /// </summary>
    public static void ApplyRuleSetToSpec(string connectionString, int ruleSetId, int styleSpecId)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        var rules = LoadGradeRules(conn, ruleSetId);
        var (baseSizeDetailId, sizeRunId) = LoadSpecHeader(conn, styleSpecId);
        var dimensionCode = LoadActiveDimensionCode(conn, styleSpecId);
        var sizes = LoadDimensionSizes(conn, sizeRunId, dimensionCode);

        int sizeCount = sizes.Count;
        int baseSizeIndex = sizes.FindIndex(s => s.SizeRunSizeId == baseSizeDetailId);
        if (baseSizeIndex < 0)
            throw new InvalidOperationException(
                $"BaseSizeDetailId {baseSizeDetailId} not found in active dimension '{dimensionCode}' for spec {styleSpecId}.");

        var specLines = LoadNonFixedSpecLines(conn, styleSpecId);

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var (pomSpecLineId, bodyPartCode) in specLines)
            {
                IReadOnlyList<decimal> deltas;
                try
                {
                    deltas = _engine.ApplyGradeRuleSet(rules, bodyPartCode, sizeCount, baseSizeIndex);
                }
                catch (InvalidOperationException)
                {
                    // No rule for this body part — leave existing deltas unchanged
                    continue;
                }

                for (int i = 0; i < sizes.Count; i++)
                    UpsertGradeValue(conn, tx, pomSpecLineId, sizes[i].SizeRunSizeId, deltas[i]);
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Returns which body part codes in the spec have a matching rule in the rule set.
    /// </summary>
    public static GradeRuleApplyCoverage GetCoverage(string connectionString, int ruleSetId, int styleSpecId)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        var ruleBodyPartCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT BodyPartCode FROM TchpGradeRule WHERE GradeRuleSetId = @ruleSetId";
            cmd.Parameters.Add(new SqlParameter("@ruleSetId", SqlDbType.Int) { Value = ruleSetId });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                ruleBodyPartCodes.Add(reader.GetString(0));
        }

        var specBodyPartCodes = new List<string>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT DISTINCT bp.Code " +
                "FROM TchpPomSpecLine psl " +
                "JOIN TchpBodyPart bp ON bp.BodyPartId = psl.BodyPartId " +
                "WHERE psl.StyleSpecId = @styleSpecId AND psl.IsFixed = 0";
            cmd.Parameters.Add(new SqlParameter("@styleSpecId", SqlDbType.Int) { Value = styleSpecId });
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                specBodyPartCodes.Add(reader.GetString(0));
        }

        var coverage = new GradeRuleApplyCoverage { TotalSpecLines = specBodyPartCodes.Count };
        foreach (var code in specBodyPartCodes)
        {
            if (ruleBodyPartCodes.Contains(code))
            {
                coverage.MatchedBodyPartCodes.Add(code);
                coverage.MatchedSpecLines++;
            }
            else
            {
                coverage.UnmatchedBodyPartCodes.Add(code);
            }
        }
        return coverage;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static IReadOnlyList<GradeRuleInput> LoadGradeRules(SqlConnection conn, int ruleSetId)
    {
        var rules = new List<GradeRuleInput>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT BodyPartCode, GradingPlusValue, GradingMinuValue " +
            "FROM TchpGradeRule WHERE GradeRuleSetId = @ruleSetId";
        cmd.Parameters.Add(new SqlParameter("@ruleSetId", SqlDbType.Int) { Value = ruleSetId });
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            rules.Add(new GradeRuleInput(
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2)));
        return rules;
    }

    private static (int BaseSizeDetailId, int SizeRunId) LoadSpecHeader(SqlConnection conn, int styleSpecId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT BaseSizeDetailId, SizeRunId FROM TchpStyleSpec WHERE StyleSpecId = @styleSpecId";
        cmd.Parameters.Add(new SqlParameter("@styleSpecId", SqlDbType.Int) { Value = styleSpecId });
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            throw new InvalidOperationException($"StyleSpec {styleSpecId} not found.");
        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    private static string LoadActiveDimensionCode(SqlConnection conn, int styleSpecId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT TOP 1 DimensionCode FROM TchpStyleSpecDimension " +
            "WHERE StyleSpecId = @styleSpecId AND IsActive = 1";
        cmd.Parameters.Add(new SqlParameter("@styleSpecId", SqlDbType.Int) { Value = styleSpecId });
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            throw new InvalidOperationException($"No active dimension found for StyleSpec {styleSpecId}.");
        return (string)result;
    }

    private record SizeEntry(int SizeRunSizeId, string SizeLabel);

    private static List<SizeEntry> LoadDimensionSizes(SqlConnection conn, int sizeRunId, string dimensionCode)
    {
        var sizes = new List<SizeEntry>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT srs.SizeRunSizeId, srs.SizeLabel " +
            "FROM TchpSizeRunSize srs " +
            "JOIN TchpSizeRunDimension srd ON srd.SizeRunSizeId = srs.SizeRunSizeId " +
            "WHERE srd.SizeRunId = @sizeRunId AND srd.DimensionCode = @dimensionCode " +
            "ORDER BY srd.SortOrder";
        cmd.Parameters.Add(new SqlParameter("@sizeRunId", SqlDbType.Int) { Value = sizeRunId });
        cmd.Parameters.Add(new SqlParameter("@dimensionCode", SqlDbType.NVarChar, 20) { Value = dimensionCode });
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            sizes.Add(new SizeEntry(reader.GetInt32(0), reader.GetString(1)));
        return sizes;
    }

    private static List<(int PomSpecLineId, string BodyPartCode)> LoadNonFixedSpecLines(
        SqlConnection conn, int styleSpecId)
    {
        var lines = new List<(int, string)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT psl.PomSpecLineId, bp.Code " +
            "FROM TchpPomSpecLine psl " +
            "JOIN TchpBodyPart bp ON bp.BodyPartId = psl.BodyPartId " +
            "WHERE psl.StyleSpecId = @styleSpecId AND psl.IsFixed = 0";
        cmd.Parameters.Add(new SqlParameter("@styleSpecId", SqlDbType.Int) { Value = styleSpecId });
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            lines.Add((reader.GetInt32(0), reader.GetString(1)));
        return lines;
    }

    private static void UpsertGradeValue(
        SqlConnection conn, SqlTransaction tx,
        int pomSpecLineId, int sizeRunSizeId, decimal gradingDelta)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "MERGE TchpGradeValue AS target " +
            "USING (VALUES (@pomSpecLineId, @sizeRunSizeId, @gradingDelta)) " +
            "  AS source (PomSpecLineId, SizeRunSizeId, GradingDelta) " +
            "ON target.PomSpecLineId = source.PomSpecLineId " +
            "   AND target.SizeRunSizeId = source.SizeRunSizeId " +
            "WHEN MATCHED THEN " +
            "  UPDATE SET GradingDelta = source.GradingDelta " +
            "WHEN NOT MATCHED THEN " +
            "  INSERT (PomSpecLineId, SizeRunSizeId, GradingDelta) " +
            "  VALUES (source.PomSpecLineId, source.SizeRunSizeId, source.GradingDelta);";
        cmd.Parameters.Add(new SqlParameter("@pomSpecLineId", SqlDbType.Int) { Value = pomSpecLineId });
        cmd.Parameters.Add(new SqlParameter("@sizeRunSizeId", SqlDbType.Int) { Value = sizeRunSizeId });
        cmd.Parameters.Add(new SqlParameter("@gradingDelta", SqlDbType.Decimal) { Value = gradingDelta, Precision = 10, Scale = 3 });
        cmd.ExecuteNonQuery();
    }
}
