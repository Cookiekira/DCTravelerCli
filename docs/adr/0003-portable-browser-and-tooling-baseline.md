# Portable browser acquisition and tooling baseline

Accepted: browser-assisted Session Acquisition should be browser-neutral at the CLI boundary, support common Chromium-based browsers across Windows, macOS, and Linux, and keep the repo on a .NET 10 formatter/analyzer baseline that CI can verify.

## Context

The original implementation named the login browser surface after Chrome and only searched Windows Chrome install paths. That made macOS Chrome installs easy to miss and prevented fallback to Edge or another Chromium browser even though the login flow only needs a CDP-capable Chromium-family browser.

The repository also targets .NET 10, so the SDK and code quality setup should express a .NET 10 baseline without pinning contributors to one exact feature band.

## Consequences

- Public CLI options should use browser-neutral names such as `--browser-path` and `--default-browser-profile`.
- Legacy Chrome-specific option names may continue to work as compatibility aliases, but help text should direct users to the browser-neutral names.
- Automatic browser discovery should check common Chrome, Edge, Chromium, Brave, Vivaldi, Opera, and Arc locations or PATH names before failing.
- The dedicated profile directory should move toward a browser-neutral name while preserving existing user profile data when possible.
- CDP endpoint validation should accept supported Chromium-family product strings and reject unknown non-browser debug endpoints.
- `global.json` should define a valid .NET 10 SDK floor with feature-band roll-forward, and CI should install `10.0.x`.
- Formatting and linting should use the .NET SDK analyzer stack and `dotnet format --verify-no-changes` before build/test in CI.
