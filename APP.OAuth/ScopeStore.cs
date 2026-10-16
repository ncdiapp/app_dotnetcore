using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace APP.OAuth;

/// <summary>OpenIddict scope store on dbo.AppOAuthScope. A scope's Resources are the allowed token audiences (RFC 8707).</summary>
internal sealed class ScopeStore : SqlStoreBase<OAuthScope>, IOpenIddictScopeStore<OAuthScope>
{
    public ScopeStore(IOptions<OAuthDbOptions> options) : base(options) { }

    protected override string Table => "AppOAuthScope";
    protected override string IdColumn => "ScopeId";
    protected override string[] DataColumns { get; } =
        { "Name", "Description", "Descriptions", "DisplayName", "DisplayNames", "Properties", "Resources" };

    protected override object?[] Values(OAuthScope s) =>
        new object?[] { s.Name, s.Description, s.Descriptions, s.DisplayName, s.DisplayNames, s.Properties, s.Resources };

    protected override void Read(SqlDataReader r, OAuthScope s)
    {
        s.Name = S(r, "Name"); s.Description = S(r, "Description"); s.Descriptions = S(r, "Descriptions");
        s.DisplayName = S(r, "DisplayName"); s.DisplayNames = S(r, "DisplayNames");
        s.Properties = S(r, "Properties"); s.Resources = S(r, "Resources");
    }

    public async ValueTask<OAuthScope?> FindByNameAsync(string name, CancellationToken ct) =>
        await QueryOneAsync("Name = @name", new[] { P("@name", name) }, ct).ConfigureAwait(false);

    public IAsyncEnumerable<OAuthScope> FindByNamesAsync(ImmutableArray<string> names, CancellationToken ct)
    {
        if (names.IsDefaultOrEmpty) return Stream(Task.FromResult(new List<OAuthScope>()), ct);
        var parameters = names.Select((n, i) => P("@n" + i, n)).ToList();
        return Stream(QueryAsync("Name IN (" + string.Join(", ", parameters.Select(p => p.ParameterName)) + ")", parameters, ct), ct);
    }

    public IAsyncEnumerable<OAuthScope> FindByResourceAsync(string resource, CancellationToken ct) =>
        Stream(QueryAsync("EXISTS (SELECT 1 FROM OPENJSON(Resources) WHERE [value] = @resource COLLATE Latin1_General_100_BIN2)",
            new[] { P("@resource", resource) }, ct), ct);

    public ValueTask<long> CountAsync<TResult>(Func<IQueryable<OAuthScope>, IQueryable<TResult>> query, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ScopeStore), "CountAsync");

    public ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<OAuthScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ScopeStore), "GetAsync");

    public IAsyncEnumerable<TResult> ListAsync<TState, TResult>(Func<IQueryable<OAuthScope>, TState, IQueryable<TResult>> query, TState state, CancellationToken ct) =>
        throw QueryNotSupported(nameof(ScopeStore), "ListAsync");

    public ValueTask<string?> GetIdAsync(OAuthScope s, CancellationToken ct) => new(s.Id);
    public ValueTask<string?> GetNameAsync(OAuthScope s, CancellationToken ct) => new(s.Name);
    public ValueTask<string?> GetDescriptionAsync(OAuthScope s, CancellationToken ct) => new(s.Description);
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDescriptionsAsync(OAuthScope s, CancellationToken ct) => new(OAuthJson.Cultures(s.Descriptions));
    public ValueTask<string?> GetDisplayNameAsync(OAuthScope s, CancellationToken ct) => new(s.DisplayName);
    public ValueTask<ImmutableDictionary<CultureInfo, string>> GetDisplayNamesAsync(OAuthScope s, CancellationToken ct) => new(OAuthJson.Cultures(s.DisplayNames));
    public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(OAuthScope s, CancellationToken ct) => new(OAuthJson.Properties(s.Properties));
    public ValueTask<ImmutableArray<string>> GetResourcesAsync(OAuthScope s, CancellationToken ct) => new(OAuthJson.Array(s.Resources));

    public ValueTask SetNameAsync(OAuthScope s, string? v, CancellationToken ct) { s.Name = v; return default; }
    public ValueTask SetDescriptionAsync(OAuthScope s, string? v, CancellationToken ct) { s.Description = v; return default; }
    public ValueTask SetDescriptionsAsync(OAuthScope s, ImmutableDictionary<CultureInfo, string> v, CancellationToken ct) { s.Descriptions = OAuthJson.Cultures(v); return default; }
    public ValueTask SetDisplayNameAsync(OAuthScope s, string? v, CancellationToken ct) { s.DisplayName = v; return default; }
    public ValueTask SetDisplayNamesAsync(OAuthScope s, ImmutableDictionary<CultureInfo, string> v, CancellationToken ct) { s.DisplayNames = OAuthJson.Cultures(v); return default; }
    public ValueTask SetPropertiesAsync(OAuthScope s, ImmutableDictionary<string, JsonElement> v, CancellationToken ct) { s.Properties = OAuthJson.Properties(v); return default; }
    public ValueTask SetResourcesAsync(OAuthScope s, ImmutableArray<string> v, CancellationToken ct) { s.Resources = OAuthJson.Array(v); return default; }
}
