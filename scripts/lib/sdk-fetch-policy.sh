# sdk-fetch-policy.sh — pure fail-closed decisions for the Viture SDK fetch.
#
# No I/O, process, env, clock, or randomness. Callers pass every input.
# Each function prints exactly one immutable result line and returns 0:
#   ok
#   ok <payload>
#   deny <code>
#   deny <code> <payload>
# Deny is a value, not a bare non-zero exit. The edge maps codes to exits.

# Trim leading and trailing ASCII whitespace (space, tab, CR, LF, VT, FF).
_sfp_trim() {
  local s="$1"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

# deny empty | deny todo | deny replace | deny example_invalid | ok
decide_placeholder() {
  local -r value="$1"
  if [[ -z "$value" ]]; then
    printf 'deny empty\n'
    return 0
  fi
  if [[ "$value" == *TODO* ]]; then
    printf 'deny todo\n'
    return 0
  fi
  if [[ "$value" == *REPLACE* ]]; then
    printf 'deny replace\n'
    return 0
  fi
  if [[ "$value" == *example.invalid* ]]; then
    printf 'deny example_invalid\n'
    return 0
  fi
  printf 'ok\n'
  return 0
}

# Placeholder check, then exactly 64 hex chars.
# deny <placeholder code> | deny bad_hex | ok
decide_digest() {
  local -r digest="$1"
  local -r placeholder="$(decide_placeholder "$digest")"
  case "$placeholder" in
    ok) ;;
    deny\ *)
      printf '%s\n' "$placeholder"
      return 0
      ;;
    *)
      printf 'deny malformed\n'
      return 0
      ;;
  esac
  if [[ "$digest" =~ ^[0-9a-fA-F]{64}$ ]]; then
    printf 'ok\n'
  else
    printf 'deny bad_hex\n'
  fi
  return 0
}

# Case-insensitive equality of two digests. Format is decide_digest's job.
# ok | deny mismatch
decide_checksum_match() {
  local -r got="$1"
  local -r expected="$2"
  local -r got_folded="${got,,}"
  local -r expected_folded="${expected,,}"
  if [[ "$got_folded" == "$expected_folded" ]]; then
    printf 'ok\n'
  else
    printf 'deny mismatch\n'
  fi
  return 0
}

# Empty path: cache only. Otherwise Viture or Viture/... with no traversal
# and no absolute (slash or backslash) path.
# ok | deny traversal | deny absolute | deny absolute_backslash | deny outside_viture
decide_extract_path() {
  local -r path="$1"
  if [[ -z "$path" ]]; then
    printf 'ok\n'
    return 0
  fi
  if [[ "$path" == *..* ]]; then
    printf 'deny traversal\n'
    return 0
  fi
  if [[ "$path" == /* ]]; then
    printf 'deny absolute\n'
    return 0
  fi
  if [[ "$path" == \\* ]]; then
    printf 'deny absolute_backslash\n'
    return 0
  fi
  case "$path" in
    Viture|Viture/*)
      printf 'ok\n'
      ;;
    *)
      printf 'deny outside_viture\n'
      ;;
  esac
  return 0
}

# Newline-separated archive member names. Empty lines are skipped.
# ok | deny escape <member> | deny absolute <member>
decide_archive_members() {
  local -r listing="$1"
  local line
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ -z "$line" ]] && continue
    if [[ "$line" == *..* ]]; then
      printf 'deny escape %s\n' "$line"
      return 0
    fi
    if [[ "$line" == /* ]]; then
      printf 'deny absolute %s\n' "$line"
      return 0
    fi
  done <<< "$listing"
  printf 'ok\n'
  return 0
}

# Explicit inputs — this function does not read GITHUB_ACTIONS or CI_NO_FETCH.
# ok | deny ci_blocked
decide_ci_fetch() {
  local -r github_actions="$1"
  local -r ci_no_fetch="$2"
  if [[ -n "$github_actions" || "$ci_no_fetch" == "1" ]]; then
    printf 'deny ci_blocked\n'
    return 0
  fi
  printf 'ok\n'
  return 0
}

# First flat key under [section]. Same rules as the previous awk getter:
# section header is optional whitespace, [section], optional whitespace;
# any other '[' line leaves the section; value strips one layer of
# surrounding ASCII whitespace and one leading and/or trailing double quote.
# ok <value> | deny missing
toml_lookup() {
  local -r section="$1"
  local -r key="$2"
  local -r text="$3"
  local in_section=0
  local line ltrimmed header after value
  while IFS= read -r line || [[ -n "$line" ]]; do
    ltrimmed="$(_sfp_trim "$line")"
    if [[ "$ltrimmed" == \[* ]]; then
      header="$ltrimmed"
      if [[ "$header" == "[${section}]" ]]; then
        in_section=1
      else
        in_section=0
      fi
      continue
    fi
    [[ "$in_section" -eq 1 ]] || continue
    [[ "$ltrimmed" == "$key"* ]] || continue
    after="${ltrimmed#"$key"}"
    [[ "$after" =~ ^[[:space:]]*= ]] || continue
    value="${ltrimmed#*=}"
    value="$(_sfp_trim "$value")"
    [[ "$value" == \"* ]] && value="${value#\"}"
    [[ "$value" == *\" ]] && value="${value%\"}"
    if [[ -z "$value" ]]; then
      printf 'ok\n'
    else
      printf 'ok %s\n' "$value"
    fi
    return 0
  done <<< "$text"
  printf 'deny missing\n'
  return 0
}

# One workflow line (no filename prefix). Same fail-closed rules as the
# historical grep: a live scripts/fetch-viture-sdk.sh invocation is deny,
# an end-anchored `bash -n` of that script is syntax-only, and a
# test-fetch-viture mention is not a live download.
# ok absent | ok syntax_only | ok test_reference | deny live_invoke
decide_workflow_script_line() {
  local -r line="$1"
  if [[ ! "$line" =~ scripts/fetch-viture-sdk\.sh ]]; then
    printf 'ok absent\n'
    return 0
  fi
  if [[ "$line" == *test-fetch-viture* ]]; then
    printf 'ok test_reference\n'
    return 0
  fi
  if [[ "$line" =~ [[:space:]]bash[[:space:]]+-n[[:space:]]+scripts/fetch-viture-sdk\.sh[[:space:]]*$ ]]; then
    printf 'ok syntax_only\n'
    return 0
  fi
  printf 'deny live_invoke\n'
  return 0
}

# Case-insensitive. ok | deny viture_host
decide_workflow_host_line() {
  local -r line="$1"
  local -r folded="${line,,}"
  if [[ "$folded" =~ curl[[:space:]].*viture ]] \
    || [[ "$folded" =~ wget[[:space:]].*viture ]] \
    || [[ "$folded" =~ shop\.viture\.com ]]; then
    printf 'deny viture_host\n'
    return 0
  fi
  printf 'ok\n'
  return 0
}
