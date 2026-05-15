# Winget-first player installation channel

Accepted: DC Traveler CLI will make winget the first package-manager installation path for player-facing Windows releases while continuing to publish self-contained binaries through GitHub Releases. NuGet .NET Tool packaging is deferred because it optimizes for developers with a .NET SDK rather than ordinary players who want a low-friction install.

## Context

DC Traveler CLI is a .NET command-line tool, so NuGet .NET Tool packaging is the closest technical equivalent to npm. However, the primary audience is FF14 China players on Windows, and requiring a .NET SDK would make the recommended installation path feel like developer tooling.

The existing release workflow produces Windows x64 zip assets and checksums on GitHub Releases. Winget portable command aliases expect the executable to remain runnable from the alias location, so the first manifest uses the Windows x64 AOT asset because the already-published 1.0.0 AOT archive contains a single executable. Future ordinary release assets should also be single-file packages so winget can use the non-AOT Windows zip after a local install smoke test passes.

## Consequences

- Stable GitHub Releases remain the source of binary release assets.
- The first package-manager manifest targets the stable Windows x64 AOT zip asset for 1.0.0; future manifests may use the ordinary Windows x64 zip once that release asset is single-file.
- The winget package identifier is `Cookiekira.DCTravelerCli`, with `DCTravelerCli` as the command alias.
- Nightly, canary, AOT, Scoop, Homebrew, and NuGet .NET Tool distribution are deferred.
- README should present winget as the recommended Windows install path and GitHub Releases as the manual fallback.
