---
applyTo: "src/FFmpegKit.Mac/ApiDefinition.cs, src/FFmpegKit.Mac/Structs.cs"
---

# Binding sources

These two files are Objective Sharpie output reconciled by hand. Regenerate them only when rebinding a newer native FFmpegKit version, not to fix a single member.

- Stage the umbrella header from the **macOS slice** of the fetched frameworks: copy `src/FFmpegKit.Mac/libs/Video/ffmpegkit.xcframework/macos-arm64_x86_64/ffmpegkit.framework/Versions/A/Headers/*` into `Headers/`, then generate `Headers/FFmpegKitUmbrella.h` from that listing, excluding the `fftools` and `ffmpegkit_exception` headers.
- Bind the umbrella header, never `FFmpegKit.h` alone: `sharpie bind -output Binding -sdk macosx26.5 -scope Headers Headers/FFmpegKitUmbrella.h -c -I Headers`. Binding `FFmpegKit.h` is how the previous binding lost `FFmpegKitConfig`, `FFprobeKit` and the `MediaInformation` types.
- `Headers/` and `Binding/` are scratch and git-ignored; reconcile the output into these two committed files by hand.
- Remove **every** `[Verify]` attribute sharpie emits, after reviewing what it flagged — they are deliberate build breaks, and neither file may contain one.
- `Level` must be declared `enum Level : long`. Sharpie emits `ulong` from `NS_ENUM(NSUInteger, Level)` despite the two negative members.
- Keep the generated formatting (tabs, space before the argument list). Put hand-written helpers, async wrappers and nullable annotations in `Additions/` instead of editing these files.
- Rebuild and re-test after regenerating: `dotnet pack src/FFmpegKit.Mac/FFmpegKit.Mac.csproj -c Release -p:FFmpegKitBuildType=Video -p:FFmpegKitSdkBand=net9 -o artifacts`, then `./.github/scripts/run-host-tests.sh Video <version> net9.0-macos15.0`.
