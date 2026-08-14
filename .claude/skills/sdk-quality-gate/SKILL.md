---
name: sdk-quality-gate
description: Use before declaring any Supabase C# SDK change done or opening a PR, and as the Verify step of every flow. Runs the committed scripts/quality-gate/gate.sh script — the mechanized gauntlet (build/analyzers with a warning ratchet, format on changed files, inner-loop tests, vulnerability scan and E2E/acceptance tests, plus a public-API diff signal) — and reports its verdict. This is the deterministic "is it done" check; do not report a change as done without a PASS.
---

# Skill: SDK quality gate

The gauntlet is a program, not a procedure. Run it and report what it says.

The script lives at `scripts/quality-gate/gate.sh` — committed alongside the code
so contributors and CI run the identical command, not just agents. Paths below
assume the working directory is the workspace root (`sdk-csharp`); the script
itself is location-independent, so an absolute path works equally well.

```
scripts/quality-gate/gate.sh <package>          # full gate — before a PR
scripts/quality-gate/gate.sh <package> --fast    # inner loop only
```

There are two modes. `--fast` is the inner loop only (build, format, tests) — the
fast local red/green cycle; the default is the full gate and adds security, the
public-API check and E2E. `<package>` is a directory — e.g. `gotrue-csharp`,
`core-csharp` — and defaults to the current directory. Run with `bash <path>` if
the executable bit is unset.

**Mutation testing is not part of the gate.** It is too slow for the inner/PR
loop and runs in its own scheduled GitHub Action.

**CI runs the same command.** The `Build and Test` workflow
(`.github/workflows/build-and-test.yml`) invokes `scripts/quality-gate/gate.sh`
on every push and pull request, so a local PASS and a CI PASS mean the same
thing. The blocking-stage verdict here is the one that gates the merge.

## Reading the verdict

| Verdict | Exit | Meaning |
|---|---|---|
| `PASS` | 0 | Blocking stages green. Report done; surface the signals. |
| `PARTIAL` | 0 | `--fast` only. **Not** sufficient to declare done. |
| `INCOMPLETE` | 2 | A blocking stage could not run. Unverified ≠ verified. |
| `FAIL` | 1 | A blocking stage failed. |

Blocking stages `[B]` decide the verdict: build/analyzers, format, the inner loop,
dependency vulnerabilities, public-API declared, **and E2E/acceptance**. A failing
E2E blocks the merge exactly like a failing unit test — there is no green build
with a red test. A package that carries no E2E tests is not a failure; only a real
E2E failure, or a stack that could not be reached, holds the gate.

The one signal stage `[s]` is the **public-API diff**: it never fails the build,
because the maintainer is the merge gate on breaking changes — a break may be
intended. The tool informs that call, it does not veto it (QUALITY_RUBRIC §4).

Stages are skipped only when an earlier failure makes them impossible or
meaningless — a failed build blocks format and tests; a red inner loop blocks
E2E. Nothing else suppresses a stage, and anything not run is still reported as
`SKIP` with its reason.

## Reporting

Paste the summary table verbatim. Never paraphrase a stage result, and never
report a stage as passing when the script recorded `SKIP`. Detail is in
`<package>/.gate/report.json` and the per-stage logs it references.

Add the triage a number alone doesn't carry:

- **Public-API diff** — additive, or a break needing sign-off + a major plan +
  `[Obsolete]` + a `MIGRATION_vN.md` entry.

## On failure — fix the cause, not the check

Not acceptable repairs:

- `#pragma warning disable`, or lowering a severity in `.editorconfig`
- re-categorising a test so a filter skips it
- deleting or `[Ignore]`-ing a red test
- retrying or quarantining a flaky E2E — flakiness is a design defect
- hand-editing `.gate-baseline.json` upward to make a stage pass

If a blocking stage fails for a reason outside the change's scope, stop and
report it. Do not work around it and continue.

## Baselines

`<package>/.gate-baseline.json` is committed and holds the discovered project
paths and the warning count per code. It is created on the
first run and **ratchets down automatically** — no flags. Raising a number means
editing the file by hand, so the increase appears in code review.

Optional keys: `formatBaseRef` (default: `origin/HEAD`, then `main`/`master`)
for the changed-file comparison, and `e2eHealthUrl` if the local stack isn't on
the default port.

When the script reports `baseline lowered`, commit the file.

## What the script cannot check

These stay human judgment. Walk them; don't recall them:

- [ ] Behavior asserted through the public surface; testability friction fixed by
      changing the design, not by exposing internals.
- [ ] Approved wire-shape snapshots are *right* — the contract tests only prove
      the current output still matches them, not that the approved bytes are correct.
- [ ] `CancellationToken` genuinely honored through the call path, not just
      accepted at the entry point.
- [ ] Orchestration reads as named intent; expected failures returned, not thrown.
- [ ] Public surface minimal and documented; entry points carry an `<example>`.
- [ ] Parity deviations noted in the PR description.
- [ ] Commits follow `type(scope): description`. The scope is required since the
      move to a monorepo and names the affected package — `functions`, `gotrue`,
      `postgrest`, `realtime`, `storage`, `core`, `supabase` (append `!` before
      the colon for breaking changes, e.g. `feat(functions)!:`).

Every item that becomes mechanizable — an analyzer, an architecture test, a
`TestConventions` rule — should move out of this list and into the script. The
list is meant to shrink.