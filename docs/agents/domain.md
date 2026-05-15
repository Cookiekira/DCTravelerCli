# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- `CONTEXT.md` at the repo root.
- Relevant ADRs under `docs/adr/`.

If any expected file does not exist, proceed silently. Do not suggest creating domain docs upfront; producer skills create or update them lazily when terms or decisions are resolved.

## File structure

This is a single-context repo:

```text
/
├── CONTEXT.md
├── docs/adr/
└── docs/agents/
```

## Use the glossary's vocabulary

When output names a domain concept in an issue title, refactor proposal, hypothesis, or test name, use the term as defined in `CONTEXT.md`. Do not drift to synonyms the glossary explicitly avoids.

## Flag ADR conflicts

If output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (example decision) - but worth reopening because..._
