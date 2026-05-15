# Travel orchestration boundary

Accepted: Travel Orchestration owns the application-level Travel Flow after Session Acquisition has produced an Official Session. It coordinates Character Selection Catalog building, direct Travel Order submission, Return-Then-Travel sequencing, Character Reconciliation, target refresh, and Order Tracking. It speaks in Character, Home Region, Home World, Source Region, Source World, Available Target, Travel Order, Active Travel Order, Return Home, and lifecycle outcome terms from `CONTEXT.md`.

## Context

The existing flow already preserves the major safety rules: browser-assisted Session Acquisition happens before orchestration, away-from-home characters are selected from Active Travel Orders, Return Home completes before refreshed Character data is used, and the chosen target is refreshed before a new Travel Order is submitted.

The next refactor should deepen that behavior without making prompt rendering, browser/CDP details, or official endpoint wire mechanics part of the orchestration model.

## Consequences

- Travel Orchestration depends on an acquired Official Session and typed official operations, preserving ADR-0001. It must not scrape or click login-page DOM elements, keep a live browser page, open raw OAuth URLs, or store Shengqu login attributes.
- Prompt rendering remains an edge adapter. Orchestration may ask for decisions through prompt interfaces, but it should not depend on Spectre.Console layout details.
- Official endpoint mechanics remain behind official API work. Character and Character Selection Catalog callers must not need official wire JSON to describe a Character.
- Direct Travel Flow offline coverage must protect: session acquisition, Character Selection Catalog use, Available Target selection, Order Confirmation, exactly one Travel Order submission, and Order Tracking completion.
- Return-Then-Travel offline coverage must protect: selecting an away-from-home Character from an Active Travel Order, confirming the full journey, completing Return Home, refreshing Character data, refreshing the selected Available Target, and stopping with no new Travel Order when the target disappears.
- Return Home should report lifecycle outcomes that Return Flow and Return-Then-Travel can consume, while preserving ADR-0002 finite retry behavior.
- Issue reporting for Partial Discovery Failure and Active Travel Order reads remains orchestration-facing behavior, but verbose console rendering stays at the edge.
