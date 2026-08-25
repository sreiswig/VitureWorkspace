# Agent notes — VitureWorkspace

Glasses-driven homelab workspace. Do not invent APIs, Flutter apps, or new product surface.

## Viture SDK (local, gitignored)

Official SDK is not in git. Fetch locally:

1. Copy `sdk-manifest.example.toml` → `sdk-manifest.toml`
2. Fill the official portal URL and SHA-256 — see [docs/sdk-setup.md](docs/sdk-setup.md)
3. Run `scripts/fetch-viture-sdk.sh`

Never commit:

- SDK zips / archives
- a filled `sdk-manifest.toml`
- `.sdk-cache/`
- extracted `Viture/` SDK blobs / new files under `Viture/`

`/Viture/` is gitignored for new drops; do not purge the existing tracked `Viture/` tree (no history rewrite).

Do not commit portal URLs, tokens, or real SHA-256 checksums of SDK zips. `sdk-manifest.example.toml` stays placeholders (`TODO` / `example.invalid`).

## Tests and CI

- **Do not download the proprietary Viture SDK in CI.** Tests are fail-closed (placeholder URLs, extract_to jail). Set `CI_NO_FETCH=1` in GitHub Actions.
- `extract_to` is jailed under `Viture/` (no `..`, no absolute paths).
- Free-tier GitHub Actions only (`ubuntu-latest`, SHA-pinned actions). No paid GHAS, billed runners, or Copilot-for-PRs.

```bash
bash -n scripts/fetch-viture-sdk.sh
tests/test-fetch-viture-sdk.sh
```
