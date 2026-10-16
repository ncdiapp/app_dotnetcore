using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace APP.OAuth.Tests;

/// <summary>[Fact] that is skipped (not failed) when SQL Server LocalDB is unavailable on the machine.</summary>
public sealed class LocalDbFactAttribute : FactAttribute
{
    public LocalDbFactAttribute()
    {
        if (!LocalDbFixture.Available) Skip = "SQL Server LocalDB is not available.";
    }
}

/// <summary>One throwaway database per test class, built by running the real master-DB script.</summary>
public sealed class LocalDbFixture : IDisposable
{
    private const string Server = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;Connect Timeout=15";

    public static readonly bool Available = Probe();

    private readonly string _dbName = "OAuthTest_" + Guid.NewGuid().ToString("N");

    public string ConnectionString => Server + ";Initial Catalog=" + _dbName;
    public IOptions<OAuthDbOptions> Options => Microsoft.Extensions.Options.Options.Create(new OAuthDbOptions { ConnectionString = ConnectionString });

    public LocalDbFixture()
    {
        if (!Available) return;
        Execute(Server + ";Initial Catalog=master", "CREATE DATABASE [" + _dbName + "]");

        var script = File.ReadAllText(FindScript()).Replace("USE AppMasterDB;", string.Empty);
        foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            if (!string.IsNullOrWhiteSpace(batch)) Execute(ConnectionString, batch);
    }

    public void Dispose()
    {
        if (!Available) return;
        SqlConnection.ClearAllPools();
        try { Execute(Server + ";Initial Catalog=master", "ALTER DATABASE [" + _dbName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + _dbName + "]"); }
        catch (SqlException) { /* a leftover throwaway database is harmless */ }
    }

    public void Sql(string sql) => Execute(ConnectionString, sql);

    public T Scalar<T>(string sql)
    {
        using var c = new SqlConnection(ConnectionString);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        return (T)Convert.ChangeType(cmd.ExecuteScalar()!, typeof(T));
    }

    private static void Execute(string connectionString, string sql)
    {
        using var c = new SqlConnection(connectionString);
        c.Open();
        using var cmd = new SqlCommand(sql, c);
        cmd.ExecuteNonQuery();
    }

    private static bool Probe()
    {
        try { Execute(Server + ";Initial Catalog=master", "SELECT 1"); return true; }
        catch (Exception) { return false; }
    }

    private static string FindScript()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Document", "Design", "MasterDB_OAuthTables.sql");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Document/Design/MasterDB_OAuthTables.sql not found above " + AppContext.BaseDirectory);
    }
}
