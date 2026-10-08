using McpGateway.Services;
using McpGateway.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpGateway.Tests.Services;

public class ApiAccessPolicyTests
{
    private sealed class FakeProvider : IApiAccessProvider
    {
        public Dictionary<int, List<(string, string)>> ByUser { get; } = [];
        public bool Throw { get; set; }
        public int Calls { get; private set; }

        public Task<IReadOnlyCollection<(string AppSource, string OperationId)>> LoadAllowedAsync(
            int companyId, int userId, CancellationToken ct)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("db down");
            IReadOnlyCollection<(string, string)> list = ByUser.TryGetValue(userId, out var l) ? l : [];
            return Task.FromResult(list);
        }
    }

    private static ApiAccessPolicy Create(FakeProvider provider, FakeCallerContext caller, int ttlSeconds = 60) =>
        new(provider, caller,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Mcp:AccessCacheSeconds"] = ttlSeconds.ToString() }).Build(),
            NullLogger<ApiAccessPolicy>.Instance);

    [Fact]
    public async Task Granted_endpoint_is_allowed_and_everything_else_is_denied()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "Styles_List")] } };
        var policy = Create(provider, FakeCallerContext.For(1, 10));

        Assert.True(await policy.IsAllowedAsync("PLM", "Styles_List"));
        Assert.False(await policy.IsAllowedAsync("PLM", "Styles_Delete"));
        Assert.False(await policy.IsAllowedAsync("ERP", "Styles_List"));
    }

    [Fact]
    public async Task User_with_no_grants_is_denied_everything()
    {
        var policy = Create(new FakeProvider(), FakeCallerContext.For(1, 10));

        Assert.False(await policy.IsAllowedAsync("PLM", "Styles_List"));
    }

    [Fact]
    public async Task Without_an_authenticated_caller_everything_is_denied_and_the_provider_is_not_asked()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "Styles_List")] } };
        var policy = Create(provider, FakeCallerContext.Anonymous());

        Assert.False(await policy.IsAllowedAsync("PLM", "Styles_List"));
        Assert.Empty(await policy.FilterAsync(new[] { "x" }, _ => ("PLM", "Styles_List")));
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task Each_user_gets_only_their_own_grants()
    {
        var provider = new FakeProvider
        {
            ByUser = { [10] = [("PLM", "A")], [11] = [("PLM", "B")] }
        };
        var caller = FakeCallerContext.For(1, 10);
        var policy = Create(provider, caller);

        Assert.True(await policy.IsAllowedAsync("PLM", "A"));
        Assert.False(await policy.IsAllowedAsync("PLM", "B"));

        caller.UserId = 11;
        Assert.False(await policy.IsAllowedAsync("PLM", "A"));
        Assert.True(await policy.IsAllowedAsync("PLM", "B"));
    }

    [Fact]
    public async Task Same_user_id_in_another_company_is_a_different_cache_entry()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "A")] } };
        var caller = FakeCallerContext.For(1, 10);
        var policy = Create(provider, caller);

        await policy.IsAllowedAsync("PLM", "A");
        caller.CompanyId = 2;
        await policy.IsAllowedAsync("PLM", "A");

        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task Grants_are_cached_until_invalidated()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "A")] } };
        var policy = Create(provider, FakeCallerContext.For(1, 10));

        await policy.IsAllowedAsync("PLM", "A");
        await policy.IsAllowedAsync("PLM", "A");
        Assert.Equal(1, provider.Calls);

        provider.ByUser[10] = [];          // admin removed the grant
        Assert.True(await policy.IsAllowedAsync("PLM", "A"));   // still cached

        policy.InvalidateAll();
        Assert.False(await policy.IsAllowedAsync("PLM", "A"));  // takes effect immediately
        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task Zero_ttl_disables_caching()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "A")] } };
        var policy = Create(provider, FakeCallerContext.For(1, 10), ttlSeconds: 0);

        await policy.IsAllowedAsync("PLM", "A");
        await policy.IsAllowedAsync("PLM", "A");

        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task Failure_to_load_grants_denies_and_is_not_cached()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "A")] }, Throw = true };
        var policy = Create(provider, FakeCallerContext.For(1, 10));

        Assert.False(await policy.IsAllowedAsync("PLM", "A"));

        provider.Throw = false;
        Assert.True(await policy.IsAllowedAsync("PLM", "A"));
    }

    [Fact]
    public async Task Filter_keeps_only_granted_items_in_their_original_order()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("PLM", "A"), ("PLM", "C")] } };
        var policy = Create(provider, FakeCallerContext.For(1, 10));

        var result = await policy.FilterAsync(new[] { "A", "B", "C", "D" }, id => ("PLM", id));

        Assert.Equal(["A", "C"], result);
    }

    [Fact]
    public async Task Operation_matching_is_case_insensitive_so_a_casing_change_cannot_hide_a_grant()
    {
        var provider = new FakeProvider { ByUser = { [10] = [("plm", "styles_list")] } };
        var policy = Create(provider, FakeCallerContext.For(1, 10));

        Assert.True(await policy.IsAllowedAsync("PLM", "Styles_List"));
    }
}
