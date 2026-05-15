# Winget manifest submission

This directory mirrors the `microsoft/winget-pkgs` manifest layout for `Cookiekira.DCTravelerCli`.

To submit a stable release:

1. Copy `manifests/c/Cookiekira/DCTravelerCli/<version>/` into a fork of `microsoft/winget-pkgs` at the same path.
2. Run `winget validate manifests/c/Cookiekira/DCTravelerCli/<version>/Cookiekira.DCTravelerCli.yaml`.
3. Test the local manifest with `winget install --manifest manifests/c/Cookiekira/DCTravelerCli/<version>/`.
4. Open a pull request to `microsoft/winget-pkgs`.

Only stable GitHub Release assets should be submitted. Do not submit nightly or canary builds.

The winget manifest uses the Windows x64 AOT zip because it is the preferred portable package for Windows Package Manager. Keep using the AOT asset unless a later release deliberately changes that distribution decision.

## Automated updates

After the first manifest has been merged into `microsoft/winget-pkgs`, stable `v*` releases automatically submit WinGet update PRs from `.github/workflows/release.yml` using `wingetcreate update`.

Required repository secret:

- `WINGET_CREATE_GITHUB_TOKEN`: a GitHub classic personal access token with the `public_repo` scope.
