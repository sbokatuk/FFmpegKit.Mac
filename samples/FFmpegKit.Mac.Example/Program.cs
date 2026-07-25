using System.Globalization;
using AppKit;
using AVFoundation;
using AVKit;
using CoreGraphics;
using Ffmpegkit.Mac;
using Foundation;
// The app's own root namespace starts with 'FFmpegKit', which would otherwise shadow the bound
// type - the same collision the iOS and Android samples document.
using FFmpeg = Ffmpegkit.Mac.FFmpegKit;

namespace FFmpegKit.Mac.Example;

/// <summary>
/// The macOS counterpart of the MAUI samples in the iOS and Android repositories: probe a bundled
/// clip, run one of three conversions with a live progress bar driven by the statistics callback,
/// support cancelling mid-run, and preview source and result side by side.
/// </summary>
public static class Program
{
    private static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new AppDelegate();
        NSApplication.SharedApplication.Run();
    }
}

public sealed class AppDelegate : NSApplicationDelegate
{
    private NSWindow _window = null!;
    private AVPlayerView _source = null!;
    private AVPlayerView _result = null!;
    private NSProgressIndicator _progress = null!;
    private NSTextField _status = null!;
    private NSButton _cancel = null!;
    private NSButton[] _actions = null!;

    private string _inputPath = null!;
    private double _durationSeconds;

    public override void DidFinishLaunching(NSNotification notification)
    {
        _inputPath = NSBundle.MainBundle.PathForResource("sample", "mp4")
            ?? throw new InvalidOperationException("sample.mp4 is missing from the bundle.");

        BuildWindow();

        _source.Player = AVPlayer.FromUrl(NSUrl.FromFilename(_inputPath));

        // Statistics arrive on an FFmpegKit thread for whichever session is running; the probe
        // below supplies the total duration that turns them into a percentage.
        FFmpegKitConfig.EnableStatisticsCallback(statistics =>
        {
            if (_durationSeconds <= 0)
                return;

            var fraction = Math.Clamp(statistics.Time / 1000.0 / _durationSeconds, 0, 1);
            InvokeOnMainThread(() =>
            {
                _progress.DoubleValue = fraction * 100;
                _status.StringValue = $"{fraction:P0}  frame {statistics.VideoFrameNumber}  {statistics.Speed:0.##}x";
            });
        });

        _ = ProbeAsync();
    }

    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;

    private async Task ProbeAsync()
    {
        var session = await FFprobeKit.GetMediaInformationAsync(_inputPath);
        var information = session.MediaInformation;

        // FFprobe reports the duration as an invariant-format string; parsing it with the
        // ambient culture misreads it on a de-DE or fr-FR system.
        if (information?.Duration is { } duration
            && double.TryParse(duration, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            _durationSeconds = seconds;
        }

        InvokeOnMainThread(() => _status.StringValue = information is null
            ? "probe failed"
            : $"{information.Format}  {_durationSeconds:0.#}s - pick a conversion");
    }

    private async Task RunAsync(string name, string arguments, string extension)
    {
        var output = Path.Combine(Path.GetTempPath(), $"ffmpegkit-example-{name}.{extension}");
        File.Delete(output);

        SetBusy(true);
        _status.StringValue = $"running {name}…";
        _progress.DoubleValue = 0;

        try
        {
            // ExecuteAsync wraps FFmpegKit's own asynchronous path - nothing blocks the UI
            // thread, and the Cancel button stops the running session via the static Cancel().
            var session = await FFmpeg.ExecuteAsync($"-y -i \"{_inputPath}\" {arguments} \"{output}\"");

            if (session.Succeeded())
            {
                _status.StringValue = $"{name}: done ({new FileInfo(output).Length / 1024} KB)";
                _progress.DoubleValue = 100;
                _result.Player = AVPlayer.FromUrl(NSUrl.FromFilename(output));
            }
            else
            {
                // A failed command explains itself in Output; its last line is FFmpeg's own
                // error message.
                var cancelled = session.ReturnCode?.IsValueCancel == true;
                _status.StringValue = cancelled ? $"{name}: cancelled" : $"{name}: failed - {LastLine(session.Output)}";
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Cancel()
    {
        // Static cancel stops every running session - there is at most one here.
        FFmpeg.Cancel();
        _status.StringValue = "cancelling…";
    }

    private static string LastLine(string? output) =>
        output?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: > 0 } lines
            ? lines[^1]
            : "(no output captured)";

    private void SetBusy(bool busy)
    {
        foreach (var action in _actions)
            action.Enabled = !busy;
        _cancel.Enabled = busy;
    }

    private void BuildWindow()
    {
        _source = new AVPlayerView { ShowsFullScreenToggleButton = false };
        _result = new AVPlayerView { ShowsFullScreenToggleButton = false };

        var players = NSStackView.FromViews([_source, _result]);
        players.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
        players.Distribution = NSStackViewDistribution.FillEqually;
        players.Spacing = 8;

        _actions =
        [
            // mpeg4, not libx264: this sample references the Full (LGPL) variant, and x264 only
            // exists in the -Gpl packages - requesting it here fails with "Unknown encoder".
            Button("Resize 320x240", () => RunAsync("resize", "-vf scale=320:240 -c:v mpeg4 -an", "mp4")),
            Button("Grayscale", () => RunAsync("grayscale", "-vf format=gray -c:v mpeg4 -an", "mp4")),
            Button("Extract audio", () => RunAsync("audio", "-vn -acodec aac", "m4a")),
        ];

        _cancel = NSButton.CreateButton("Cancel", Cancel);
        _cancel.Enabled = false;

        var buttons = NSStackView.FromViews([.. _actions, _cancel]);
        buttons.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
        buttons.Spacing = 8;

        _progress = new NSProgressIndicator
        {
            Style = NSProgressIndicatorStyle.Bar,
            Indeterminate = false,
            MinValue = 0,
            MaxValue = 100,
        };

        _status = NSTextField.CreateLabel("probing…");

        var column = NSStackView.FromViews([players, buttons, _progress, _status]);
        column.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        column.Spacing = 10;
        column.EdgeInsets = new NSEdgeInsets(12, 12, 12, 12);

        _window = new NSWindow(
            new CGRect(0, 0, 760, 420),
            NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Resizable,
            NSBackingStore.Buffered,
            deferCreation: false)
        {
            Title = "FFmpegKit.Mac Example",
            ContentView = column,
        };

        _window.Center();
        _window.MakeKeyAndOrderFront(null);

        // Activate() replaced ActivateIgnoringOtherApps(bool) in macOS 14; the sample's floor is
        // 12.0, so call whichever the running OS supports rather than eat a CA1422 deprecation.
        if (OperatingSystem.IsMacOSVersionAtLeast(14))
            NSApplication.SharedApplication.Activate();
        else
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
    }

    private NSButton Button(string title, Func<Task> action) =>
        NSButton.CreateButton(title, () => _ = action());
}
