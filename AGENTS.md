# Agent notes

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
- extracted `Viture/` SDK blobs

`/Viture/` is gitignored for new drops; do not purge the existing tracked `Viture/` tree.

Do not add `.github/workflows` (DevOps owns CI).

## Tests (no network)

```bash
bash -n scripts/fetch-viture-sdk.sh
scripts/test-fetch-viture-sdk.sh
```
