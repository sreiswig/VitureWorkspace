#!/usr/bin/env bash
# fetch-viture-sdk.sh — download Viture SDK from an official URL in sdk-manifest.toml,
# verify SHA-256, extract into gitignored Viture/. Fail closed on any doubt.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MANIFEST="${SDK_MANIFEST:-$ROOT/sdk-manifest.toml}"
CACHE="${SDK_CACHE:-$ROOT/.sdk-cache}"

die() { echo "ERROR: $*" >&2; exit 1; }

need_cmd() { command -v "$1" >/dev/null 2>&1 || die "missing required command: $1"; }

is_placeholder() {
  local v="$1"
  [[ -z "$v" ]] && return 0
  [[ "$v" == *TODO* ]] && return 0
  [[ "$v" == *REPLACE* ]] && return 0
  [[ "$v" == *example.invalid* ]] && return 0
  return 1
}

# extract_to must be empty (cache only) or a path under Viture/ with no traversal.
is_jailed_extract() {
  local p="$1"
  [[ -z "$p" ]] && return 0
  [[ "$p" == *..* ]] && return 1
  [[ "$p" == /* ]] && return 1
  [[ "$p" == \\* ]] && return 1
  case "$p" in
    Viture|Viture/*) return 0 ;;
    *) return 1 ;;
  esac
}

# Archive member paths must not escape the extract root.
archive_paths_safe() {
  local dest="$1"
  local listing
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
  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    [[ "$line" == *..* ]] && die "archive member escapes extract root: $line"
    [[ "$line" == /* ]] && die "archive member is absolute: $line"
  done <<< "$listing"
}

# Minimal TOML getter for flat keys under [section]: key = "value"
toml_get() {
  local section="$1" key="$2" file="$3"
  awk -v sec="$section" -v key="$key" '
    BEGIN { insec=0 }
    /^[[:space:]]*\[/ {
      insec = ($0 ~ "^[[:space:]]*\\[" sec "\\][[:space:]]*$")
      next
    }
    insec && $0 ~ "^[[:space:]]*" key "[[:space:]]*=" {
      sub(/^[^=]*=[[:space:]]*/, "")
      gsub(/^[[:space:]]+|[[:space:]]+$/, "")
      gsub(/^"|"$/, "")
      print
      exit
    }
  ' "$file"
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
  echo "==> $label"

  is_placeholder "$url" && die "$label: url is a placeholder/TODO — edit sdk-manifest.toml with the official portal URL"
  is_placeholder "$expect_sha" && die "$label: sha256 is a placeholder/TODO — refuse to download without a real digest"
  [[ "$expect_sha" =~ ^[0-9a-fA-F]{64}$ ]] || die "$label: sha256 must be 64 hex chars"
  is_jailed_extract "$extract_to" || die "$label: extract_to='$extract_to' must be empty or under Viture/ (no .., no absolute paths)"

  # CI / explicit guard: never pull the proprietary SDK on GitHub Actions.
  if [[ -n "${GITHUB_ACTIONS:-}" || "${CI_NO_FETCH:-}" == "1" ]]; then
    die "$label: refusing to download SDK in CI (fail-closed; no network fetch)"
  fi

  mkdir -p "$CACHE"
  local base dest
  base="$(basename "${url%%\?*}")"
  dest="$CACHE/$base"

  echo "    downloading → $dest"
  curl -fL --retry 3 --retry-delay 2 -o "$dest.partial" "$url" \
    || die "$label: download failed (fail closed)"
  mv "$dest.partial" "$dest"

  local got
  got="$(sha256_file "$dest")"
  if [[ "${got,,}" != "${expect_sha,,}" ]]; then
    rm -f "$dest"
    die "$label: checksum mismatch (got $got, expected $expect_sha) — deleted download"
  fi
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
