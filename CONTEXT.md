# DC Traveler CLI

DC Traveler CLI automates the FF14 China cross-region travel workflow while keeping the final order submission explicit and user-confirmed.

## Language

**Character Discovery**:
The login-scoped scan that finds characters eligible for direct travel by querying every available source region and world.
_Avoid_: Auto fetch characters, auto get roles, role crawling

**Character Selection Catalog**:
The unified character list shown at the start of a Travel Flow. It combines direct-travel characters found through Character Discovery with away-from-home characters found through Active Travel Orders.
_Avoid_: Raw role list, merged role API data

**Bounded Character Discovery**:
Character Discovery performed with a small concurrency limit so the scan is responsive without overwhelming the official service.
_Avoid_: Full fan-out scan, sequential-only scan

**Partial Discovery Failure**:
A non-blocking failure to scan one or more source worlds while other character discovery results remain usable.
_Avoid_: Fatal scan failure

**Character**:
A game character identified by official role-list or order data and selected by the user as the subject of travel.
_Avoid_: Role

**Home Region**:
The character's original data-center region.
_Avoid_: Native area, original area

**Home World**:
The character's original server/world.
_Avoid_: Native server, original server

**Current Travel Region**:
The data-center region where an away-from-home character is currently located according to an Active Travel Order.
_Avoid_: Destination area as current area

**Current Travel World**:
The server/world where an away-from-home character is currently located according to an Active Travel Order.
_Avoid_: Destination server as current server

**Source Region**:
The data-center region used as the official source for a direct travel order. For a character at home, this is also the Home Region.
_Avoid_: Current area

**Source World**:
The server/world used as the official source for a direct travel order. For a character at home, this is also the Home World.
_Avoid_: Current server, group

**Target Region**:
The destination data-center region for a travel order.
_Avoid_: Target area

**Target World**:
The destination server/world for a travel order.
_Avoid_: Target server, target group

**Available Target**:
A target region or world that the official API does not mark as unavailable for the selected character.
_Avoid_: Disabled target

**Travel Order**:
The official cross-region travel request submitted for one selected character.
_Avoid_: Teleport order, migration order

**Active Travel Order**:
An official outgoing travel order that shows a character is still away from home and provides both the Home World and Current Travel World needed for a seamless return.
_Avoid_: Existing order, old order

**Return Home**:
The official travel-back request that returns an away-from-home character from the Current Travel World to the Home World.
_Avoid_: Go back, reset travel

**Return-Then-Travel**:
A seamless Travel Flow path for an away-from-home character: Return Home first, then submit the new Travel Order from the Home World to the selected target.
_Avoid_: Manual return first, chained teleport

**Order Confirmation**:
A single explicit user approval before submitting a travel order to the official service.
_Avoid_: Strong text challenge, repeated confirmation

**Order Tracking**:
The post-submission polling loop that watches official migration status until completion, failure, or required user confirmation.
_Avoid_: Status spam

**Offline Coverage**:
Automated tests for travel decisions and API parsing that do not require Chrome, a live login session, or the official service.
_Avoid_: Browser automation tests, live service tests

**Travel Flow**:
The one-command user journey that reuses an existing saved login session or prompts for login before continuing to character and target selection.
_Avoid_: Separate login workflow, two-step travel

**Advanced Option**:
A command-line setting intended for troubleshooting, environment differences, or power users rather than the normal travel flow.
_Avoid_: Primary option

**Session Refresh**:
An advanced maintenance action that completes login without submitting a travel order.
_Avoid_: Main login step

**Session Acquisition**:
The browser-assisted login step that obtains official FF14 session cookies from a real Chrome authentication flow.
_Avoid_: Raw cookie login, custom WeGame login, DOM click automation

**WeGame Login Shortcut**:
A user-selected login path that starts from the official travel page, then opens Shengqu's first-party login frame with `goClick=wegame` so the official page performs its own WeGame handoff.
_Avoid_: Direct old Rail OAuth entry, out-of-browser login attribute saves, scraping or clicking the Shengqu login page

**Travel Orchestration**:
The application flow that ensures login, discovers characters, collects target selection, submits the travel order, and tracks completion.
_Avoid_: Script body, command handler logic

**Official Response**:
A typed representation of an official FF14 API response at the boundary of the application.
_Avoid_: Dynamic API blob

**Official Session**:
The authenticated HTTP session derived from browser-acquired or locally saved cookies and used for official API calls.
_Avoid_: Live browser API context

## Relationships

- A **Travel Flow** requires a saved or newly completed login session
- **Session Refresh** updates the saved login session used by later **Travel Flows**
- **Session Acquisition** produces an **Official Session**
- **Official Session** provides the cookies used by official API calls during **Character Discovery** and **Travel Orders**
- **WeGame Login Shortcut** starts from the official travel page, opens the Shengqu auto-handoff login frame, and lets that first-party page prepare login attributes before WeGame
- **Travel Orchestration** coordinates one **Official Session**, one **Character Discovery** result, and at most one submitted **Travel Order**
- **Travel Orchestration** presents a **Character Selection Catalog** before target selection
- **Official Responses** are translated into application records before they reach **Travel Orchestration**
- **Character Discovery** returns zero or more **Characters**
- **Bounded Character Discovery** is the default implementation strategy for **Character Discovery**
- **Partial Discovery Failure** may accompany **Character Discovery** results
- A **Character Selection Catalog** may include **Characters** from **Character Discovery** and **Active Travel Orders**
- A **Character** has exactly one **Home Region** and one **Home World**
- A **Character** found through **Character Discovery** has a **Source Region** and **Source World** for direct travel
- A **Character** found through an **Active Travel Order** has a **Current Travel Region** and **Current Travel World**
- A **Travel Order** is submitted for exactly one **Character**
- A **Travel Order** has exactly one **Available Target** composed of a **Target Region** and **Target World**
- **Return-Then-Travel** requires one **Return Home** before the new **Travel Order**
- **Return Home** uses an **Active Travel Order** and the character's **Current Travel World**
- A **Travel Order** requires exactly one **Order Confirmation** before submission
- **Order Tracking** follows exactly one submitted **Travel Order**
- **Offline Coverage** protects **Character Discovery**, target filtering, status mapping, and official response parsing

## Example dialogue

> **Dev:** "Should the user choose a source region first?"
> **Domain expert:** "No. Run **Character Discovery** first, then let the user choose the **Character** directly."

## Flagged ambiguities

- "自动获取角色" is resolved as **Character Discovery**: scan all available source regions and worlds after login, then present a searchable character list before target selection.
- The CLI should expose **Travel Flow** as the primary one-command journey: reuse a saved login session when present; otherwise prompt for login and continue.
- A separate `login` command is allowed as **Session Refresh**, but it is secondary to the one-command **Travel Flow** and should overwrite the saved session.
- The default command surface should stay minimal; browser, port, timeout, profile, and concurrency settings are **Advanced Options**.
- Login should use browser-assisted **Session Acquisition**, keeping WeGame authentication in real Chrome instead of reimplementing third-party login.
- CDP should only support **Session Acquisition** and cookie extraction; official API calls should use typed HTTP clients, not page-injected JavaScript fetches.
- After **Session Acquisition** succeeds, **Travel Orchestration** should use an **Official Session** through HTTP clients and no longer depend on the live browser page.
- **Session Acquisition** should not simulate DOM clicks.
- The official travel page is the **Session Acquisition** entry point; `--wegame` enables **WeGame Login Shortcut** by opening the Shengqu `goClick=wegame` frame, not by opening a raw Tencent Rail OAuth URL.
- Code should keep **Travel Orchestration** readable and isolated from CDP details; browser automation belongs behind infrastructure services.
- Official API JSON should be represented by thin typed DTOs at the boundary, avoiding dynamic JSON in the main flow.
- Order submission should use one simple **Order Confirmation**. Queue estimates should not be shown in confirmation because the official data is not reliable enough for a user-facing promise.
- **Order Tracking** should keep normal output quiet, showing ongoing spinner/status text and only writing lines when the status changes; verbose output may include every poll.
- Tests should focus on **Offline Coverage**. Real Chrome login and official service behavior remain manual verification paths.
- **Character Discovery** should use a small bounded concurrency limit, defaulting to 4 unless implementation testing shows the official service needs a lower value.
- **Partial Discovery Failure** should not block travel when at least one character is found; it should be summarized tersely, with details available in verbose output.
- Target selection should only present **Available Targets**. If no targets are available, explain that before asking for input.
- The first character picker should show a **Character Selection Catalog**, not only direct-travel characters from **Character Discovery**.
- Away-from-home characters should remain selectable through **Active Travel Orders**. Selecting one starts **Return-Then-Travel** instead of asking the user to manually return first.
- **Source World** must not be used as a synonym for an away-from-home character's **Current Travel World**. Use **Home World** and **Current Travel World** when both matter.
