using FluentAssertions;
using TS4Tools.UI.Services;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for the update checker endpoints.
/// </summary>
/// <remarks>
/// Legacy behaviour: legacy_references/Sims4Tools/s4pe/Settings/UpdateChecker.cs lines 82-129
/// (the checker queries the project's GitHub releases).
/// </remarks>
public class UpdateCheckerServiceTests
{
    [Fact]
    public void UpdateCheck_TargetsTheTS4ToolsRepository()
    {
        UpdateCheckerService.GitHubApiUrl.Should()
            .Be("https://api.github.com/repos/nawglan/TS4Tools/releases/latest");
        UpdateCheckerService.DownloadPage.Should().Be("https://github.com/nawglan/TS4Tools/releases");
    }

    private sealed class StatusHandler(System.Net.HttpStatusCode status, string body = "") : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    [Fact]
    public async Task CheckForUpdates_NoReleasePublished_IsNotAnError()
    {
        // GitHub returns 404 for /releases/latest until a release exists.
        using var client = new HttpClient(new StatusHandler(System.Net.HttpStatusCode.NotFound));
        var result = await new UpdateCheckerService(client).CheckForUpdatesAsync();

        result.Success.Should().BeTrue();
        result.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdates_ServerError_IsReportedAsFailure()
    {
        using var client = new HttpClient(new StatusHandler(System.Net.HttpStatusCode.InternalServerError));
        var result = await new UpdateCheckerService(client).CheckForUpdatesAsync();

        result.Success.Should().BeFalse();
    }
}
