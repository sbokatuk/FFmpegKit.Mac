# FFmpegKit.Mac

[![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Video.Mac?label=nuget)](https://www.nuget.org/packages/FFmpegKit.Net.Video.Mac)
[![release](https://github.com/sbokatuk/FFmpegKit.Mac/actions/workflows/release.yml/badge.svg)](https://github.com/sbokatuk/FFmpegKit.Mac/actions/workflows/release.yml)
[![Targets: net8.0 | net9.0 | net10.0](https://img.shields.io/badge/targets-net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4)](#packages)
[![ffmpeg 8.1.2](https://img.shields.io/badge/ffmpeg-8.1.2-632CA6)](#about)
[![Licence: MIT AND LGPL-3.0 or GPL-3.0](https://img.shields.io/badge/licence-MIT%20AND%20LGPL--3.0%20or%20GPL--3.0-orange)](#licence)

.NET for macOS bindings for the native **FFmpegKit** library.

> GitHub reports this repository as MIT because that is what it contains: binding source only, no native binaries. **The published packages are not MIT** — they embed native FFmpeg builds and are additionally covered by LGPL-3.0, or GPL-3.0 for the `-gpl` variants. See [License](#license).

Built against the prebuilt Apple binaries from **[sk3llo/ffmpeg_kit_flutter](https://github.com/sk3llo/ffmpeg_kit_flutter)** — see [Where the native binaries come from](#where-the-native-binaries-come-from) for why that fork and not the original.

## About

This repository contains .NET bindings on top of the FFmpegKit `.xcframework` build, keeping its native **macOS** slice. One project, `src/FFmpegKit.Mac`, produces all eight package variants — the variant is selected with the `FFmpegKitBuildType` MSBuild property.

Packages target `net8.0-macos14.0`, `net9.0-macos15.0` and `net10.0-macos26.0`.

> **These packages supersede the `6.0.0.1-beta1` prereleases published under the same ids.** Those were **Mac Catalyst** bindings (`net*-maccatalyst` target frameworks) generated from arthenica's FFmpeg 6.0 xcframeworks. No live source publishes Catalyst slices any more — arthenica is archived with its release assets deleted — so from `8.1.2.1` on, `FFmpegKit.Net.<Variant>.Mac` means **native macOS** (`net*-macos`, AppKit apps). If you need Catalyst, the old betas remain on nuget.org, frozen at FFmpeg 6.0.

Each .NET SDK's macOS workload supports only two target frameworks — the .NET 9 band ships `net8` and `net9`, the .NET 10 band ships `net9` and `net10` — so no single `dotnet pack` can produce all three. [`BuildNugets.sh`](build/BuildNugets.sh) packs once per band and [`build/merge-packages.py`](build/merge-packages.py) merges the `lib/` trees and nuspec dependency groups into one package per variant.

### Where the native binaries come from

FFmpegKit has several relevant repositories, and only one of them still ships usable **Apple** binaries:

| Repository | State | Prebuilt `.xcframework` |
| --- | --- | --- |
| [`arthenica/ffmpeg-kit`](https://github.com/arthenica/ffmpeg-kit) | archived | none — every release now carries **zero assets** |
| [`arthenica/ffmpeg-kit-next`](https://github.com/arthenica/ffmpeg-kit-next) | active, the official continuation | none — source only |
| [`ffmpegkit-maintained/ffmpeg`](https://github.com/ffmpegkit-maintained/ffmpeg) | active community fork | none — **Android `.aar` only**, nothing for Apple platforms |
| [`ffmpegkit-maintained/ffmpeg-kit-ios-full`](https://github.com/ffmpegkit-maintained/ffmpeg-kit-ios-full) | stale | yes, but FFmpeg **6.0**, `full-gpl` only, no releases or tags to pin |
| [`sk3llo/ffmpeg_kit_flutter`](https://github.com/sk3llo/ffmpeg_kit_flutter) | active | **yes** — all eight variants, currently `8.1.2` |

Note that this is *not* the same source as the Android bindings use: [`FFmpegKit.Android`](https://github.com/sbokatuk/FFmpegKit.Android) takes its `.aar` files from `ffmpegkit-maintained/ffmpeg` via Maven Central, and that fork does not build for Apple platforms.

Releases there are tagged `<version>-<variant>` and carry each xcframework as a separate zip plus a `checksums.json`. [`FetchXcFrameworks.sh`](build/FetchXcFrameworks.sh) downloads all eight and verifies every one against that manifest — these are tens of megabytes of native code that gets linked into your app, so a truncated or substituted archive fails the build rather than shipping.

The version is set by `FFmpegKitNativeVersion` in [`Directory.Build.props`](Directory.Build.props), which `FetchXcFrameworks.sh` reads, so the download and the frameworks the project expects cannot drift apart.

The fork currently publishes four FFmpeg lines, each with all eight variants:

| FFmpeg |
| --- |
| `7.1.1` |
| `8.0.0` |
| `8.1.1` |
| `8.1.2` |

Each xcframework carries an iOS device slice, an iOS simulator slice and a **universal macOS slice** (`macos-arm64_x86_64` — Apple silicon and Intel in one binary). This repository keeps only the macOS slice; the two iOS ones are stripped on download since they cannot be reached from a `net*-macos` binding and would triple the package size. The exact slice names are upstream's to decide and have changed within a version, so the package tests assert the *shape* — exactly one slice, macOS, nothing else — rather than specific names. ([`FFmpegKit.iOS`](https://github.com/sbokatuk/FFmpegKit.iOS) does the mirror image with the same downloads.)

Note that **no Mac Catalyst slice is published by any live source** — see the note under [About](#about) about the old Catalyst-based prereleases.

### Versioning

Package versions are **`<ffmpeg version>.<binding revision>`**:

```
8.1.2.1
└───┬─┘ └┬┘
    │    └─ binding revision — this repository
    └────── FFmpeg version — the native build inside the package
```

The first three components name the FFmpeg build the package contains, which is also the version [`FetchXcFrameworks.sh`](build/FetchXcFrameworks.sh) downloads and what upstream tags its releases with. The fourth belongs to this repository and increments whenever the bindings or packaging change while the native binaries stay put — `8.1.2.1` and `8.1.2.2` are the same FFmpeg with different bindings.

A floating range such as `8.1.2.*` therefore always resolves to the newest bindings for that exact FFmpeg build and never crosses onto another one. Pin an exact version instead if you would rather approve every binding update yourself.

> The [iOS](https://github.com/sbokatuk/FFmpegKit.iOS) and [Android](https://github.com/sbokatuk/FFmpegKit.Android) bindings use the same scheme, and currently track the same FFmpeg line — `8.1.2` everywhere. The **binding revisions advance independently**, so the fourth component differs between repositories.

### Releasing an older line

The tag selects the FFmpeg line: the first three components of **`v7.1.1.1`** are the FFmpeg version to build against, so that tag binds FFmpeg `7.1.1` and publishes `7.1.1.1` packages. The fourth component is the binding revision and does not affect which native build is fetched (`v8.1.2.6` → FFmpeg `8.1.2`), and a prerelease suffix is ignored too (`v8.1.2.1-beta.1` → FFmpeg `8.1.2`). No branch or `Directory.Build.props` edit is needed.

Locally, pass the native version as the second argument:

```sh
./build/FetchXcFrameworks.sh 7.1.1     # fetch that line's xcframeworks
./build/BuildNugets.sh 7.1.1 7.1.1     # package version, native version
```

## License

> This section describes what the upstream project states. It is not legal advice — if the distinction matters for your product, get it reviewed.

The C# binding code in this repository is [MIT](LICENSE). **The published NuGet packages are not**, because each one embeds native FFmpeg binaries that carry their own copyleft terms. Each package therefore declares `MIT AND <native license>`:

| Package | Native license | SPDX expression |
| --- | --- | --- |
| `FFmpegKit.Net.Audio.Mac` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Full.Mac` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Https.Mac` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Min.Mac` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Video.Mac` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.FullGpl.Mac` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `FFmpegKit.Net.HttpsGpl.Mac` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `FFmpegKit.Net.MinGpl.Mac` | **GPL-3.0** | `MIT AND GPL-3.0-only` |

The `-gpl` variants enable `x264`, `x265`, `xvid` and `vidstab`, which are GPL — upstream keeps them as separate artifacts specifically so they never contaminate the LGPL ones. Upstream's guidance is direct: **if your app is closed-source, use a non-GPL variant.**

Upstream states version 3.0 with no "or later" wording, hence the `-only` SPDX identifiers.

Every package ships the texts it is covered by under `licenses/` — `LICENSE` (MIT, the bindings) and `LGPL-3.0.txt` or `GPL-3.0.txt` (the native binaries). The same texts are in this repository under [`licenses/`](licenses).

The package tests assert this rather than trusting it: every variant is checked for the presence or absence of x264 in its `libavcodec`, so a GPL build packed under an LGPL licence expression fails the build.

## Installation

Install the package via NuGet. There are various packages depending on what you plan to use and if you require a GPL compatible package or not. These package variants match the different variants built in the FFmpegKit repository. The `-gpl` variants are GPL-3.0 — see [License](#license) before choosing one.

| Package | Link |
|------------|-----|
| FFmpegKit.Net.Audio.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Audio.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Audio.Mac) |
| FFmpegKit.Net.Full.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Full.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Full.Mac) |
| FFmpegKit.Net.FullGpl.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.FullGpl.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.FullGpl.Mac) |
| FFmpegKit.Net.Https.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Https.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Https.Mac) |
| FFmpegKit.Net.HttpsGpl.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.HttpsGpl.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.HttpsGpl.Mac) |
| FFmpegKit.Net.Min.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Min.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Min.Mac) |
| FFmpegKit.Net.MinGpl.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.MinGpl.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.MinGpl.Mac) |
| FFmpegKit.Net.Video.Mac | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Video.Mac.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Video.Mac) |

A package version is its FFmpeg version plus a binding revision — see [Versioning](#versioning). `8.1.2.*` floats to the newest bindings for FFmpeg `8.1.2` without ever crossing onto another FFmpeg build.

### Migrating from the `6.0.0.1-beta1` prereleases

The package ids are unchanged; the platform is not. The old prereleases targeted **Mac Catalyst** (`net7.0-maccatalyst16.1` / `net8.0-maccatalyst17.2`); from `8.1.2.1` these packages target **native macOS**. A Catalyst app cannot upgrade — a native macOS (AppKit or plain `net*-macos`) app is the consumer now.

```diff
-<PackageReference Include="FFmpegKit.Net.Video.Mac" Version="6.0.0.1-beta1" />
+<PackageReference Include="FFmpegKit.Net.Video.Mac" Version="8.1.2.1" />
```

The namespace is `Ffmpegkit.Mac`. It deliberately does not follow the package name: a namespace rooted at `FFmpegKit` containing a type also called `FFmpegKit` makes `FFmpegKit.Execute(...)` resolve the namespace instead of the class and fail to compile.

Also worth knowing when upgrading:

- The old prereleases declared their licence as `MIT` while shipping LGPL/GPL FFmpeg binaries. Corrected here — see [License](#license). Your obligations have not changed; the metadata was simply wrong.
- The bound API surface is much larger now: `FFmpegKitConfig`, `FFprobeKit`, `MediaInformation` and friends were missing from the old binding entirely, plus `Task`-returning execute/probe wrappers and ergonomic helpers are added.

## Usage

Include the `Ffmpegkit.Mac` namespace:

```c#
using Ffmpegkit.Mac;
```

Execute your FFmpeg command:

```c#
var session = await FFmpegKit.ExecuteAsync("-i input.mov -c:v libx264 output.mp4");

if (session.Succeeded())
    Console.WriteLine("done");
```

`ExecuteAsync` wraps FFmpegKit's own asynchronous path, so nothing blocks the calling thread. Pass a `CancellationToken` to stop a running command — the session then completes with a cancelled return code rather than throwing. A synchronous `FFmpegKit.Execute` is also bound, but it blocks for the whole transcode, which on the UI thread means a frozen app.

Probing works the same way:

```c#
var probe = await FFprobeKit.GetMediaInformationAsync(path);
Console.WriteLine(probe.MediaInformation?.Format);
```

`Registrar`: the packages ship a small `.targets` that defaults consuming apps to the `partial-static` registrar. The .NET macOS SDK's Release default (`managed-static`) crashes at runtime on NuGet-delivered bindings with a missing `ObjCRuntime.__Registrar__` type; `partial-static` is fully supported and handles the binding correctly. Set `<Registrar>` in your app project yourself to override.

Global log and statistics hooks take lambdas, and are cleared by name:

```c#
FFmpegKitConfig.EnableLogCallback(log => Console.WriteLine(log.Message));
FFmpegKitConfig.DisableLogCallback();          // likewise DisableStatisticsCallback()
```

Both callbacks arrive on an FFmpegKit worker thread — marshal before touching UI — and are
held by FFmpegKit until cleared, so anything they capture stays alive too.

More examples and usage can be found in the [original FFmpegKit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/MacOS). That repository is archived, but the Objective-C API it documents is the one these bindings expose, so it remains the reference.

### Signing, hardened runtime and notarization

The packages ship unsigned FFmpegKit `.framework`s. The .NET macOS SDK embeds them under
`Contents/Frameworks` and signs each nested framework with the app's own identity as part of
signing the app, so there is nothing FFmpegKit-specific to configure:

- **Local and ad-hoc builds** run as-is — the default ad-hoc signature covers the embedded
  frameworks.
- **Hardened runtime + library validation** (what notarization requires): frameworks signed as
  part of the app bundle share its Team ID and pass library validation. Do not re-sign them
  with a different identity afterwards, and `com.apple.security.cs.disable-library-validation`
  is not needed for these frameworks.
- **Custom signing scripts**: sign inside-out — nested frameworks first, the app bundle last.
  Re-signing the outer bundle alone invalidates the nested seals and the app dies at load with
  a code-signature error.
- **Sandboxed / App Store builds**: FFmpeg reads and writes ordinary file paths, so the usual
  user-selected-file entitlements govern what commands can touch; the `Https*` variants only
  need `com.apple.security.network.client` if your commands actually fetch remote inputs.

## Building

### Prerequisites

macOS with Xcode, and the .NET 9 and 10 SDKs each with the macOS workload installed — every band supplies a different reference pack. The SDK is chosen by the `global.json` in the *working directory*, so install each band from a directory pinned to it:

```sh
for major in 9 10; do
  dir=$(mktemp -d) && cd "$dir"
  dotnet new globaljson --sdk-version "$(dotnet --list-sdks | grep "^${major}\." | tail -1 | cut -d' ' -f1)" --force
  dotnet workload install macos
done
```

Python 3 is also needed, for the xcframework slice stripping and the package merge step.

### All variants

```sh
./build/FetchXcFrameworks.sh          # downloads the xcframeworks (~1 GB for all eight)
./build/BuildNugets.sh                # packs all 8 variants into ./artifacts
./build/BuildNugets.sh 8.1.2-rc.1     # ...or with an explicit package version
```

`FetchXcFrameworks.sh` reads the FFmpegKit version from `FFmpegKitNativeVersion` in `Directory.Build.props`, the same property the `.csproj` uses to locate the frameworks, so the two cannot drift apart. Pass a version to override it, and a variant to fetch just one:

```sh
./build/FetchXcFrameworks.sh 8.1.2 Video
```

### A single variant

```sh
# net8 + net9 assets (.NET 9 SDK, per global.json)
dotnet pack src/FFmpegKit.Mac/FFmpegKit.Mac.csproj \
    -c Release -p:FFmpegKitBuildType=Video -p:FFmpegKitSdkBand=net9 -o artifacts
```

`FFmpegKitBuildType` is one of `Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl`, `Video`. `FFmpegKitSdkBand` is `net9` or `net10` and must match the SDK actually running the build. Each variant builds into its own `obj/` and `bin/` subdirectory, so they can be built in sequence without interfering with each other.

### Regenerating the binding

Only needed when bumping to a newer native FFmpegKit version. The binding is generated with [Objective Sharpie](https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/macios/binding/objective-sharpie/) from the vendored frameworks' public headers:

```sh
# Stage the public headers from the macOS slice
mkdir -p Headers
cp -R src/FFmpegKit.Mac/libs/Video/ffmpegkit.xcframework/macos-arm64_x86_64/ffmpegkit.framework/Versions/A/Headers/* Headers/

# Sharpie must be pointed at an umbrella header. FFmpegKit.h alone only pulls in a fraction of
# the API - binding just that is how the previous binding ended up missing FFmpegKitConfig,
# FFprobeKit and the MediaInformation types.
ls Headers/*.h | grep -v fftools | grep -v ffmpegkit_exception \
  | sed 's|Headers/|#import "|; s|$|"|' > Headers/FFmpegKitUmbrella.h

sharpie bind -output Binding -sdk macosx26.5 -scope Headers Headers/FFmpegKitUmbrella.h -c -I Headers
```

Then reconcile `Binding/ApiDefinitions.cs` and `Binding/StructsAndEnums.cs` into `src/FFmpegKit.Mac/ApiDefinition.cs` and `src/FFmpegKit.Mac/Structs.cs`. Every `[Verify]` attribute sharpie emits must be reviewed and removed — they intentionally cause build failures. Note that sharpie emits the `Level` enum as `ulong` despite its negative members; it has to be `long`.

## Sample

[`samples/FFmpegKit.Mac.Example`](samples/FFmpegKit.Mac.Example) is a native AppKit app - the
counterpart of the MAUI samples in the iOS and Android repositories, which cannot cover this
platform (MAUI's "Mac" is Mac Catalyst). It probes a bundled clip, runs resize / grayscale /
audio-extract conversions with a live progress bar driven by the statistics callback, supports
cancelling mid-run, and previews source and result side by side with `AVPlayerView`.

It references the packed `FFmpegKit.Net.Full.Mac` (LGPL, deliberately not a `-gpl` variant) from
`./artifacts` through the local feed in `NuGet.config` - build the packages first:

```sh
./build/FetchXcFrameworks.sh 8.1.2 Full
dotnet pack src/FFmpegKit.Mac/FFmpegKit.Mac.csproj -c Release -p:FFmpegKitBuildType=Full -o artifacts
dotnet build samples/FFmpegKit.Mac.Example/FFmpegKit.Mac.Example.csproj
```

Like the tests, it is deliberately not in `FFmpegKit.sln`, since a fresh clone has no artifacts
to restore it against. CI compile-checks it against the freshly packed package on every build.

## Tests

**Package tests** run anywhere and inspect the packed `.nupkg` files — assembly present for every target framework, all eight xcframeworks with exactly one macOS slice each, manifests consistent with the slices actually shipped, the GPL/LGPL split matching what the binaries contain, the registrar workaround `.targets` present, and nuspec metadata:

```sh
./build/BuildNugets.sh                       # produce ./artifacts first
dotnet test tests/FFmpegKit.Mac.PackageTests
FFMPEGKIT_VARIANTS=Video dotnet test tests/FFmpegKit.Mac.PackageTests   # ...or just one variant
```

**Host smoke tests** build an app against the packed package and run real FFmpeg commands directly on this Mac — macOS is the target platform, so no simulator or device is involved:

```sh
./.github/scripts/run-host-tests.sh Video 8.1.2.1 net9.0-macos15.0
```

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Builds and packs all 8 variants as `<version>-beta.<pr>.<run>`, runs package tests and the host smoke tests (net8 and net10 legs in parallel), then publishes the betas to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | Same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release with the changelog since the previous tag and links to every package. |

Both call the reusable [`build.yml`](.github/workflows/build.yml), which runs on macOS — which is also where the smoke tests execute, since the runner itself is the target platform.

Note that prereleases pushed to nuget.org cannot be deleted, only unlisted — every pull request push publishes eight packages.

### Package size

The single universal macOS slice keeps packages well clear of nuget.org's **250 MB** limit (`Video` carries ~20 MB of payload per target framework). If a future FFmpegKit release grows the binaries enough to threaten that line, the options are to drop the (end-of-life) `net8.0-macos` target, or to thin the universal slice to `arm64` only — which costs support for Intel Macs.
