#!/usr/bin/env bash
# In-memory tests for scripts/lib/sdk-fetch-policy.sh.
# Strings only: no manifest files, network, archive tools, or env reads inside the cores.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=../scripts/lib/sdk-fetch-policy.sh
source "$ROOT/scripts/lib/sdk-fetch-policy.sh"

PASS=0
FAIL=0

# Cores must report deny as a typed line and exit 0. A bare failure is a bug.
assert_result() {
  local name="$1"
  local expected="$2"
  shift 2
  local got rc
  set +e
  got="$("$@" 2>&1)"
  rc=$?
  set -e
  if [[ $rc -ne 0 ]]; then
    echo "FAIL: $name (core exited $rc; expected a typed result)"
    echo "$got"
    FAIL=$((FAIL + 1))
    return
  fi
  if [[ "$got" == "$expected" ]]; then
    echo "PASS: $name"
    PASS=$((PASS + 1))
  else
    echo "FAIL: $name"
    echo "  expected: [$expected]"
    echo "  got:      [$got]"
    FAIL=$((FAIL + 1))
  fi
}

HEX64="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

echo "== decide_placeholder =="
assert_result "empty is deny empty" "deny empty" decide_placeholder ""
assert_result "TODO wins before example.invalid" "deny todo" \
  decide_placeholder "https://TODO.example.invalid/viture-sdk-REPLACE_VERSION.zip"
assert_result "REPLACE marker" "deny replace" \
  decide_placeholder "REPLACE_WITH_64_HEX_SHA256_DIGEST_OF_THE_ZIP"
assert_result "example.invalid host" "deny example_invalid" \
  decide_placeholder "https://downloads.example.invalid/sdk.zip"
assert_result "ordinary url is ok" "ok" \
  decide_placeholder "https://example.com/sdk.zip"

echo "== decide_digest =="
assert_result "placeholder digest keeps replace code" "deny replace" \
  decide_digest "REPLACE_WITH_64_HEX_SHA256_DIGEST_OF_THE_ZIP"
assert_result "empty digest" "deny empty" decide_digest ""
assert_result "short digest" "deny bad_hex" decide_digest "abcd"
assert_result "non-hex digest" "deny bad_hex" \
  decide_digest "gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg"
assert_result "64 hex is ok" "ok" decide_digest "$HEX64"
assert_result "uppercase hex is ok" "ok" \
  decide_digest "ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD"

echo "== decide_checksum_match =="
assert_result "identical digests" "ok" decide_checksum_match "$HEX64" "$HEX64"
assert_result "case-insensitive match" "ok" \
  decide_checksum_match \
  "ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD" \
  "abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd"
assert_result "mismatch" "deny mismatch" \
  decide_checksum_match "$HEX64" "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"

echo "== decide_extract_path =="
assert_result "empty extract is cache-only" "ok" decide_extract_path ""
assert_result "Viture root" "ok" decide_extract_path "Viture"
assert_result "path under Viture" "ok" decide_extract_path "Viture/Linux_x86_64"
assert_result "parent traversal" "deny traversal" \
  decide_extract_path "Viture/../../hello_flutter"
assert_result "dotdot inside tree" "deny traversal" \
  decide_extract_path "Viture/foo/../bar"
assert_result "absolute slash" "deny absolute" decide_extract_path "/tmp/viture-sdk"
assert_result "absolute backslash" "deny absolute_backslash" \
  decide_extract_path '\Windows\Temp\sdk'
assert_result "outside Viture" "deny outside_viture" decide_extract_path "docs"

echo "== decide_archive_members =="
assert_result "relative members" "ok" decide_archive_members $'include/foo.h\nlib/bar.so'
assert_result "blank lines skipped" "ok" decide_archive_members $'include/foo.h\n\nlib/bar.so'
assert_result "empty listing" "ok" decide_archive_members ""
assert_result "member escape" "deny escape ../../etc/passwd" \
  decide_archive_members $'include/foo.h\n../../etc/passwd'
assert_result "absolute member" "deny absolute /etc/passwd" \
  decide_archive_members "/etc/passwd"

echo "== decide_ci_fetch =="
assert_result "no CI flags allows fetch" "ok" decide_ci_fetch "" ""
assert_result "CI_NO_FETCH=1 blocks" "deny ci_blocked" decide_ci_fetch "" "1"
assert_result "GITHUB_ACTIONS set blocks" "deny ci_blocked" decide_ci_fetch "true" ""
assert_result "CI_NO_FETCH other than 1 does not block" "ok" decide_ci_fetch "" "true"
# Same-shell variables must not count as inputs. Prefix assignment on a
# function would be visible; the core may only read its parameters.
decide_ci_fetch_ignoring_env() {
  CI_NO_FETCH=1
  GITHUB_ACTIONS=true
  decide_ci_fetch "" ""
}
assert_result "shell env is ignored when arguments are empty" "ok" \
  decide_ci_fetch_ignoring_env

echo "== toml_lookup =="
MANIFEST_TEXT='
# secret = "should-not-parse"
[sdk]
name = "Viture SDK"
url = "https://example.com/sdk.zip?a=1"
sha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
extract_to = "Viture"

[unity]
name = "com.viture.xr"
url = "https://example.com/com.viture.xr-0.5.0.tgz"
extract_to = ""
'
assert_result "sdk url keeps equals in value" "ok https://example.com/sdk.zip?a=1" \
  toml_lookup sdk url "$MANIFEST_TEXT"
assert_result "sdk extract_to" "ok Viture" toml_lookup sdk extract_to "$MANIFEST_TEXT"
assert_result "unity name does not leak into sdk" "ok Viture SDK" \
  toml_lookup sdk name "$MANIFEST_TEXT"
assert_result "empty quoted value" "ok" toml_lookup unity extract_to "$MANIFEST_TEXT"
assert_result "missing key" "deny missing" toml_lookup sdk version "$MANIFEST_TEXT"
assert_result "missing section" "deny missing" toml_lookup other url "$MANIFEST_TEXT"
assert_result "commented key is not a value" "deny missing" \
  toml_lookup sdk secret "$MANIFEST_TEXT"
assert_result "header with trailing comment is not the section" "deny missing" \
  toml_lookup side url $'[side] # comment\nurl = "https://example.com/x.zip"\n'
assert_result "first matching section wins" "ok first" \
  toml_lookup sdk name $'[sdk]\nname = "first"\n[other]\nname = "nope"\n[sdk]\nname = "second"\n'
assert_result "key prefix is not a match" "deny missing" \
  toml_lookup sdk url $'[sdk]\nurl_backup = "https://example.com/nope.zip"\n'

echo "== decide_workflow_script_line =="
assert_result "unrelated line" "ok absent" \
  decide_workflow_script_line "        run: bash tests/test-sdk-fetch-policy.sh"
assert_result "end-anchored bash -n is syntax only" "ok syntax_only" \
  decide_workflow_script_line "        run: bash -n scripts/fetch-viture-sdk.sh"
assert_result "bash -n without leading space is live" "deny live_invoke" \
  decide_workflow_script_line "bash -n scripts/fetch-viture-sdk.sh"
assert_result "bash -n chained to a live run" "deny live_invoke" \
  decide_workflow_script_line "run: bash -n scripts/fetch-viture-sdk.sh && bash scripts/fetch-viture-sdk.sh"
assert_result "direct invocation" "deny live_invoke" \
  decide_workflow_script_line "        run: bash scripts/fetch-viture-sdk.sh"
assert_result "test harness mention is not a live fetch" "ok test_reference" \
  decide_workflow_script_line "see tests/test-fetch-viture-sdk.sh and scripts/fetch-viture-sdk.sh"

echo "== decide_workflow_host_line =="
assert_result "prose mention of Viture is ok" "ok" \
  decide_workflow_host_line "    # Never download the proprietary Viture SDK."
assert_result "curl of a Viture host" "deny viture_host" \
  decide_workflow_host_line "        run: curl -fL https://shop.viture.com/sdk.zip"
assert_result "wget match is case-insensitive" "deny viture_host" \
  decide_workflow_host_line "        run: WGET https://cdn.example/VitureSDK.tgz"
assert_result "curl with no viture token" "ok" \
  decide_workflow_host_line "        run: curl -fL https://example.com/sdk.zip"

# Builtins only. An external command under PATH= fails the function (set -e).
all_cores_without_path() {
  set -euo pipefail
  PATH=
  local r
  r="$(decide_placeholder "https://TODO.example.invalid/x")"
  [[ "$r" == "deny todo" ]]
  r="$(decide_digest "abcd")"
  [[ "$r" == "deny bad_hex" ]]
  r="$(decide_checksum_match aa AA)"
  [[ "$r" == "ok" ]]
  r="$(decide_extract_path "Viture/x")"
  [[ "$r" == "ok" ]]
  r="$(decide_archive_members $'a\n/b')"
  [[ "$r" == "deny absolute /b" ]]
  r="$(decide_ci_fetch "" "1")"
  [[ "$r" == "deny ci_blocked" ]]
  r="$(toml_lookup sdk url $'[sdk]\nurl = "https://example.com/a.zip"\n')"
  [[ "$r" == "ok https://example.com/a.zip" ]]
  r="$(decide_workflow_script_line "  run: bash scripts/fetch-viture-sdk.sh")"
  [[ "$r" == "deny live_invoke" ]]
  r="$(decide_workflow_host_line "curl https://shop.viture.com/x")"
  [[ "$r" == "deny viture_host" ]]
  printf 'ok\n'
}
echo "== no external commands =="
assert_result "cores run with an empty PATH" "ok" all_cores_without_path

echo
echo "$PASS passed, $FAIL failed"
[[ "$FAIL" -eq 0 ]]
