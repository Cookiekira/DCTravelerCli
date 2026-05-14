# Seamless return-then-travel

Accepted: DC Traveler CLI will treat away-from-home characters as selectable in the normal travel flow. When the selected character is currently traveling, the CLI asks for the new target first, confirms the whole two-leg journey once, returns the character home, refreshes official character and target data, and only then submits the new travel order.

## Context

The official travel workflow only lets a character start a new travel order from its home world. A character that has already traveled away from home does not appear in the role-list scan for normal travel, so a user previously had to manually return home before the CLI could find and travel that character again.

Official order data can identify active outgoing travel orders and includes the home world and current travel world needed to call the travel-back API. That makes a seamless return-then-travel flow possible without keeping browser automation alive after login.

## Consequences

- The first travel picker is a character selection catalog, combining direct-travel characters with away-from-home characters from active travel orders.
- Away-from-home characters display both current location and home world so the user can tell why a return is needed.
- The CLI confirms the complete intended journey before starting the return: current travel world to home world, then home world to target world.
- The CLI must refresh character data after return home and must not submit a new travel order with stale role-list payload.
- Character reconciliation prefers role id. If only role name is available, the match must be unique within the home world.
- If the selected target becomes unavailable after the return, the CLI stops with the character home rather than submitting a different travel order.
- A standalone return flow is also exposed for users who only want to return a traveling character home.
- Return home uses finite retry with about 65 seconds between attempts and stops after 3 failed submit/status attempts.
- Local `--yes` skips CLI-owned confirmations only. Official second-step confirmation still requires user input.
