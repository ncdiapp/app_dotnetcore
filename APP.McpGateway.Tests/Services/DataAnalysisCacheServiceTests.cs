using System.Text.Json;
using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpGateway.Tests.Services;

public sealed class DataAnalysisCacheServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "mcp-cache-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private DataAnalysisCacheService Create(FakeCallerContext caller) =>
        new(NullLogger<DataAnalysisCacheService>.Instance, caller,
            Options.Create(new DataAnalysisSettings { CacheDirectory = _dir, MaxRows = 1000 }));

    private static JsonElement Rows(string json = "[{\"id\":1,\"name\":\"a\"}]") =>
        JsonDocument.Parse(json).RootElement.Clone();

    private static string Name(string op = "ops") => DataAnalysisCacheService.BuildDatasetName("PLM", op);

    // ── per-user (private) datasets ───────────────────────────────────────────

    [Fact]
    public async Task User_dataset_is_visible_to_the_same_user()
    {
        var caller = FakeCallerContext.For(1, 10);
        var cache = Create(caller);
        await cache.SyncFromJsonAsync("PLM", "ops", Rows());

        Assert.NotNull(await cache.GetCachedDatasetAsync(Name()));
    }

    [Fact]
    public async Task User_dataset_is_not_visible_to_another_user_in_the_same_company()
    {
        var caller = FakeCallerContext.For(1, 10);
        var cache = Create(caller);
        await cache.SyncFromJsonAsync("PLM", "ops", Rows());

        caller.UserId = 11;

        Assert.Null(await cache.GetCachedDatasetAsync(Name()));
    }

    [Fact]
    public async Task User_dataset_is_not_visible_to_the_same_user_id_in_another_company()
    {
        var caller = FakeCallerContext.For(1, 10);
        var cache = Create(caller);
        await cache.SyncFromJsonAsync("PLM", "ops", Rows());

        caller.CompanyId = 2;

        Assert.Null(await cache.GetCachedDatasetAsync(Name()));
    }

    // ── company-global datasets ───────────────────────────────────────────────

    [Fact]
    public async Task Global_dataset_is_shared_inside_one_company()
    {
        var caller = FakeCallerContext.For(1, 10);
        var cache = Create(caller);
        await cache.SyncFromJsonAsync("PLM", "countries", Rows(), isGlobal: true);

        caller.UserId = 11;

        Assert.NotNull(await cache.GetCachedDatasetAsync(Name("countries")));
    }

    [Fact]
    public async Task Global_dataset_is_not_visible_to_another_company()
    {
        var caller = FakeCallerContext.For(1, 10);
        var cache = Create(caller);
        await cache.SyncFromJsonAsync("PLM", "countries", Rows(), isGlobal: true);

        caller.CompanyId = 2;

        Assert.Null(await cache.GetCachedDatasetAsync(Name("countries")));
    }

    [Fact]
    public async Task Global_dataset_is_stored_under_the_company_folder_and_survives_a_restart()
    {
        await Create(FakeCallerContext.For(7, 10))
            .SyncFromJsonAsync("PLM", "countries", Rows(), isGlobal: true);

        Assert.True(File.Exists(Path.Combine(_dir, "7", Name("countries") + ".json")));

        var restarted = Create(FakeCallerContext.For(7, 99));
        Assert.NotNull(await restarted.GetCachedDatasetAsync(Name("countries")));

        var otherCompany = Create(FakeCallerContext.For(8, 99));
        Assert.Null(await otherCompany.GetCachedDatasetAsync(Name("countries")));
    }

    // ── no caller: never a shared fallback ────────────────────────────────────

    [Fact]
    public async Task Writing_without_an_authenticated_caller_throws()
    {
        var cache = Create(FakeCallerContext.Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => cache.SyncFromJsonAsync("PLM", "ops", Rows()));
    }

    [Fact]
    public async Task Reading_without_an_authenticated_caller_throws()
    {
        var cache = Create(FakeCallerContext.Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => cache.GetCachedDatasetAsync(Name()));
    }

    // ── dataset names reach a file path: only generated names are accepted ────

    [Theory]
    [InlineData("../secret")]
    [InlineData("..\\secret")]
    [InlineData("a/b")]
    [InlineData("a.b")]
    [InlineData("C:\\Windows\\win")]
    [InlineData("")]
    public async Task Unsafe_dataset_names_are_rejected(string name)
    {
        var cache = Create(FakeCallerContext.For(1, 10));

        Assert.Null(await cache.GetCachedDatasetAsync(name));
    }

    [Fact]
    public void Generated_dataset_names_are_always_safe()
    {
        Assert.True(DataAnalysisCacheService.IsSafeDatasetName(
            DataAnalysisCacheService.BuildDatasetName("My App", "get/../Items.v1")));
    }
}
