---
applyTo: ".github/workflows/*.yml"
---

# Workflows

- Every job that builds, packs or tests runs on **macos-15** and selects its toolchain through `./.github/actions/select-xcode`; only the version, publish, guard and drift jobs run on `ubuntu-latest`.
- `build.yml` is the reusable pipeline. Its `verify` input gates package validation, the sample matrix and the host smoke tests: pull requests leave it `true`, releases pass `false` because the tagged commit was already verified on its pull request. Keep the input's name and meaning identical to the sibling repositories.
- Publishing uses nuget.org **trusted publishing**: `environment: nuget.org`, `permissions: id-token: write`, `NuGet/login@v1` with `secrets.NUGET_USER`, and the login step immediately before `dotnet nuget push` — the issued key lives one hour and each OIDC token is exchangeable once. There is no API-key secret; do not add one.
- Forked pull requests get no OIDC token, so keep the `head.repo.full_name == github.repository` condition on the publish job — forks must still build and test.
- Do not remove or weaken `release.yml`'s `guard` job: it proves the tagged commit is an ancestor of the default branch, which is the only thing that makes `verify: false` safe.
- `auto-release.yml` tags only release notes **added** by a push to `main` (`--diff-filter=A`), and starts `release.yml` with `workflow_dispatch` because a tag pushed with `GITHUB_TOKEN` does not trigger `on: push: tags` — keep both triggers on `release.yml`.
- Keep the xcframework cache key tied to the resolved native version and `build/FetchXcFrameworks.sh`, or a build of an older line restores the wrong frameworks.
- Each SDK band's `macos` workload is installed from a scratch directory pinned by its own `global.json`; the repository `global.json` pins .NET 9, so a net10 leg without that scratch pin silently uses the wrong SDK.
- Watch a new upstream component by adding a row to `build/upstream.tsv`, not by editing `upstream-drift.yml` or `build/check-upstream.sh`.
