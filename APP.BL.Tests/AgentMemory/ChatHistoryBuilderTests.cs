#pragma warning disable SKEXP0001
using System.Runtime.CompilerServices;
using App.BL.AIAgent.GenericAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;

namespace APP.BL.Tests.AgentMemory;

public class ChatHistoryBuilderTests
{
    private sealed class FakeChat : IChatCompletionService
    {
        public List<string> Prompts { get; } = new();
        public bool Fail { get; set; }
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            Prompts.Add(chatHistory.Last().Content ?? "");
            if (Fail) throw new InvalidOperationException("boom");
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>(
                new List<ChatMessageContent> { new(AuthorRole.Assistant, "SUMMARY-" + Prompts.Count) });
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static (Kernel Kernel, FakeChat Chat) NewKernel()
    {
        var chat = new FakeChat();
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(chat);
        return (builder.Build(), chat);
    }

    private static List<JObject> Conversation(int turns, int charsPerTurn = 10)
    {
        var list = new List<JObject>();
        for (var i = 0; i < turns; i++)
            list.Add(new JObject
            {
                ["role"] = i % 2 == 0 ? "user" : "assistant",
                ["content"] = $"t{i}-" + new string('x', charsPerTurn)
            });
        return list;
    }

    private static string TextOf(ChatMessageContent m) => m.Content ?? "";

    [Fact]
    public async Task Under_threshold_sends_everything_verbatim_without_a_summary_call()
    {
        var (kernel, chat) = NewKernel();
        var history = await ChatHistoryBuilder.BuildAsync(Conversation(30), 10, 60000, kernel);

        Assert.Equal(30, history.Count);
        Assert.Empty(chat.Prompts);
        Assert.DoesNotContain(history, m => m.Role == AuthorRole.System);
    }

    [Fact]
    public async Task Over_threshold_summarises_old_turns_once_and_persists_it()
    {
        var (kernel, chat) = NewKernel();
        RollingSummary? saved = null;
        var msgs = Conversation(30, 400);

        var history = await ChatHistoryBuilder.BuildAsync(msgs, 10, 500, kernel, null, s => saved = s);

        Assert.Single(chat.Prompts);
        Assert.NotNull(saved);
        Assert.Equal(20, saved!.TurnCount);
        Assert.Equal("SUMMARY-1", saved.Text);
        Assert.Equal(AuthorRole.System, history[0].Role);
        Assert.Contains("SUMMARY-1", TextOf(history[0]));
        Assert.Equal(11, history.Count);
        Assert.StartsWith("t20-", TextOf(history[1]));
    }

    [Fact]
    public async Task Stored_summary_is_reused_while_the_verbatim_tail_stays_within_two_windows()
    {
        var (kernel, chat) = NewKernel();
        var msgs = Conversation(35, 400);
        var stored = new RollingSummary { Text = "OLD", TurnCount = 20, Fingerprint = ChatHistoryBuilder.Fingerprint("t0-" + new string('x', 400)) };
        var persisted = false;

        var history = await ChatHistoryBuilder.BuildAsync(msgs, 10, 500, kernel, stored, _ => persisted = true);

        Assert.Empty(chat.Prompts);
        Assert.False(persisted);
        Assert.Contains("OLD", TextOf(history[0]));
        Assert.Equal(1 + 15, history.Count);
        Assert.StartsWith("t20-", TextOf(history[1]));
    }

    [Fact]
    public async Task Summary_is_refreshed_incrementally_when_the_tail_outgrows_two_windows()
    {
        var (kernel, chat) = NewKernel();
        var msgs = Conversation(45, 400);
        var stored = new RollingSummary { Text = "OLD", TurnCount = 20, Fingerprint = ChatHistoryBuilder.Fingerprint("t0-" + new string('x', 400)) };
        RollingSummary? saved = null;

        var history = await ChatHistoryBuilder.BuildAsync(msgs, 10, 500, kernel, stored, s => saved = s);

        Assert.Single(chat.Prompts);
        Assert.Contains("OLD", chat.Prompts[0]);
        Assert.Contains("t20-", chat.Prompts[0]);
        Assert.DoesNotContain("t0-", chat.Prompts[0]);
        Assert.Equal(35, saved!.TurnCount);
        Assert.Equal(1 + 10, history.Count);
    }

    [Fact]
    public async Task Stored_summary_with_a_different_first_turn_is_ignored()
    {
        var (kernel, chat) = NewKernel();
        var msgs = Conversation(30, 400);
        var stored = new RollingSummary { Text = "STALE", TurnCount = 20, Fingerprint = "does-not-match" };

        var history = await ChatHistoryBuilder.BuildAsync(msgs, 10, 500, kernel, stored);

        Assert.Single(chat.Prompts);
        Assert.DoesNotContain("STALE", TextOf(history[0]));
    }

    [Fact]
    public async Task Failed_refresh_keeps_the_previous_summary_and_does_not_persist()
    {
        var (kernel, chat) = NewKernel();
        chat.Fail = true;
        var msgs = Conversation(45, 400);
        var stored = new RollingSummary { Text = "OLD", TurnCount = 20, Fingerprint = ChatHistoryBuilder.Fingerprint("t0-" + new string('x', 400)) };
        var persisted = false;

        var history = await ChatHistoryBuilder.BuildAsync(msgs, 10, 500, kernel, stored, _ => persisted = true);

        Assert.False(persisted);
        Assert.Contains("OLD", TextOf(history[0]));
        Assert.StartsWith("t20-", TextOf(history[1]));
    }

    [Fact]
    public void Assistant_turn_keeps_a_compact_note_of_the_tools_it_ran()
    {
        var msgs = new List<JObject>
        {
            new() { ["role"] = "user", ["content"] = "list styles" },
            new()
            {
                ["role"] = "assistant", ["content"] = "Found 3 styles.",
                ["toolSteps"] = new JArray(new JObject
                {
                    ["toolName"] = "plm_get_styles", ["args"] = "{\"season\":\"SS27\"}", ["isSuccess"] = true, ["result"] = "3 rows"
                })
            }
        };

        var turns = ChatHistoryBuilder.ToTextTurns(msgs);

        Assert.Equal(2, turns.Count);
        Assert.Contains("Found 3 styles.", turns[1].Text);
        Assert.Contains("plm_get_styles", turns[1].Text);
        Assert.Contains("3 rows", turns[1].Text);
    }

    [Fact]
    public void Assistant_turn_with_only_tool_steps_is_kept_and_array_content_is_skipped()
    {
        var msgs = new List<JObject>
        {
            new() { ["role"] = "assistant", ["content"] = "", ["toolSteps"] = new JArray(new JObject { ["toolName"] = "t1", ["isSuccess"] = false }) },
            new() { ["role"] = "assistant", ["content"] = new JArray() }
        };

        var turns = ChatHistoryBuilder.ToTextTurns(msgs);

        Assert.Single(turns);
        Assert.Contains("t1", turns[0].Text);
        Assert.Contains("failed", turns[0].Text);
    }
}
