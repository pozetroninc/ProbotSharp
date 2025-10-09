# kubeconform Helper Assets

This folder stores support files for the `kubeconform` validation that runs as part of the Husky.Net pre-commit hook.

- `schemas/master-standalone-strict`: pre-downloaded JSON schemas for the Kubernetes resources used in this repository. Keeping them locally allows validation to run without network access.
- `kubeconform` / `kubeconform.exe`: cached binaries downloaded on demand by `scripts/run-kubeconform.*`. These binaries are ignored via `.gitignore` so that each developer can manage their own copy.

If you run into issues with validation you can manually download a newer `kubeconform` release and replace the cached binary. Update the helper scripts if the CLI interface changes.
