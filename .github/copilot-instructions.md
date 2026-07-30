# FFmpegKit.Mac — instructions for coding agents

## Overview

- .NET for **macOS** bindings (`net*-macos`, AppKit apps) over the native FFmpegKit `.xcframework` build. **Not Mac Catalyst** — see the hard rules.
- One project, `src/FFmpegKit.Mac`, produces all eight packages `FFmpegKit.Net.<Variant>.Mac`; the variant is chosen with `-p:FFmpegKitBuildType=` (`Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl`, `Video`).
- Native binaries come from **`sk3llo/ffmpeg_kit_flutter`** releases (tag `<version>-<variant>`), not arthenica — that repository is archived with every release asset deleted. Each xcframework carries iOS device, iOS simulator and universal macOS slices; keep **only `macos-arm64_x86_64`**, strip the rest.
- Versions are `<ffmpeg version>.<binding revision>` — `FFmpegKitNativeVersion` (8.1.2) + `FFmpegKitBindingRevision` (2) in `Directory.Build.props`. Bump the revision for binding or packaging changes, the native version only when rebinding a new FFmpeg line. **Not** comparable with the Android or iOS repositories' numbers.
- Mirror image of [`sbokatuk/FFmpegKit.iOS`](https://github.com/sbokatuk/FFmpegKit.iOS) over the same downloads. Copy patterns from it only where verified here — the registrar choice differs (below).

## Build and verify

Prerequisites: macOS with Xcode, .NET 9 **and** 10 SDKs each with the `macos` workload (install each from a directory pinned by its own `global.json`), Python 3.

```sh
./build/FetchXcFrameworks.sh                 # all variants (~1 GB); add "8.1.2 Video" for one
./build/BuildNugets.sh                       # packs all 8 into ./artifacts (two passes, merged)
dotnet test tests/FFmpegKit.Mac.PackageTests
./.github/scripts/run-host-tests.sh Video 8.1.2.2 net9.0-macos15.0
```

- Single variant: `dotnet pack src/FFmpegKit.Mac/FFmpegKit.Mac.csproj -c Release -p:FFmpegKitBuildType=Video -p:FFmpegKitSdkBand=net9 -o artifacts`.
- `FFmpegKitSdkBand` is `net9` (packs `net8.0-macos14.0`+`net9.0-macos15.0`) or `net10` (packs `net10.0-macos26.0`) and **must match the SDK running the build** — `BuildNugets.sh` runs pass 2 from a scratch directory pinning 10.0.100, then merges with `build/merge-packages.py`.
- The build fails fast when `src/FFmpegKit.Mac/libs/<Variant>/` is empty; run the fetch script rather than working around the validation target.
- Sample: `dotnet build samples/FFmpegKit.Mac.Example/FFmpegKit.Mac.Example.csproj` (add `-p:SampleSdkBand=net8|net9|net10`), against `./artifacts` via the local feed in `NuGet.config`.

## Layout

- `src/FFmpegKit.Mac/` — `ApiDefinition.cs`, `Structs.cs` (generated, reconciled by hand), `Additions/` (hand-written async wrappers and `Ergonomics.cs`), `FFmpegKit.Net.Mac.targets` (registrar workaround), untracked `libs/`.
- `build/` — `FetchXcFrameworks.sh`, `BuildNugets.sh`, `merge-packages.py`, `check-upstream.sh`, `upstream.tsv`, `icon.png`.
- `tests/FFmpegKit.Mac.PackageTests` (runs anywhere), `tests/FFmpegKit.Mac.HostTests` (runs on this Mac), driven by `.github/scripts/run-host-tests.sh`.
- `samples/FFmpegKit.Mac.Example` — native AppKit exe; deliberately outside `FFmpegKit.sln`, like a real consumer it restores the packed nupkg.
- `docs/release-notes/<version>.md`, `licenses/` (`GPL-3.0.txt`, `LGPL-3.0.txt`), `artifacts/` (packages, git-ignored except `.gitkeep`).

## Conventions

- Namespace stays `Ffmpegkit.Mac`; assembly and package id stay `FFmpegKit.Net.<Variant>.Mac`.
- The csproj, scripts and workflows are heavily commented and the comments explain *why* — keep them, and extend them when you change the reasoning they describe.
- Prose (README, release notes, comments) uses British spelling — "licence" as a noun, "behaviour", "recognise".
- Match surrounding formatting: binding sources follow the generated style (tabs, space before parentheses); test and sample C# uses ordinary .NET style.
- New public helpers belong in `Additions/`, annotated with `#nullable enable` and XML docs, never in the generated files.

## CI and release flow

- Every build job runs on **macos-15**, selecting Xcode through `.github/actions/select-xcode`.
- `pr.yml` → reusable `build.yml` with `verify: true` (pack, package tests, sample matrix, host smoke on the net8 and net10 legs), then publishes `<version>-beta.<pr>.<run>` to nuget.org. Betas are permanent — they can only be unlisted.
- Release: merge `docs/release-notes/<version>.md` to `main` → `auto-release.yml` tags `v<version>` and dispatches `release.yml`, whose `guard` job proves the commit is on `main` before it publishes with `verify: false`.
- The tag chooses the FFmpeg line (`v7.1.1.1` → FFmpeg 7.1.1; prerelease suffix ignored). Locally pass the native version as the second script argument.
- `upstream-drift.yml` runs `build/check-upstream.sh` daily against `build/upstream.tsv`; add a row there rather than editing the checker.
- After releasing here, bump `FFmpegKitMacPackageVersion` in the umbrella [`sbokatuk/FFMpegKit.Net`](https://github.com/sbokatuk/FFMpegKit.Net).

## Testing

- Run `dotnet test tests/FFmpegKit.Mac.PackageTests` before every pull request; scope iterations with `FFMPEGKIT_VARIANTS=Video`. It asserts per-TFM assemblies, all eight xcframeworks with **exactly one macOS slice each** (shape, not slice names — upstream renames them), manifests matching the shipped slices, the GPL/LGPL split against the actual binaries, the registrar `.targets`, licence texts and nuspec metadata.
- Run the host tests whenever you touch `NativeReference`s, `Additions/` or `FFmpegKit.Net.Mac.targets`. They build a real app against the packed nupkg and run FFmpeg on this Mac — no simulator or device exists for this platform.

## Hard rules

- Never commit xcframeworks or anything under `src/FFmpegKit.Mac/libs/`.
- Never skip or weaken the `checksums.json` verification, the AppleDouble/`__MACOSX` cleanup or the non-macOS slice stripping in `FetchXcFrameworks.sh`.
- Never reintroduce Mac Catalyst target frameworks or claim Catalyst support: **no live source publishes Catalyst slices**, and the pre-`8.1.2.1` `6.0.0.1-beta1` Catalyst packages are frozen history. This is a recurring support question — answer it, do not "fix" it.
- Never drop the `partial-static` registrar default or its packing into both `build/` and `buildTransitive/` without HostTests proof; the SDK's Release CoreCLR default `managed-static` crashes with a missing `ObjCRuntime.__Registrar__`. The iOS repository chooses `dynamic` — do not copy that here.
- Never rename `Ffmpegkit.Mac` or root a namespace at `FFmpegKit`; `FFmpegKit.Execute(...)` would resolve the namespace and stop compiling.
- Keep the per-variant licence expressions (`MIT AND LGPL-3.0-only`, `MIT AND GPL-3.0-only` for `-Gpl`) and both packed licence texts exact.
- Never unpin the TFM platform versions, pack a band with the wrong SDK, or hand-edit merged packages — fix `build/merge-packages.py` instead.
- Never bypass the release `guard` job or publish from a commit outside `main`'s history.

## References

- [arthenica/ffmpeg-kit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/MacOS) — archived, still the reference for the Objective-C API these bindings expose.
- [sk3llo/ffmpeg_kit_flutter releases](https://github.com/sk3llo/ffmpeg_kit_flutter/releases) — the xcframeworks and their `checksums.json`.
- Siblings: [FFmpegKit.iOS](https://github.com/sbokatuk/FFmpegKit.iOS) (mirror, same downloads), [FFmpegKit.Android](https://github.com/sbokatuk/FFmpegKit.Android) (different native source), umbrella [FFMpegKit.Net](https://github.com/sbokatuk/FFMpegKit.Net).
- Consuming-app signing, hardened runtime and notarisation questions: point at the README section, do not restate it.

Trust these instructions and search the codebase only when something here is incomplete or wrong.
