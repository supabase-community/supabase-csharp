#!/usr/bin/env bash
#
# format-suggest.sh [dir] — apply `dotnet format` to changed .cs files and leave
# the fix as an uncommitted working-tree diff, for a CI job to turn into a
# one-click PR suggestion (e.g. via reviewdog/action-suggester).
#
# Unlike gate.sh's stage_format (--verify-no-changes, blocking, never writes),
# this APPLIES the fix in place. It is not part of the gate: the gate stays the
# enforcement mechanism; this script only exists to make the fix easy to accept.
#
# Package resolution and changed-file scoping are shared with gate.sh via
# lib/common.sh (discover_scope) and lib/stages.sh (changed_cs_files) — same
# scope, same set of files, so the suggestion matches exactly what the gate's
# format stage would have failed on.
#
set -euo pipefail

SELF_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SELF_DIR/lib/common.sh"
source "$SELF_DIR/lib/stages.sh"

PACKAGE_DIR="$PWD"; ALL=0; CLI_PROJECT=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --all) ALL=1; shift ;;
    -h|--help) sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    -*) echo "unknown option: $1  (try --help)" >&2; exit 3 ;;
    *)  PACKAGE_DIR="$1"; shift ;;
  esac
done

[[ -d "$PACKAGE_DIR" ]] || { echo "no such directory: $PACKAGE_DIR" >&2; exit 3; }
# Physical path — see gate.sh for why: `git rev-parse --show-toplevel` returns the
# symlink-resolved path, and changed_cs_files strips that root off project dirs.
PACKAGE_DIR="$(cd "$PACKAGE_DIR" && pwd -P)"
PACKAGE_NAME="$(basename "$PACKAGE_DIR")"

have dotnet || { echo "dotnet not on PATH (DOTNET_ROOT=$DOTNET_ROOT)" >&2; exit 3; }
have jq || { echo "jq not found — required by discover_scope's baseline reads. brew install jq" >&2; exit 3; }

CONFIG=""; _d="$PACKAGE_DIR"
while [[ "$_d" != "/" && -n "$_d" ]]; do
  [[ -f "$_d/.gate-config.json" ]] && { CONFIG="$_d/.gate-config.json"; break; }
  _d="$(dirname "$_d")"
done

discover_scope
SCOPE_DIR="$PACKAGE_DIR"

root="$(git -C "$SCOPE_DIR" rev-parse --show-toplevel)"
files=(); rel=""
while IFS= read -r rel; do [[ -n "$rel" && -f "$root/$rel" ]] && files+=("$rel"); done < <(changed_cs_files)

if [[ ${#files[@]} -eq 0 ]]; then
  echo "no changed .cs files in scope — nothing to format"
  exit 0
fi

for i in "${!PKG_DIR[@]}"; do
  for p in "${PKG_PROJ[$i]}" "${PKG_TEST[$i]}"; do
    pdir="$(dirname "$p")"; pdir="${pdir#$root/}"
    inc=()
    for rf in "${files[@]}"; do [[ "$rf" == "$pdir/"* ]] && inc+=("$rf"); done
    [[ ${#inc[@]} -eq 0 ]] && continue
    echo "== $p (${#inc[@]} file(s)) =="
    ( cd "$root" && dotnet format "$p" --severity warn --include "${inc[@]}" )
  done
done
