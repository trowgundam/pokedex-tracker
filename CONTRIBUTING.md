# Contributing

Changes to `main` go through a pull request. The branch must be up to date with `main`, all required checks must pass, and review conversations must be resolved. An approving human review is optional. These protections apply to administrators too. Force pushes and deletion of `main` are blocked; verified commit signatures are optional.

The required checks are:

- **Catalog and tracker checks** compiles the catalog generator and runs the catalog and tracker behavior checks.
- **JavaScript syntax** checks application scripts and browser verification scripts.
- **Release publish** builds the optimized Blazor application, prepares the GitHub Pages path, and verifies every offline asset hash.

Keep these job names stable. GitHub branch protection accepts them only from GitHub Actions. Pages deployment waits for all three checks and runs only on `main`.

See [verification and publishing instructions](README.md#verify-and-publish) for local commands. Browser verification scripts require a disposable preview origin and are run separately from CI; the JavaScript syntax check does not execute them.
