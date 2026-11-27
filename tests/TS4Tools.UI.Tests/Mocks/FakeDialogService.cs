using TS4Tools.UI.Services;

namespace TS4Tools.UI.Tests.Mocks;

/// <summary>
/// Scripted <see cref="IDialogService"/> for view model tests: answers are dequeued in order
/// and every prompt is recorded.
/// </summary>
public sealed class FakeDialogService : IDialogService
{
    public Queue<CommitPromptResult> CommitAnswers { get; } = new();

    public Queue<SavePromptResult> SaveAnswers { get; } = new();

    public Queue<string?> SavePaths { get; } = new();

    public List<string> CommitPrompts { get; } = [];

    public int SavePromptCount { get; private set; }

    public List<string> SavePathRequests { get; } = [];

    public Task<CommitPromptResult> AskCommitChangesAsync(string resourceName)
    {
        CommitPrompts.Add(resourceName);
        if (CommitAnswers.Count == 0)
            throw new InvalidOperationException($"Unexpected commit prompt for {resourceName}");
        return Task.FromResult(CommitAnswers.Dequeue());
    }

    public Task<SavePromptResult> AskSaveChangesAsync()
    {
        SavePromptCount++;
        if (SaveAnswers.Count == 0)
            throw new InvalidOperationException("Unexpected save prompt");
        return Task.FromResult(SaveAnswers.Dequeue());
    }

    public Task<string?> PickSavePackagePathAsync(string title, string? suggestedFileName = null)
    {
        SavePathRequests.Add(title);
        return Task.FromResult(SavePaths.Count > 0 ? SavePaths.Dequeue() : null);
    }
}
