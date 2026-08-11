# Viture SDK setup

Sam-authored samples (`hello_viture/`, `hello_flutter/`, devenv) expect the
**official** Viture SDK to be present under a **gitignored** `Viture/` directory.
Do not commit SDK zips or extracted trees.

## Official download

1. Open Viture’s developer portal: <https://www.viture.com/developer>
2. Sign in / accept Viture terms as required.
3. Download the SDK package appropriate for your platform/toolchain.
4. Prefer the scripted path: copy `sdk-manifest.example.toml` → `sdk-manifest.toml`,
   fill in the real URL + SHA-256, then run `scripts/fetch-viture-sdk.sh`.

## Version notes (as of security draft 2026-08-11)

| Component | Version / note |
|-----------|----------------|
| Unity package | `com.viture.xr` **0.5.0** (confirm current on the portal before pinning) |
| Native / device SDK | Fill exact product name + version from the portal download page |
| Manifest | Local `sdk-manifest.toml` (gitignored if it contains non-public URLs) |

Re-check versions on the portal; bump manifest checksums when you upgrade.

## Checksums (placeholders — replace before use)

```text
# TODO: replace with real SHA-256 of the downloaded artifact(s)
viture-sdk-TODO.zip  sha256:REPLACE_WITH_REAL_SHA256_OF_DOWNLOADED_ZIP
com.viture.xr-0.5.0.tgz  sha256:REPLACE_WITH_REAL_SHA256_OF_UNITY_PACKAGE
```

Compute locally after download:

```bash
sha256sum path/to/downloaded.zip
# or: shasum -a 256 path/to/downloaded.zip
```

## Expected extract layout for `hello_viture`

After a successful fetch/extract (exact names depend on the official archive):

```text
Viture/                          # gitignored
  README_OR_LICENSE_FROM_VITURE
  include/                       # or Headers/ — per upstream layout
  lib/                           # or prebuilt binaries
  # …upstream SDK tree…
hello_viture/                    # Sam-authored sample (in git)
  … points at ../Viture via devenv / build flags …
```

If upstream uses a different top-level folder name, either:

- extract so the usable root is `Viture/`, or
- adjust the sample’s include/lib paths and document the mapping here.

## Fail-closed policy

- Missing manifest, URL, or checksum → **do not** download.
- Checksum mismatch → **delete** the partial download and exit non-zero.
- Never commit `Viture/`, `*.zip` / SDK archives, or filled manifests with secrets.
