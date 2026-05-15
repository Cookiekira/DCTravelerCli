# Winget-first player installation channel

Accepted: DC Traveler CLI will make winget the first package-manager installation path for player-facing Windows releases while continuing to publish self-contained binaries through GitHub Releases. NuGet .NET Tool packaging is deferred because it optimizes for developers with a .NET SDK rather than ordinary players who want a low-friction install.

## Context

DC Traveler CLI is a .NET command-line tool, so NuGet .NET Tool packaging is the closest technical equivalent to npm. However, the primary audience is FF14 China players on Windows, and requiring a .NET SDK would make the recommended installation path feel like developer tooling.

The existing release workflow already produces Windows x64 zip assets and checksums on GitHub Releases. Winget portable command aliases expect the executable to remain runnable from the alias location, so the first manifest uses the Windows x64 AOT asset because it contains a single executable. The non-AOT Windows zip remains available as a manual GitHub Releases download.

## Consequences

- Stable GitHub Releases remain the source of binary release assets.
- The first package-manager manifest targets only the stable Windows x64 AOT zip asset.
- The winget package identifier is `Cookiekira.DCTravelerCli`, with `DCTravelerCli` as the command alias.
- Nightly, canary, AOT, Scoop, Homebrew, and NuGet .NET Tool distribution are deferred.
- README should present winget as the recommended Windows install path and GitHub Releases as the manual fallback.
