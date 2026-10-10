# Contributing

Changes to `main` go through a pull request. The branch must be up to date with `main`, all required checks must pass, and review conversations must be resolved. An approving human review is optional. These protections apply to administrators too. Force pushes and deletion of `main` are blocked; verified commit signatures are optional.

The required checks are:

- **Catalog and tracker checks** requires the check-tool build and every catalog, generation, evolution reproducibility, and tracker category to pass.
- **JavaScript syntax** checks application and verification scripts and executes appearance preference tests.
- **Release publish** requires the optimized release build, offline asset verification, and every browser category to pass.

Keep these job names stable. GitHub branch protection accepts them only from GitHub Actions. Catalog and release checks are aggregate gates; their category jobs identify the failing subsystem. The gates run after upstream failures and reject failed, cancelled, missing, or unexpectedly skipped jobs. Pages deployment waits for all three checks and runs only on `main`.

The check-tool build produces one `check-tools` artifact containing runnable .NET outputs and their dependencies. Four jobs consume it without building or restoring: Catalog data and rules, Catalog generation fixtures, Evolution reproducibility, and Tracker behavior. The optimized release build produces one `prepared-site` artifact with the Pages base path and verified offline hashes. Five browser jobs consume it: Interface, Appearance, Sources and evolutions, Storage and sync, and Navigation and offline. Matrix jobs continue independently when another category fails. Deployment downloads the same prepared artifact that the browser jobs tested.

Changes limited to root Markdown files or Markdown under `docs/` skip application builds, tests, and deployment. Change scope validates changed-file whitespace. All three required checks still complete successfully for deliberate skips. Unknown paths, shipped app content, CI changes, and moves from executable files into documentation run all checks. Manual runs always run all checks.

NuGet packages and pinned PokéAPI evolution inputs use dependency caches. Runnable tools and the prepared website use artifacts retained for three days. Category logs and JSON results, build binary logs, and available browser failure screenshots and page text are retained for seven days. Each executed check also writes its result and diagnostic output to the Actions job summary.

See [verification and publishing instructions](README.md#verify-and-publish) for local commands. CI uses Chrome with a disposable profile. The browser scripts can also run in a disposable preview origin. They exercise real Blazor controls, IndexedDB, and OPFS; they replace only the native folder picker and deliberately failed storage transactions.

Run `node tools/javascript-checks.mjs` to check JavaScript syntax, preference behavior, CI scope and gate behavior, result reporting, and browser-category coverage. Select a .NET category with `dotnet run --project tools/Checks -c Release -- . --category catalog`, `generation`, or `tracker`. Omit `--category` to run every category. Evolution reproducibility remains `dotnet run --project tools/CatalogGenerator -c Release -- . --verify-evolutions`. Select a browser category with `node tools/browser-ci.mjs artifacts/publish/wwwroot /pokedex-tracker/ interface`, `appearance`, `sources`, `storage`, or `navigation`. Omit the final argument to run the full suite.
