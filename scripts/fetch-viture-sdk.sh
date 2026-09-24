#!/usr/bin/env bash
# fetch-viture-sdk.sh — download Viture SDK from an official URL in sdk-manifest.toml,
# verify SHA-256, extract into gitignored Viture/. Fail closed on any doubt.
# Policy decisions live in lib/sdk-fetch-policy.sh (pure). This file is the edge:
# files, curl, checksum process, archive listing, env, and process exit.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="${SDK_MANIFEST:-$ROOT/sdk-manifest.toml}"
CACHE="${SDK_CACHE:-$ROOT/.sdk-cache}"

# shellcheck source=lib/sdk-fetch-policy.sh
source "$ROOT/scripts/lib/sdk-fetch-policy.sh"

die() { echo "ERROR: $*" >&2; exit 1; }

need_cmd() { command -v "$1" >/dev/null 2>&1 || die "missing required command: $1"; }

# Archive member paths must not escape the extract root.
# Listing the archive is an effect; the allow/deny decision is pure.
archive_paths_safe() {
  local dest="$1"
  local listing result member
  case "$dest" in
    *.zip)
      listing="$(unzip -Z -1 "$dest" 2>/dev/null || unzip -l "$dest" | awk 'NR>3 {print $4}')"
      ;;
    *.tgz|*.tar.gz|*.tar)
      listing="$(tar -tf "$dest")"
      ;;
    *)
      die "unknown archive type for $dest"
      ;;
  esac
  result="$(decide_archive_members "$listing")"
  case "$result" in
    ok) ;;
    deny\ escape\ *)
      member="${result#deny escape }"
      die "archive member escapes extract root: $member"
      ;;
    deny\ absolute\ *)
      member="${result#deny absolute }"
      die "archive member is absolute: $member"
      ;;
    *)
      die "malformed archive member result: $result"
      ;;
  esac
}

# Minimal TOML getter for flat keys under [section]: key = "value"
# File read stays here; matching is toml_lookup.
toml_get() {
  local section="$1" key="$2" file="$3"
  local text result
  text="$(<"$file")"
  result="$(toml_lookup "$section" "$key" "$text")"
  case "$result" in
    ok)
      printf '\n'
      ;;
    ok\ *)
      printf '%s\n' "${result#ok }"
      ;;
    deny\ missing)
      return 0
      ;;
    *)
      echo "ERROR: malformed toml lookup result: $result" >&2
      return 1
      ;;
  esac
}

sha256_file() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

fetch_one() {
  local label="$1" url="$2" expect_sha="$3" extract_to="$4"
  local url_result digest_result path_result ci_result got checksum_result
  echo "==> $label"

  url_result="$(decide_placeholder "$url")"
  case "$url_result" in
    ok) ;;
    deny\ *)
      die "$label: url is a placeholder/TODO — edit sdk-manifest.toml with the official portal URL"
      ;;
    *)
      die "$label: malformed placeholder result: $url_result"
      ;;
  esac

  digest_result="$(decide_digest "$expect_sha")"
  case "$digest_result" in
    ok) ;;
    deny\ bad_hex)
      die "$label: sha256 must be 64 hex chars"
      ;;
    deny\ empty|deny\ todo|deny\ replace|deny\ example_invalid)
      die "$label: sha256 is a placeholder/TODO — refuse to download without a real digest"
      ;;
    *)
      die "$label: malformed digest result: $digest_result"
      ;;
  esac

  path_result="$(decide_extract_path "$extract_to")"
  case "$path_result" in
    ok) ;;
    deny\ *)
      die "$label: extract_to='$extract_to' must be empty or under Viture/ (no .., no absolute paths)"
      ;;
    *)
      die "$label: malformed extract path result: $path_result"
      ;;
  esac

  # CI / explicit guard: never pull the proprietary SDK on GitHub Actions.
  # Env is read here and passed in; decide_ci_fetch does not see the environment.
  ci_result="$(decide_ci_fetch "${GITHUB_ACTIONS:-}" "${CI_NO_FETCH:-}")"
  case "$ci_result" in
    ok) ;;
    deny\ ci_blocked)
      die "$label: refusing to download SDK in CI (fail-closed; no network fetch)"
      ;;
    *)
      die "$label: malformed CI fetch result: $ci_result"
      ;;
  esac

  mkdir -p "$CACHE"
  local base dest
  base="$(basename "${url%%\?*}")"
  dest="$CACHE/$base"

  echo "    downloading → $dest"
  curl -fL --retry 3 --retry-delay 2 -o "$dest.partial" "$url" \
    || die "$label: download failed (fail closed)"
  mv "$dest.partial" "$dest"

  got="$(sha256_file "$dest")"
  checksum_result="$(decide_checksum_match "$got" "$expect_sha")"
  case "$checksum_result" in
    ok) ;;
    deny\ mismatch)
      rm -f "$dest"
      die "$label: checksum mismatch (got $got, expected $expect_sha) — deleted download"
      ;;
    *)
      rm -f "$dest"
      die "$label: malformed checksum result: $checksum_result"
      ;;
  esac
  echo "    checksum OK ($got)"

  if [[ -n "$extract_to" ]]; then
    local target="$ROOT/$extract_to"
    mkdir -p "$target"
    archive_paths_safe "$dest"
    echo "    extracting → $target"
    case "$dest" in
      *.zip)  need_cmd unzip; unzip -q -o "$dest" -d "$target" ;;
      *.tgz|*.tar.gz) tar -xzf "$dest" -C "$target" ;;
      *.tar)  tar -xf "$dest" -C "$target" ;;
      *) die "$label: unknown archive type for $dest" ;;
    esac
  else
    echo "    stored in cache only (no extract_to)"
  fi
}

need_cmd curl
need_cmd awk
[[ -f "$MANIFEST" ]] || die "manifest not found: $MANIFEST (copy sdk-manifest.example.toml → sdk-manifest.toml)"

sdk_url="$(toml_get sdk url "$MANIFEST")"
sdk_sha="$(toml_get sdk sha256 "$MANIFEST")"
sdk_ext="$(toml_get sdk extract_to "$MANIFEST")"
sdk_name="$(toml_get sdk name "$MANIFEST")"
[[ -n "$sdk_name" ]] || sdk_name="sdk"

fetch_one "$sdk_name" "$sdk_url" "$sdk_sha" "$sdk_ext"

# Optional unity section
unity_url="$(toml_get unity url "$MANIFEST" || true)"
if [[ -n "${unity_url:-}" ]]; then
  unity_sha="$(toml_get unity sha256 "$MANIFEST")"
  unity_ext="$(toml_get unity extract_to "$MANIFEST" || true)"
  unity_name="$(toml_get unity name "$MANIFEST")"
  [[ -n "$unity_name" ]] || unity_name="unity"
  fetch_one "$unity_name" "$unity_url" "$unity_sha" "$unity_ext"
fi

echo "Done. Remember: Viture/ and SDK archives must remain gitignored."
