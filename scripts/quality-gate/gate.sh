#!/usr/bin/env bash
#
# gate.sh — the Supabase C# SDK quality gauntlet (QUALITY_RUBRIC §4), as a program.
#
#   gate.sh [dir]           full gate — build, tests, security, public API, E2E
#   gate.sh [dir] --fast    inner loop only — the agent's red/green cycle
#
# One directory with several packages runs as a solution: one build, one scan, one
# API check, per-package tests, and a single verdict. A single package keeps the
# fast per-package path the inner loop uses. --all forces the solution run.
#
# Mutation testing is not run here — it is too slow for the inner/PR loop and lives
# in its own scheduled GitHub Action.
#
# Config that belongs to the package lives in a committed .gate-baseline.json, not
# on the command line. The file is created on the first run.
#
# Exit codes:
#   0  all blocking stages passed
#   1  a blocking stage FAILED
#   2  a blocking stage could not be run — "couldn't check" is not "done"
#   3  usage / environment error
#
# Signal stages never affect the exit code; they are surfaced for the human merge
# decision (QUALITY_RUBRIC §4).
#
# Layout: this entry script does argument parsing, scope discovery and
# orchestration. The stages and their machinery live in lib/ beside it:
#   lib/common.sh    environment, jq readers, project discovery, run()
#   lib/report.sh    the row store, streaming renderer and report.json
#   lib/warnings.sh  the analyzer ratchet and per-package baseline IO
#   lib/coverage.sh  the line-coverage ratchet and per-package baseline IO
#   lib/stages.sh    one definition of each gauntlet stage, for both scopes
#
set -euo pipefail

SELF_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SELF_DIR/lib/common.sh"
source "$SELF_DIR/lib/report.sh"
source "$SELF_DIR/lib/warnings.sh"
source "$SELF_DIR/lib/coverage.sh"
source "$SELF_DIR/lib/stages.sh"

# -------------------------------------------------------------------- options

PACKAGE_DIR="$PWD"; MODE="standard"; ALL=0; CLI_PROJECT=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --fast) MODE="fast"; shift ;;
    --full) shift ;;   # accepted as an alias: the default run is already the full gate
    --all)  ALL=1; shift ;;
    -h|--help) sed -n '3,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    -*) echo "unknown option: $1  (try --help)" >&2; exit 3 ;;
    *)  PACKAGE_DIR="$1"; shift ;;
  esac
done

# Accept a package directory, or a .csproj (use its directory, and pin it).
if [[ -f "$PACKAGE_DIR" ]]; then
  case "$PACKAGE_DIR" in
    *.csproj)
      CLI_PROJECT="$(cd "$(dirname "$PACKAGE_DIR")" && pwd -P)/$(basename "$PACKAGE_DIR")"
      PACKAGE_DIR="$(dirname "$PACKAGE_DIR")" ;;
    *) echo "expected a package directory or a .csproj, got a file: $PACKAGE_DIR" >&2; exit 3 ;;
  esac
elif [[ ! -d "$PACKAGE_DIR" ]]; then
  echo "no such directory: $PACKAGE_DIR" >&2; exit 3
fi
# Physical path (pwd -P): `git rev-parse --show-toplevel` returns the symlink-
# resolved path, and the format stage strips that root off project dirs to match
# changed files. A logical path here (e.g. macOS /var -> /private/var) would fail
# the strip and every file would "match nothing" — a silent format false-pass.
PACKAGE_DIR="$(cd "$PACKAGE_DIR" && pwd -P)"
PACKAGE_NAME="$(basename "$PACKAGE_DIR")"

have dotnet || { echo "dotnet not on PATH (DOTNET_ROOT=$DOTNET_ROOT)" >&2; exit 3; }
# Required, not optional: every baseline read goes through jq. Without it the reads
# return empty, no baseline is ever enforced, and the gate reports "baseline
# established by this run" on every invocation — passing silently.
have jq || { echo "jq not found — required to read baselines. brew install jq" >&2; exit 3; }

# Repo-wide settings live in one .gate-config.json, found by walking up from the
# package. Per-package measurements stay in .gate-baseline.json, which may override
# any config key.
CONFIG=""; _d="$PACKAGE_DIR"
while [[ "$_d" != "/" && -n "$_d" ]]; do
  [[ -f "$_d/.gate-config.json" ]] && { CONFIG="$_d/.gate-config.json"; break; }
  _d="$(dirname "$_d")"
done

# --------------------------------------------------------------- scope discovery
# One or many packages? A baseline that pins testProject settles it as single.
# Otherwise, the number of test projects under the directory decides: more than one
# means the directory holds several packages, so the run is a solution. --all forces
# the solution run for a directory that would otherwise resolve to one package.

BASELINE="$PACKAGE_DIR/.gate-baseline.json"
PKG_DIR=(); PKG_NAME=(); PKG_PROJ=(); PKG_TEST=()
MULTI=0; SLN=""

_bpkg_test="$(bget .testProject)"
[[ -n "$_bpkg_test" ]] && _bpkg_test="$PACKAGE_DIR/$_bpkg_test"

if [[ -n "$_bpkg_test" && -f "$_bpkg_test" ]]; then
  MULTI=0                                   # baseline pins a single package
else
  _dirs=(); _seen=""
  while IFS= read -r t; do
    [[ -n "$t" ]] || continue
    p="$(cd "$(dirname "$(dirname "$t")")" && pwd -P)"
    case "$_seen" in *"|$p|"*) ;; *) _seen="$_seen|$p|"; _dirs+=("$p") ;; esac
  done < <(all_test_projects "$PACKAGE_DIR")
  if [[ ${#_dirs[@]} -gt 1 || ( $ALL -eq 1 && ${#_dirs[@]} -ge 1 ) ]]; then
    MULTI=1; PKG_DIR=("${_dirs[@]}")
  fi
fi

if [[ $MULTI -eq 1 ]]; then
  # Solution: one .sln at the git root drives the build; each package resolves its
  # own two projects for tests and warning attribution.
  SCOPE_DIR="$PACKAGE_DIR"
  _sroot="$(git -C "$SCOPE_DIR" rev-parse --show-toplevel 2>/dev/null || echo "$SCOPE_DIR")"
  SLN="$(find "$_sroot" -maxdepth 1 -name '*.sln' 2>/dev/null | sort | head -n1)"
  [[ -f "$SLN" ]] || { echo "solution mode needs a .sln at $_sroot, none found" >&2; exit 3; }
  for p in "${PKG_DIR[@]}"; do
    PKG_NAME+=("$(basename "$p")")
    PKG_PROJ+=("$(find_production_project "$p")")
    PKG_TEST+=("$(find_test_project "$p")")
  done
  BUILD_TARGET="$SLN"; SCAN_TARGET="$SLN"
else
  # Single package: production project = the one non-test csproj, unless the baseline
  # or a .csproj argument pins it. The test project is required.
  SCOPE_DIR="$PACKAGE_DIR"
  _proj="$(bget .project)"; [[ -n "$_proj" ]] && _proj="$PACKAGE_DIR/$_proj"
  [[ -f "${_proj:-}"      ]] || _proj="$(find_production_project "$PACKAGE_DIR")"
  [[ -n "$CLI_PROJECT"    ]] && _proj="$CLI_PROJECT"
  _test="${_bpkg_test:-}"
  [[ -f "${_test:-}"      ]] || _test="$(find_test_project "$PACKAGE_DIR")"
  [[ -f "${_test:-}"      ]] || { echo "no test project found under $PACKAGE_DIR — set testProject in $BASELINE" >&2; exit 3; }
  [[ -f "${_proj:-}"      ]] || { echo "no production project found under $PACKAGE_DIR — set production project in $BASELINE" >&2; exit 3; }
  PKG_DIR=("$PACKAGE_DIR"); PKG_NAME=("$PACKAGE_NAME"); PKG_PROJ=("$_proj"); PKG_TEST=("$_test")
  BUILD_TARGET="$_test"; SCAN_TARGET="$_proj"
fi

SCOPE_OUT="$SCOPE_DIR/.gate"; SCOPE_LOGS="$SCOPE_OUT/logs"; mkdir -p "$SCOPE_LOGS"

# ==================================================================== orchestration
# The short-circuits are CAUSAL, not sequential: only a failed build makes the
# stages below impossible. A format violation says nothing about whether the tests
# pass, so it must not hide them. Anything not run is still recorded as SKIP with a
# reason — a stage that silently vanishes reads as verified when it wasn't.
#
# Three test-execution paths (STACK_OK probed once, right after the build, so
# both "which test path to run" and "which SKIP markers to emit" share one
# network check instead of probing twice):
#   --fast                    stage_inner_loop, filtered TestCategory!=E2E — the
#                              fast local cycle; no coverage, verdict is PARTIAL.
#   full mode, stack up       stage_tests_full — ONE unfiltered run (every
#                              TestCategory together) that is both the test result
#                              and the coverage source, then stage_coverage.
#   full mode, stack down     stage_inner_loop as a fallback, so local dev still
#                              gets build/test feedback without `supabase start`;
#                              E2E and coverage both SKIP — "full" coverage isn't
#                              measurable without the E2E half of the suite.

if [[ $MULTI -eq 1 ]]; then
  echo "${DIM}$(basename "$SLN")  ·  ${#PKG_DIR[@]} packages  ·  mode=$MODE${OFF}"
else
  echo "${DIM}$SCOPE_DIR  ·  $(basename "$BUILD_TARGET")  ·  mode=$MODE${OFF}"
fi
echo

stage_build
STACK_OK=0
if [[ $BUILD_OK -eq 1 && "$MODE" != "fast" ]]; then
  # stage_build just reset $BASELINE to "" (so it doesn't leak into the
  # scope-wide stages below) — restore it to a package's own baseline before
  # probing, so a per-package e2eHealthUrl override is actually read. Without
  # this, the probe silently falls through to $CONFIG / the hardcoded default,
  # which can false-positive against an unrelated stack already running on the
  # default port. First package is representative for a solution run.
  BASELINE="${PKG_DIR[0]}/.gate-baseline.json"
  if stack_up; then STACK_OK=1; fi
  BASELINE=""
fi

if [[ $BUILD_OK -eq 1 ]]; then
  stage_format
  if [[ "$MODE" == "fast" ]]; then
    stage_inner_loop
  elif [[ $STACK_OK -eq 1 ]]; then
    stage_tests_full
    # No separate id-7 row here: stage_tests_full's own per-package rows already
    # carry the full outcome (label says "Unit + Contract + E2E"), so a second row
    # would only restate it — and imprecisely, since "E2E ran" isn't uniformly true
    # across every package (a package with zero E2E tests has nothing to report).
    stage_coverage
  else
    stage_inner_loop
  fi
else
  add 1b "Format + naming"              block "" SKIP "not run — build failed" ""
  add 2  "Inner loop (Unit + Contract)" block "" SKIP "not run — build failed" ""
fi

if [[ "$MODE" != "fast" ]]; then
  stage_security
  # The sync check compiles (dotnet format runs the analyzers), so a failed build
  # makes it impossible, not failing — skip it causally, as with format and tests.
  if [[ $BUILD_OK -eq 1 ]]; then stage_api_sync
  else add 5a "Public API declared" block "" SKIP "not run — build failed" ""; fi
  stage_api_diff

  if [[ $BUILD_OK -eq 0 ]]; then
    add 7  "E2E / acceptance" block "" SKIP "not run — build failed" ""
    add 2b "Coverage (line)"  block "" SKIP "not run — build failed" ""
  elif [[ $STACK_OK -eq 0 ]]; then
    add 7  "E2E / acceptance" block "" SKIP "stack down per $STACK_CHECK — run: supabase start" "$SCOPE_LOGS/7-stack.log"
    add 2b "Coverage (line)"  block "" SKIP "not measured — stack down, full run did not execute" ""
  fi
fi

# -------------------------------------------------------------------- verdict
EXIT=0
for i in "${!R_ID[@]}"; do
  [[ "${R_KIND[$i]}" == "block" ]] || continue
  case "${R_STATUS[$i]}" in
    FAIL) EXIT=1 ;;
    SKIP) [[ $EXIT -eq 0 ]] && EXIT=2 ;;
  esac
done
case $EXIT in 0) V="PASS" ;; 1) V="FAIL" ;; 2) V="INCOMPLETE" ;; esac
[[ "$MODE" == "fast" && $EXIT -eq 0 ]] && V="PARTIAL"

write_report "$V" "$EXIT"
print_verdict "$V" "$EXIT"
exit $EXIT
