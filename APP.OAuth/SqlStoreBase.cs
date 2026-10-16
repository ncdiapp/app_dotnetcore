using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>
/// Shared ADO.NET plumbing for the four OpenIddict stores: parameterized SQL only, one pooled connection per call,
/// optimistic concurrency on ConcurrencyToken. Each store supplies its table, columns and row mapping.
/// </summary>
internal abstract class SqlStoreBase<T> where T : OAuthEntity, new()
{
    private readonly string _connectionString;

    protected SqlStoreBase(IOptions<OAuthDbOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("OAuth:ConnectionString (AppMasterDB) is not configured.");
    }

    protected abstract string Table { get; }
    protected abstract string IdColumn { get; }
    /// <summary>Data columns in the same order as <see cref="Values"/>; excludes the id and ConcurrencyToken.</summary>
    protected abstract string[] DataColumns { get; }
    protected abstract object?[] Values(T entity);
    protected abstract void Read(SqlDataReader reader, T entity);

    private string SelectList => IdColumn + ", " + string.Join(", ", DataColumns) + ", ConcurrencyToken";

    // ── connection / command helpers ────────────────────────────────────────────

    protected async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    protected static SqlParameter P(string name, object? value)
    {
        switch (value)
        {
            case null: return new SqlParameter(name, DBNull.Value);
            case string s: return new SqlParameter(name, SqlDbType.NVarChar, s.Length <= 4000 ? 4000 : -1) { Value = s };
            case DateTimeOffset d: return new SqlParameter(name, SqlDbType.DateTime2) { Value = d.UtcDateTime };
            case DateTime dt: return new SqlParameter(name, SqlDbType.DateTime2) { Value = dt };
            default: return new SqlParameter(name, value);
        }
    }

    protected static SqlCommand Command(SqlConnection c, string sql, IEnumerable<SqlParameter>? ps, SqlTransaction? tx = null)
    {
        var cmd = new SqlCommand(sql, c, tx);
        if (ps != null) foreach (var p in ps) cmd.Parameters.Add(p);
        return cmd;
    }

    protected async Task<long> ExecAsync(string sql, IEnumerable<SqlParameter>? ps, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = Command(c, sql, ps);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    protected async Task<long> ScalarLongAsync(string sql, IEnumerable<SqlParameter>? ps, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = Command(c, sql, ps);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0L);
    }

    // ── queries ─────────────────────────────────────────────────────────────────

    protected async Task<List<T>> QueryAsync(string? where, IEnumerable<SqlParameter>? ps, CancellationToken ct,
        int? count = null, int? offset = null)
    {
        var sql = "SELECT " + SelectList + " FROM dbo." + Table + (where is null ? "" : " WHERE " + where);
        var parameters = ps?.ToList() ?? new List<SqlParameter>();
        if (count.HasValue || offset.HasValue)
        {
            sql += " ORDER BY " + IdColumn + " OFFSET @off ROWS" + (count.HasValue ? " FETCH NEXT @cnt ROWS ONLY" : "");
            parameters.Add(P("@off", offset ?? 0));
            if (count.HasValue) parameters.Add(P("@cnt", count.Value));
        }

        await using var c = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = Command(c, sql, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false)) rows.Add(Map(reader));
        return rows;
    }

    protected async Task<T?> QueryOneAsync(string where, IEnumerable<SqlParameter> ps, CancellationToken ct) =>
        (await QueryAsync(where, ps, ct).ConfigureAwait(false)).FirstOrDefault();

    protected static async IAsyncEnumerable<T> Stream(Task<List<T>> rows, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var row in await rows.ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            yield return row;
        }
    }

    private T Map(SqlDataReader r)
    {
        var token = S(r, "ConcurrencyToken") ?? string.Empty;
        var e = new T { Id = r.GetString(r.GetOrdinal(IdColumn)), ConcurrencyToken = token };
        Read(r, e);
        return e;
    }

    /// <summary>WHERE clause for the optional subject/client/status/type filters shared by authorizations and tokens.</summary>
    protected static string Filter(List<SqlParameter> ps, string? subject, string? client, string? status, string? type)
    {
        var parts = new List<string>();
        void Add(string column, string name, string? value)
        {
            if (value is null) return;
            parts.Add(column + " = " + name);
            ps.Add(P(name, value));
        }
        Add("Subject", "@subject", subject);
        Add("ApplicationId", "@client", client);
        Add("Status", "@status", status);
        Add("Type", "@type", type);
        return parts.Count == 0 ? "1 = 1" : string.Join(" AND ", parts);
    }

    /// <summary>Marks every matching row revoked and rotates its ConcurrencyToken so a concurrent writer fails.</summary>
    protected Task<long> RevokeWhereAsync(string where, IEnumerable<SqlParameter> ps, CancellationToken ct) =>
        ExecAsync("UPDATE dbo." + Table + " SET Status = N'" + OpenIddictConstants.Statuses.Revoked + "', " +
                  "ConcurrencyToken = CONVERT(NVARCHAR(50), NEWID()) WHERE " + where, ps, ct);

    // ── reader helpers for the stores ───────────────────────────────────────────

    protected static string? S(SqlDataReader r, string column) { var i = r.GetOrdinal(column); return r.IsDBNull(i) ? null : r.GetString(i); }
    protected static int? I(SqlDataReader r, string column) { var i = r.GetOrdinal(column); return r.IsDBNull(i) ? null : r.GetInt32(i); }
    protected static DateTimeOffset? D(SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc));
    }

    // ── create / update / delete / count (the members every store shares) ──────

    public virtual async ValueTask CreateAsync(T entity, CancellationToken ct)
    {
        entity.ConcurrencyToken = Guid.NewGuid().ToString("N");
        var names = DataColumns.Select((_, i) => "@p" + i);
        var sql = "INSERT INTO dbo." + Table + " (" + IdColumn + ", " + string.Join(", ", DataColumns) + ", ConcurrencyToken) " +
                  "VALUES (@id, " + string.Join(", ", names) + ", @token)";
        var ps = Values(entity).Select((v, i) => P("@p" + i, v)).Append(P("@id", entity.Id)).Append(P("@token", entity.ConcurrencyToken));
        await ExecAsync(sql, ps, ct).ConfigureAwait(false);
    }

    public virtual async ValueTask UpdateAsync(T entity, CancellationToken ct)
    {
        var expected = entity.ConcurrencyToken;
        var next = Guid.NewGuid().ToString("N");
        var sets = DataColumns.Select((c, i) => c + " = @p" + i);
        var sql = "UPDATE dbo." + Table + " SET " + string.Join(", ", sets) + ", ConcurrencyToken = @next " +
                  "WHERE " + IdColumn + " = @id AND ConcurrencyToken = @expected";
        var ps = Values(entity).Select((v, i) => P("@p" + i, v))
            .Append(P("@next", next)).Append(P("@id", entity.Id)).Append(P("@expected", expected));
        if (await ExecAsync(sql, ps, ct).ConfigureAwait(false) == 0)
            throw new OpenIddictExceptions.ConcurrencyException(Table + " " + entity.Id + " was modified or deleted by another request.");
        entity.ConcurrencyToken = next;
    }

    public virtual async ValueTask DeleteAsync(T entity, CancellationToken ct)
    {
        var deleted = await ExecAsync("DELETE FROM dbo." + Table + " WHERE " + IdColumn + " = @id AND ConcurrencyToken = @token",
            new[] { P("@id", entity.Id), P("@token", entity.ConcurrencyToken) }, ct).ConfigureAwait(false);
        if (deleted == 0)
            throw new OpenIddictExceptions.ConcurrencyException(Table + " " + entity.Id + " was modified or deleted by another request.");
    }

    public async ValueTask<long> CountAsync(CancellationToken ct) =>
        await ScalarLongAsync("SELECT COUNT_BIG(*) FROM dbo." + Table, null, ct).ConfigureAwait(false);

    public ValueTask<T> InstantiateAsync(CancellationToken ct) => new(new T());

    public async ValueTask<T?> FindByIdAsync(string identifier, CancellationToken ct) =>
        await QueryOneAsync(IdColumn + " = @id", new[] { P("@id", identifier) }, ct).ConfigureAwait(false);

    public IAsyncEnumerable<T> ListAsync(int? count, int? offset, CancellationToken ct) =>
        Stream(QueryAsync(null, null, ct, count, offset), ct);

    // OpenIddict LINQ-query overloads cannot be translated to SQL. A spike running every flow (authorize, token,
    // refresh, replay, revoke, registration) never called them; if a future version does, fail loudly.
    protected static NotSupportedException QueryNotSupported(string store, string method) =>
        new(store + "." + method + " takes a LINQ query, which the ADO.NET OAuth store does not support.");
}
