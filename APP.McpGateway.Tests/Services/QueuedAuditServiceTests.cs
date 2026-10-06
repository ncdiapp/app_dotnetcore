using McpGateway.Models;
using McpGateway.Services;
using McpGateway.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpGateway.Tests.Services;

public class QueuedAuditServiceTests
{
    private sealed class FakeSink : IAuditSink
    {
        public object? Target { get; set; } = "tenant-1";
        public bool FailFirstWrite { get; set; }
        public TimeSpan WriteDelay { get; set; }
        public List<(object Target, List<AuditEntry> Entries)> Writes { get; } = [];
        private int _calls;

        public object? CaptureTarget() => Target;

        public async Task WriteAsync(object target, IReadOnlyList<AuditEntry> entries, CancellationToken ct)
        {
            if (WriteDelay > TimeSpan.Zero) await Task.Delay(WriteDelay, ct);
            if (FailFirstWrite && Interlocked.Increment(ref _calls) == 1)
                throw new InvalidOperationException("db down");
            lock (Writes) Writes.Add((target, entries.ToList()));
        }

        public List<AuditEntry> AllEntries() { lock (Writes) return Writes.SelectMany(w => w.Entries).ToList(); }
    }

    private static QueuedAuditService Create(FakeSink sink, FakeCallerContext? caller = null, Dictionary<string, string?>? config = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        return new QueuedAuditService(
            sink,
            caller ?? FakeCallerContext.For(5, 50),
            accessor.Object,
            new ConfigurationBuilder().AddInMemoryCollection(config ?? []).Build(),
            NullLogger<QueuedAuditService>.Instance);
    }

    private static AuditEntry Entry(string action = "a") =>
        new() { Code = AuditCode.A021_ApiCrudAction, Action = action };

    [Fact]
    public async Task Events_are_stamped_with_the_authenticated_caller_and_written()
    {
        var sink = new FakeSink();
        var svc = Create(sink, FakeCallerContext.For(5, 50));
        await svc.StartAsync(CancellationToken.None);

        svc.Log(Entry());
        await svc.StopAsync(CancellationToken.None);

        var written = Assert.Single(sink.AllEntries());
        Assert.Equal(5, written.CompanyId);
        Assert.Equal(50, written.UserId);
    }

    [Fact]
    public async Task Caller_identity_cannot_be_overridden_by_the_entry_when_already_set_by_the_service_only()
    {
        // The service fills identity from the validated token; an entry with no identity gets exactly that identity.
        var sink = new FakeSink();
        var caller = FakeCallerContext.For(5, 50);
        var svc = Create(sink, caller);
        await svc.StartAsync(CancellationToken.None);

        svc.Log(Entry("first"));
        caller.UserId = 51;
        svc.Log(Entry("second"));
        await svc.StopAsync(CancellationToken.None);

        var byAction = sink.AllEntries().ToDictionary(e => e.Action, e => e.UserId);
        Assert.Equal(50, byAction["first"]);
        Assert.Equal(51, byAction["second"]);
    }

    [Fact]
    public async Task Events_for_different_tenants_are_written_in_separate_groups()
    {
        var sink = new FakeSink();
        var svc = Create(sink);
        await svc.StartAsync(CancellationToken.None);

        sink.Target = "tenant-1"; svc.Log(Entry("a1"));
        sink.Target = "tenant-2"; svc.Log(Entry("b1"));
        sink.Target = "tenant-1"; svc.Log(Entry("a2"));
        await svc.StopAsync(CancellationToken.None);

        lock (sink.Writes)
        {
            foreach (var (target, entries) in sink.Writes)
            {
                var expectedPrefix = (string)target == "tenant-1" ? "a" : "b";
                Assert.All(entries, e => Assert.StartsWith(expectedPrefix, e.Action));
            }
        }
        Assert.Equal(3, sink.AllEntries().Count);
    }

    [Fact]
    public async Task Event_without_a_tenant_context_is_not_persisted()
    {
        var sink = new FakeSink { Target = null };
        var svc = Create(sink);
        await svc.StartAsync(CancellationToken.None);

        svc.Log(Entry());
        await svc.StopAsync(CancellationToken.None);

        Assert.Empty(sink.AllEntries());
    }

    [Fact]
    public async Task Disabled_service_writes_nothing()
    {
        var sink = new FakeSink();
        var svc = Create(sink, config: new() { ["Audit:Enabled"] = "false" });
        await svc.StartAsync(CancellationToken.None);

        svc.Log(Entry());
        await svc.StopAsync(CancellationToken.None);

        Assert.False(svc.IsEnabled);
        Assert.Empty(sink.AllEntries());
    }

    [Fact]
    public void Full_queue_discards_new_events_and_counts_them()
    {
        // Service is not started, so nothing consumes the queue.
        var svc = Create(new FakeSink(), config: new() { ["Audit:QueueCapacity"] = "2" });

        for (var i = 0; i < 5; i++) svc.Log(Entry($"e{i}"));

        Assert.Equal(3, svc.DroppedCount);
    }

    [Fact]
    public async Task A_failed_write_does_not_stop_later_events()
    {
        var sink = new FakeSink { FailFirstWrite = true };
        // Batch size 1 so the first event is its own (failing) write.
        var svc = Create(sink, config: new() { ["Audit:BatchSize"] = "1" });
        await svc.StartAsync(CancellationToken.None);

        svc.Log(Entry("lost"));
        svc.Log(Entry("kept"));
        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(["kept"], sink.AllEntries().Select(e => e.Action));
    }

    [Fact]
    public async Task Queued_events_are_drained_on_shutdown()
    {
        var sink = new FakeSink { WriteDelay = TimeSpan.FromMilliseconds(20) };
        var svc = Create(sink, config: new() { ["Audit:BatchSize"] = "5" });
        await svc.StartAsync(CancellationToken.None);

        for (var i = 0; i < 40; i++) svc.Log(Entry($"e{i}"));
        await svc.StopAsync(CancellationToken.None);

        Assert.Equal(40, sink.AllEntries().Count);
    }

    [Fact]
    public async Task Runtime_toggle_is_not_persisted_and_takes_effect()
    {
        var sink = new FakeSink();
        var svc = Create(sink);
        await svc.StartAsync(CancellationToken.None);

        await svc.SetEnabledAsync(false, persist: true);
        svc.Log(Entry());
        await svc.StopAsync(CancellationToken.None);

        Assert.Empty(sink.AllEntries());
    }
}
