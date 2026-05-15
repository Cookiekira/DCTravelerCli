# Winget manifest submission

This directory mirrors the `microsoft/winget-pkgs` manifest layout for `Cookiekira.DCTravelerCli`.

To submit a stable release:

1. Copy `manifests/c/Cookiekira/DCTravelerCli/<version>/` into a fork of `microsoft/winget-pkgs` at the same path.
2. Run `winget validate manifests/c/Cookiekira/DCTravelerCli/<version>/Cookiekira.DCTravelerCli.yaml`.
3. Test the local manifest with `winget install --manifest manifests/c/Cookiekira/DCTravelerCli/<version>/`.
4. Open a pull request to `microsoft/winget-pkgs`.

Only stable GitHub Release assets should be submitted. Do not submit nightly or canary builds.

The winget manifest uses the Windows x64 AOT zip because it is the preferred portable package for Windows Package Manager. Keep using the AOT asset unless a later release deliberately changes that distribution decision.
