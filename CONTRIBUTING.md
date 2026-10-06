# Contributing

Changes to `main` go through a pull request. The branch must be up to date with `main`, all required checks must pass, and review conversations must be resolved. An approving human review is optional. These protections apply to administrators too. Force pushes and deletion of `main` are blocked; verified commit signatures are optional.

The required checks are:

- **Catalog and tracker checks** runs catalog, extraction-fixture, synchronization-decision, and tracker-store behavior checks.
- **JavaScript syntax** checks application and verification scripts and executes appearance preference tests.
- **Release publish** builds the optimized Blazor application, prepares the GitHub Pages path, verifies every offline asset hash, and executes browser behavior and offline checks.

Keep these job names stable. GitHub branch protection accepts them only from GitHub Actions. Pages deployment waits for all three checks and runs only on `main`.

See [verification and publishing instructions](README.md#verify-and-publish) for local commands. CI uses Chrome with a disposable profile. The browser scripts can also run in a disposable preview origin. They exercise real Blazor controls, IndexedDB, and OPFS; they replace only the native folder picker and deliberately failed storage transactions.
