# Browser-assisted session acquisition

Accepted: DC Traveler CLI uses a real Chrome login flow to acquire FF14 China session cookies, then performs official API calls through typed .NET HTTP clients after the session has been extracted. This keeps WeGame/QQ authentication, redirects, and human verification inside the browser while avoiding brittle DOM click automation and page-injected JavaScript fetches in the business workflow.

## Consequences

- CDP is an infrastructure detail for launching Chrome, observing login success, and extracting cookies.
- After cookie extraction, the CLI saves the official session locally and future travel runs probe that saved session before opening Chrome.
- On Windows, the saved session is protected with current-user DPAPI and stored under the user's local application data directory.
- After cookie extraction or session loading, travel orchestration depends on an HTTP session rather than a live browser page.
- The travel workflow must not depend on scraping or clicking login-page DOM elements.
- The official travel page is the entry point. A `--wegame` option opens Shengqu's own login frame with `goClick=wegame`; that first-party page prepares return attributes, generates the WeGame state, and performs the OAuth handoff.
- The CLI must not open a raw Tencent Rail or WeGame OAuth URL, and must not save Shengqu login attributes out-of-browser.
