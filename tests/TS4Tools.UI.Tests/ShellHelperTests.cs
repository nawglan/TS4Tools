using FluentAssertions;
using TS4Tools.UI.Services;
using Xunit;

namespace TS4Tools.UI.Tests;

/// <summary>
/// Tests for helper programs: .helper parsing, matching, command line expansion with resource
/// properties, and the clipboard exchange used when the command line has no {}.
/// </summary>
/// <remarks>
/// Legacy behaviour under test: legacy_references/Sims4Tools/s4pi Extras/Helpers/Helpers.cs lines 20-205
/// (ReadConfig, HelperManager, execHelper) and 252-279 (Execute), and
/// legacy_references/Sims4Tools/s4pe/Helpers/Helpers.txt (file format).
/// </remarks>
public sealed class ShellHelperTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ts4tools-helpers-" + Guid.NewGuid().ToString("N"));

    public ShellHelperTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private HelperManager LoadManager(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_folder, fileName), content);
        return new HelperManager(_folder);
    }

    private sealed class FakeHelperClipboard(byte[]? result) : IHelperClipboard
    {
        public byte[]? Set { get; private set; }

        public Task SetResourceAsync(byte[] data)
        {
            Set = data;
            return Task.CompletedTask;
        }

        public Task<byte[]?> TryGetResourceAsync() => Task.FromResult(result);
    }

    private sealed class SampleResource
    {
        public string Title { get; set; } = "My Title";

        public int Count { get; set; } = 7;

        public bool Enabled { get; set; } = true;

        public byte[] Data { get; set; } = [1, 2, 3];

        public int this[int index] => index;

        public List<string> Items { get; } = ["a"];
    }

    private static HelperInstance Instance(string command, string arguments, bool readOnly = false, bool ignoreTs = false)
        => new(
            new HelperDefinition
            {
                Id = "test",
                Command = command,
                Arguments = arguments,
                IsReadOnly = readOnly,
                IgnoreWriteTimestamp = ignoreTs
            },
            0x220557DA,
            0,
            1);

    [Fact]
    public void Parse_ReadsKeywordsAndIgnoresComments()
    {
        var manager = LoadManager("sample.helper", """
            # a comment
            ResourceType: 0x220557DA 0x0166038C ; trailing comment
            /* block
               comment */
            Label: Sample
            Desc: A description // note
            Command: notepad
            Arguments: "{}" "-title:{title}"
            """);

        var helper = manager.AllHelpers.Should().ContainSingle().Subject;
        helper.Id.Should().Be("sample");
        helper.Label.Should().Be("Sample");
        helper.Description.Should().Be("A description");
        helper.Command.Should().Be("notepad");
        helper.Arguments.Should().Be("\"{}\" \"-title:{title}\"");
        helper.ResourceTypes.Should().Equal("0x220557DA", "0x0166038C");
    }

    [Fact]
    public void Parse_ReadOnlyAndIgnoreTimestamp_NeedAValue()
    {
        var manager = LoadManager("flags.helper", "Wrapper: *\nCommand: x\nReadOnly:\nIgnoreWriteTimestamp: whatever\n");

        var helper = manager.AllHelpers.Single();
        helper.IsReadOnly.Should().BeFalse("legacy treats an empty value as not set");
        helper.IgnoreWriteTimestamp.Should().BeTrue();

        File.WriteAllText(Path.Combine(_folder, "flags.helper"), "Wrapper: *\nCommand: x\nReadOnly: yes\n");
        manager.Reload();
        manager.AllHelpers.Single().IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Parse_FileWithoutCommand_IsIgnored()
    {
        var manager = LoadManager("nocommand.helper", "ResourceType: 0x1\nLabel: x\n");

        manager.AllHelpers.Should().BeEmpty();
    }

    [Fact]
    public void Matching_ByTypeWrapperAndAny()
    {
        File.WriteAllText(Path.Combine(_folder, "type.helper"), "ResourceType: 0x220557DA\nCommand: a\n");
        File.WriteAllText(Path.Combine(_folder, "wrapper.helper"), "Wrapper: StblResource\nCommand: b\n");
        File.WriteAllText(Path.Combine(_folder, "any.helper"), "ResourceType: *\nCommand: c\n");
        File.WriteAllText(Path.Combine(_folder, "other.helper"), "ResourceType: 0x00000001\nCommand: d\n");
        var manager = new HelperManager(_folder);

        manager.GetHelpersForResource(0x220557DA, 0, 1, "StblResource").Select(h => h.Id)
            .Should().BeEquivalentTo("type", "wrapper", "any");
        manager.GetHelpersForResource(0x220557DA, 0, 1, "OtherResource").Select(h => h.Id)
            .Should().BeEquivalentTo("type", "any");
    }

    [Fact]
    public void Expand_ReplacesBraceBraceInCommandAndArguments()
    {
        var (command, args) = Instance("{}", "\"{}\" x").ExpandCommandLine("/tmp/file.bin");

        command.Should().Be("/tmp/file.bin");
        args.Should().Be("\"/tmp/file.bin\" x");
    }

    [Fact]
    public void Expand_ReplacesResourcePropertiesInArgumentsOnly()
    {
        var properties = HelperProperties.From(new SampleResource());
        var helper = Instance("run-{title}", "\"-load:{}\" \"-title:{title}\" -n {count} {enabled} {missing}");

        var (command, args) = helper.ExpandCommandLine("/tmp/f", properties);

        command.Should().Be("run-{title}", "properties are only expanded in the arguments");
        args.Should().Be("\"-load:/tmp/f\" \"-title:My Title\" -n 7 True {missing}");
    }

    [Fact]
    public void ExpandArgumentList_HostilePropertyValue_CannotInjectArguments()
    {
        // Resource field values come from untrusted package files.
        var properties = new Dictionary<string, string>
        {
            ["title"] = "x\" --evil \"y\n--also-evil",
        };
        var helper = Instance("tool", "\"-load:{}\" -t {title} end");

        var (command, args) = helper.ExpandArgumentList("/tmp/a b.bin", properties);

        command.Should().Be("tool");
        args.Should().Equal("-load:/tmp/a b.bin", "-t", "x\" --evil \"y--also-evil", "end");
    }

    [Fact]
    public void TokenizeArguments_HonoursQuotesAndEscapedQuotes()
    {
        HelperInstance.TokenizeArguments("a \"b c\" d\\\"e  \"\"").Should().Equal("a", "b c", "d\"e", "");
    }

    [Fact]
    public void Expand_WithoutFile_LeavesBraceBraceAlone_AndStillExpandsProperties()
    {
        var helper = Instance("tool", "-t {title}");

        var (_, args) = helper.ExpandCommandLine(null, HelperProperties.From(new SampleResource()));

        args.Should().Be("-t My Title");
    }

    [Fact]
    public void Properties_AreTheSimplePublicFieldsByLowerCaseName()
    {
        var properties = HelperProperties.From(new SampleResource());

        properties.Keys.Should().BeEquivalentTo("title", "count", "enabled");
        properties["Title"].Should().Be("My Title", "lookup is case-insensitive");
        HelperProperties.From(null).Should().BeEmpty();
    }

    [Fact]
    public void UsesFileExport_DependsOnBraceBrace()
    {
        Instance("a", "{}").UsesFileExport.Should().BeTrue();
        Instance("{}", "").UsesFileExport.Should().BeTrue();
        Instance("a", "-x").UsesFileExport.Should().BeFalse();
        Instance("a", "-x", ignoreTs: true).IgnoreWriteTimestamp.Should().BeFalse("only file helpers have a timestamp to ignore");
        Instance("a", "{}", ignoreTs: true).IgnoreWriteTimestamp.Should().BeTrue();
    }

    [Fact]
    public async Task ClipboardHelper_GetsTheResource_AndReturnsWhatItLeavesOnTheClipboard()
    {
        if (OperatingSystem.IsWindows()) return;
        var edited = new byte[] { 9, 9 };
        var clipboard = new FakeHelperClipboard(edited);

        var result = await Instance("sh", "-c \"exit 0\"").ExecuteAsync(new byte[] { 1, 2, 3 }, clipboard: clipboard);

        result.Success.Should().BeTrue();
        clipboard.Set.Should().Equal(1, 2, 3);
        result.ModifiedData.Should().Equal(edited);
    }

    [Fact]
    public async Task ClipboardHelper_LeavingNothingOnTheClipboard_ChangesNothing()
    {
        if (OperatingSystem.IsWindows()) return;

        var result = await Instance("sh", "-c \"exit 0\"").ExecuteAsync(new byte[] { 1 }, clipboard: new FakeHelperClipboard(null));

        result.Success.Should().BeTrue();
        result.ModifiedData.Should().BeNull();
    }

    [Fact]
    public async Task ClipboardHelper_ReadOnly_NeverReadsTheClipboardBack()
    {
        if (OperatingSystem.IsWindows()) return;

        var result = await Instance("sh", "-c \"exit 0\"", readOnly: true)
            .ExecuteAsync(new byte[] { 1 }, clipboard: new FakeHelperClipboard(new byte[] { 5 }));

        result.Success.Should().BeTrue();
        result.ModifiedData.Should().BeNull();
    }

    [Fact]
    public async Task ClipboardHelper_NonZeroExit_Fails()
    {
        if (OperatingSystem.IsWindows()) return;

        var result = await Instance("sh", "-c \"exit 3\"").ExecuteAsync(new byte[] { 1 }, clipboard: new FakeHelperClipboard(new byte[] { 5 }));

        result.Success.Should().BeFalse();
        result.ModifiedData.Should().BeNull();
    }

    [Fact]
    public async Task ClipboardHelper_WithoutClipboard_Fails()
    {
        var result = await Instance("sh", "-c \"exit 0\"").ExecuteAsync(new byte[] { 1 });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("clipboard");
    }

    [Fact]
    public async Task FileHelper_ExpandsPropertiesAndDetectsChanges()
    {
        if (OperatingSystem.IsWindows()) return;

        // The helper appends a line to the file it is given and gets the resource's title as an argument.
        var helper = Instance("sh", "-c \"echo {title} >> \\\"$0\\\"\" \"{}\"");

        var result = await helper.ExecuteAsync(
            new byte[] { (byte)'x', (byte)'\n' },
            "name",
            new Dictionary<string, string> { ["title"] = "hello" });

        result.Success.Should().BeTrue(result.ErrorMessage);
        System.Text.Encoding.UTF8.GetString(result.ModifiedData!).Should().Be("x\nhello\n");
    }

    [Fact]
    public async Task FileHelper_ReadOnly_ReportsNoChanges()
    {
        if (OperatingSystem.IsWindows()) return;

        var result = await Instance("sh", "-c \"echo y >> \\\"$0\\\"\" \"{}\"", readOnly: true).ExecuteAsync(new byte[] { 1 }, "ro");

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.ModifiedData.Should().BeNull();
    }

    [Fact]
    public async Task HelperMenuItem_ExecuteCommand_IsCachedAndRunsTheHelper()
    {
        var helper = new HelperInstance(
            new HelperDefinition { Id = "h", Label = "Helper", Command = "true" }, 0x220557DA, 0, 1);
        var runs = 0;
        HelperInstance? ran = null;
        var item = new TS4Tools.UI.ViewModels.HelperMenuItemViewModel(helper, h =>
        {
            runs++;
            ran = h;
            return Task.CompletedTask;
        });

        item.ExecuteCommand.Should().BeSameAs(item.ExecuteCommand);

        await item.ExecuteCommand.ExecuteAsync(null);

        runs.Should().Be(1);
        ran.Should().BeSameAs(helper);
    }
}
