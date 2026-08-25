# Agent notes — VitureWorkspace

Security constraints for anyone changing this repo (including CI):

- **Do not download the proprietary Viture SDK in CI.** Tests must be fail-closed (placeholder URLs, jail checks). Set `CI_NO_FETCH=1` in GitHub Actions.
- **Do not commit** `sdk-manifest.toml`, new files under `Viture/`, or `.sdk-cache/`. Those paths are gitignored. Existing tracked `Viture/` blobs stay until Sam approves a purge — do not history-rewrite them here.
- **Do not commit portal URLs, tokens, or real SHA-256 checksums** of SDK zips. `sdk-manifest.example.toml` stays placeholders (`TODO` / `example.invalid`).
- `extract_to` is jailed under `Viture/` (no `..`, no absolute paths).
- Free-tier GitHub Actions only (`ubuntu-latest`, SHA-pinned actions). No paid GHAS, billed runners, or Copilot-for-PRs.
