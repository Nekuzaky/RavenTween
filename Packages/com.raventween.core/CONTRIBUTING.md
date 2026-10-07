# Contributing

Thanks for taking the time to improve RavenTween. The bar for this package is "internal tool for a studio that ships": every change must keep the engine allocation-free, warning-free and fully tested.

## Ground rules

- **No steady-state allocation.** Any code that runs per frame must not allocate. Pools and fixed-size slots only.
- **No reflection, no threads, no recursion** in runtime code. Nested-structure walks use explicit work stacks with depth guards.
- **Short functions, defensive checks.** Keep functions under ~60 lines, validate inputs with `Debug.Assert` plus a graceful runtime fallback (assertions are stripped from release builds).
- **Warning-free.** The package must compile clean with the default Unity compiler settings and pass Roslyn analyzers without suppressions.
- **Tests first.** Every bug fix lands with a test that fails before the fix. Keep coverage above 90% of runtime code.

## Workflow

1. Fork and branch from `main` (`feature/<topic>` or `fix/<topic>`).
2. Make your change inside `Packages/com.raventween.core`.
3. Run the full test suite (Window → General → Test Runner, both Edit Mode and Play Mode).
4. Update `CHANGELOG.md` under an `Unreleased` heading.
5. Open a pull request with a short description of the behavior change and the tests covering it.

## Continuous integration

`.github/workflows/tests.yml` runs the Play Mode suite in package mode on Unity 2021.3, 2022.3 and 6 using GameCI. It needs three repository secrets (**Settings → Secrets and variables → Actions**):

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Contents of your `Unity_lic.ulf` file (Personal licenses work) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

See [GameCI activation](https://game.ci/docs/github/activation) for how to obtain the license file. Without these secrets the workflow skips the Unity steps and reports a notice instead of failing.

## Reporting bugs

Open an issue with: Unity version, package version, a minimal reproduction (ideally a single script), expected vs. actual behavior. Crashes and GC regressions are treated as release blockers.

## Release process (maintainers)

1. Move `Unreleased` notes to a new version heading in `CHANGELOG.md`.
2. Bump `version` in `package.json` (semver: breaking / feature / fix).
3. Tag the commit `vX.Y.Z` so Git URL installs can pin it.
