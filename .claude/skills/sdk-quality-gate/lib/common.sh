# lib/common.sh — environment, terminal, tool checks, jq readers, process runner.
# Sourced by gate.sh; runs under its `set -euo pipefail`.

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

if [[ -t 1 && -z "${NO_COLOR:-}" ]]; then
  RED=$'\033[31m'; GRN=$'\033[32m'; YEL=$'\033[33m'
  DIM=$'\033[2m'; BLD=$'\033[1m'; OFF=$'\033[0m'
else RED=""; GRN=""; YEL=""; DIM=""; BLD=""; OFF=""; fi

have() { command -v "$1" >/dev/null 2>&1; }

# Baseline / config readers. $BASELINE (per-package measurements) and $CONFIG
# (repo-wide settings) are globals set by discovery. jq is required — see gate.sh:
# without it every read returns empty, no baseline is ever enforced, and the gate
# reports "baseline established by this run" on every invocation, passing silently.
bget() {  # bget <jq-path> — read a field from the per-package baseline
  [[ -f "$BASELINE" ]] || { echo ""; return; }
  jq -r "$1 // empty" "$BASELINE"
}

cget() {  # cget <jq-path> — package baseline wins, then repo config
  local v; v="$(bget "$1")"
  if [[ -z "$v" && -n "$CONFIG" ]]; then v="$(jq -r "$1 // empty" "$CONFIG")"; fi
  echo "$v"
}

# Run a command with output captured to a log, recording its exit in $RC without
# tripping `set -e`. Callers read $RC immediately after.
RC=0
run() { local log="$1"; shift; set +e; "$@" >"$log" 2>&1; RC=$?; set -e; }

# Project discovery, shared by single- and solution-scope resolution.
# Production project = the one non-test csproj. Exclusion patterns are QUOTED so
# find does the matching: passed unquoted through a variable, the shell would glob
# '*Tests*' against the cwd first and the exclusion would silently stop working.
find_production_project() {  # find_production_project <dir>
  find "$1" -name '*.csproj' -not -name '*Tests*' \
    -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null | sort | head -n1
}
find_test_project() {  # find_test_project <dir>
  find "$1" \( -name '*Tests*.csproj' -o -name '*.Tests.csproj' \) \
    -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null | sort -u | head -n1
}
all_test_projects() {  # all_test_projects <dir>
  find "$1" -name '*Tests*.csproj' -o -name '*.Tests.csproj' 2>/dev/null \
    | grep -v '/bin/\|/obj/' | sort -u
}
