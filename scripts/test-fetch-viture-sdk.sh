#!/usr/bin/env bash
# Offline fail-close tests for scripts/fetch-viture-sdk.sh.
# Does not rewrite the fetch script. Does not hit the network.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
FETCH_SRC="$HERE/fetch-viture-sdk.sh"
EXAMPLE_SRC="$REPO/sdk-manifest.example.toml"

fail() { echo "FAIL: $*" >&2; exit 1; }
pass() { echo "PASS: $*"; }

[[ -f "$FETCH_SRC" ]] || fail "missing $FETCH_SRC"
[[ -f "$EXAMPLE_SRC" ]] || fail "missing $EXAMPLE_SRC"

echo "==> bash -n scripts/fetch-viture-sdk.sh"
bash -n "$FETCH_SRC" || fail "bash -n scripts/fetch-viture-sdk.sh"
pass "bash -n scripts/fetch-viture-sdk.sh"

WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT

mkdir -p "$WORKDIR/scripts"
cp "$FETCH_SRC" "$WORKDIR/scripts/fetch-viture-sdk.sh"
chmod +x "$WORKDIR/scripts/fetch-viture-sdk.sh"
cp "$EXAMPLE_SRC" "$WORKDIR/sdk-manifest.example.toml"
FETCH="$WORKDIR/scripts/fetch-viture-sdk.sh"

run_fetch() {
  local out rc
  set +e
  out="$(bash "$FETCH" 2>&1)"
  rc=$?
  set -e
  printf '%s\n' "$out"
  return "$rc"
}

echo "==> missing manifest dies"
rm -f "$WORKDIR/sdk-manifest.toml"
set +e
out="$(run_fetch)"
rc=$?
set -e
[[ "$rc" -ne 0 ]] || fail "missing manifest: expected non-zero exit, got $rc"
[[ "$out" == *"manifest not found:"* ]] || fail "missing manifest: expected 'manifest not found:' in output: $out"
[[ "$out" == *"sdk-manifest.example.toml"* ]] || fail "missing manifest: expected example-toml hint: $out"
pass "missing manifest dies (exit $rc)"

echo "==> placeholder URL/sha (example toml as-is) dies before curl"
cp "$WORKDIR/sdk-manifest.example.toml" "$WORKDIR/sdk-manifest.toml"
set +e
out="$(run_fetch)"
rc=$?
set -e
[[ "$rc" -ne 0 ]] || fail "placeholder: expected non-zero exit, got $rc"
[[ "$out" == *"placeholder/TODO"* ]] || fail "placeholder: expected placeholder/TODO die: $out"
[[ "$out" != *"downloading"* ]] || fail "placeholder: must die before curl/download: $out"
pass "placeholder URL/sha dies before curl (exit $rc)"

echo "==> extract_to outside Viture/ dies (local file:// zip, matching sha)"
ZIPFILE="$WORKDIR/dummy-sdk.zip"
python3 - "$ZIPFILE" <<'PY'
import sys, zipfile
z = zipfile.ZipFile(sys.argv[1], "w")
z.writestr("dummy.txt", "offline-test\n")
z.close()
PY
SHA="$(sha256sum "$ZIPFILE" | awk '{print $1}')"
[[ "$SHA" =~ ^[0-9a-fA-F]{64}$ ]] || fail "could not hash dummy zip"

cat > "$WORKDIR/sdk-manifest.toml" <<EOF
[sdk]
name = "offline-extract-test"
url = "file://${ZIPFILE}"
sha256 = "${SHA}"
extract_to = "docs"
EOF

set +e
out="$(run_fetch)"
rc=$?
set -e
[[ "$rc" -ne 0 ]] || fail "bad extract_to: expected non-zero exit, got $rc"
[[ "$out" == *"not in allowed set"* ]] || fail "bad extract_to: expected allowlist die: $out"
[[ "$out" == *"extract_to='docs'"* ]] || fail "bad extract_to: expected extract_to='docs' in error: $out"
pass "extract_to outside Viture/ dies (exit $rc)"

echo "All fetch-viture-sdk fail-close tests passed."
