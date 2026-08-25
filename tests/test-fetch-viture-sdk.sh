#!/usr/bin/env bash
# Fail-closed tests for scripts/fetch-viture-sdk.sh
# Never downloads the proprietary Viture SDK. Safe for public GitHub Actions.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCRIPT="$ROOT/scripts/fetch-viture-sdk.sh"
PASS=0
FAIL=0

assert_fail() {
  local name="$1"
  shift
  local out
  set +e
  out="$("$@" 2>&1)"
  local rc=$?
  set -e
  if [[ $rc -eq 0 ]]; then
    echo "FAIL: $name (expected non-zero)"
    echo "$out"
    FAIL=$((FAIL + 1))
  else
    echo "PASS: $name (rc=$rc)"
    PASS=$((PASS + 1))
  fi
}

# Fail-closed with output checks: must be non-zero, must mention $needle,
# and must not reach the curl/download line.
assert_fail_before_curl() {
  local name="$1"
  local needle="$2"
  shift 2
  local out
  set +e
  out="$("$@" 2>&1)"
  local rc=$?
  set -e
  if [[ $rc -eq 0 ]]; then
    echo "FAIL: $name (expected non-zero)"
    echo "$out"
    FAIL=$((FAIL + 1))
    return
  fi
  if [[ "$out" != *"$needle"* ]]; then
    echo "FAIL: $name (missing '$needle')"
    echo "$out"
    FAIL=$((FAIL + 1))
    return
  fi
  if [[ "$out" == *"downloading"* ]]; then
    echo "FAIL: $name (reached curl/download)"
    echo "$out"
    FAIL=$((FAIL + 1))
    return
  fi
  echo "PASS: $name (rc=$rc, before curl)"
  PASS=$((PASS + 1))
}

assert_ok() {
  local name="$1"
  shift
  if "$@"; then
    echo "PASS: $name"
    PASS=$((PASS + 1))
  else
    echo "FAIL: $name"
    FAIL=$((FAIL + 1))
  fi
}

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

assert_ok "bash -n fetch script" bash -n "$SCRIPT"

# 1) missing manifest
assert_fail_before_curl "missing manifest" "manifest not found:" \
  env SDK_MANIFEST="$tmpdir/nope.toml" bash "$SCRIPT"

# 2) example/placeholder manifest — must die before curl
assert_fail_before_curl "placeholder example manifest" "placeholder/TODO" \
  env SDK_MANIFEST="$ROOT/sdk-manifest.example.toml" bash "$SCRIPT"

# 3) extract_to jail: parent traversal
cat > "$tmpdir/escape.toml" <<'EOF'
[sdk]
name = "escape"
url = "https://example.com/sdk.zip"
sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
extract_to = "Viture/../../hello_flutter"
EOF
assert_fail_before_curl "extract_to traversal" "extract_to=" \
  env SDK_MANIFEST="$tmpdir/escape.toml" bash "$SCRIPT"

# 4) extract_to absolute
cat > "$tmpdir/abs.toml" <<'EOF'
[sdk]
name = "abs"
url = "https://example.com/sdk.zip"
sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
extract_to = "/tmp/viture-sdk"
EOF
assert_fail_before_curl "extract_to absolute" "extract_to=" \
  env SDK_MANIFEST="$tmpdir/abs.toml" bash "$SCRIPT"

# 5) extract_to outside Viture
cat > "$tmpdir/other.toml" <<'EOF'
[sdk]
name = "other"
url = "https://example.com/sdk.zip"
sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
extract_to = "docs"
EOF
assert_fail_before_curl "extract_to not under Viture/" "extract_to='docs'" \
  env SDK_MANIFEST="$tmpdir/other.toml" bash "$SCRIPT"

# 6) valid-looking manifest still refused in CI (no network)
cat > "$tmpdir/ci.toml" <<'EOF'
[sdk]
name = "ci"
url = "https://example.com/sdk.zip"
sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
extract_to = "Viture"
EOF
assert_fail_before_curl "CI_NO_FETCH blocks download" "refusing to download SDK in CI" \
  env SDK_MANIFEST="$tmpdir/ci.toml" CI_NO_FETCH=1 bash "$SCRIPT"

# 7) gitignore must keep secrets/SDK out
assert_ok "gitignore has /Viture/" grep -qxF '/Viture/' "$ROOT/.gitignore"
assert_ok "gitignore has sdk-manifest.toml" grep -qxF 'sdk-manifest.toml' "$ROOT/.gitignore"
assert_ok "gitignore has .sdk-cache/" grep -qxF '.sdk-cache/' "$ROOT/.gitignore"

# 8) example manifest has no real portal host
if grep -Eiq 'shop\.viture\.com|cdn\.viture|download\.viture' "$ROOT/sdk-manifest.example.toml"; then
  echo "FAIL: example manifest contains a Viture download host"
  FAIL=$((FAIL + 1))
else
  echo "PASS: example manifest has no Viture download host"
  PASS=$((PASS + 1))
fi

# 9) workflows never invoke a live SDK download
if [[ -d "$ROOT/.github/workflows" ]]; then
  if grep -RniE 'curl[[:space:]].*viture|wget[[:space:]].*viture|shop\.viture\.com' "$ROOT/.github/workflows"; then
    echo "FAIL: workflow fetches from Viture"
    FAIL=$((FAIL + 1))
  else
    echo "PASS: workflows do not fetch from Viture hosts"
    PASS=$((PASS + 1))
  fi
  if grep -R 'fetch-viture-sdk\.sh' "$ROOT/.github/workflows" | grep -v test-fetch; then
    if grep -R 'scripts/fetch-viture-sdk.sh' "$ROOT/.github/workflows" | grep -v test-fetch-viture; then
      echo "FAIL: workflow calls fetch script outside tests"
      FAIL=$((FAIL + 1))
    else
      echo "PASS: fetch script not invoked live in CI"
      PASS=$((PASS + 1))
    fi
  else
    echo "PASS: fetch script not invoked live in CI"
    PASS=$((PASS + 1))
  fi
fi

echo
echo "$PASS passed, $FAIL failed"
[[ "$FAIL" -eq 0 ]]
