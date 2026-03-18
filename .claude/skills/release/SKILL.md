---
name: release
description: >-
  Release SDK packages to NuGet, PyPI, and npm. Bumps versions, updates READMEs
  if needed, builds, tests, and publishes. Use when the user says "release",
  "publish", "bump version", "push to registry", or "new version".
argument-hint: "<version> [csharp|python|typescript...]"
disable-model-invocation: true
---

# Release SDKs

Publish one or more SDK packages to their registries (NuGet, PyPI, npm).

## Arguments

Parse `$ARGUMENTS` for:
- **First argument** (`$0`): the semantic version to release (e.g. `0.2.0`). Required.
- **Remaining arguments**: which SDKs to release — `csharp`, `python`, `typescript`.
  If none specified, release all three.

If no version is provided, stop immediately:
"Usage: `/release <version> [csharp|python|typescript...]`"

## Step 1: Pre-flight Checks

Before doing anything, verify the repo is in a clean state:

```bash
cd /home/najgeetsrev/ClariveSDK
```

1. **Check for staged files.** Run `git diff --cached --name-only`. If there is ANY
   output (staged files exist), stop immediately and report:
   "There are staged files in the working tree. Commit or unstage them before releasing."

2. **Check for uncommitted changes.** Run `git status --porcelain`. If there is output,
   warn the user but don't stop — version bumps will create new changes anyway.

3. **Check branch.** Run `git branch --show-current`. Warn if not on `main`.

4. **Check environment variables.** For each SDK being released, verify the
   corresponding API key env var is set:
   - C#: `NUGET_API_KEY`
   - Python: `PIPY_API_KEY` (note: this is the env var name, not a typo)
   - TypeScript: `NPM_API_KEY`

   If any required key is missing, stop and report which one.

## Step 2: Run Tests

Run the full test suite for the SDKs being released:

```bash
make test-csharp    # if releasing C#
make test-python    # if releasing Python
make test-typescript  # if releasing TypeScript
```

If any tests fail, stop immediately. Do not proceed to version bumping.

## Step 3: Version Bump

Update the version string in all relevant files for each SDK being released.
The version argument from `$0` is the new version (e.g. `0.2.0`).

**C#:**
- `sdks/csharp/src/ClariveSDK/ClariveSDK.csproj` — update `<Version>X.Y.Z</Version>`

**Python:**
- `sdks/python/pyproject.toml` — update `version = "X.Y.Z"`
- `sdks/python/src/clarive/__init__.py` — update `__version__ = "X.Y.Z"`

**TypeScript:**
- `sdks/typescript/package.json` — update `"version": "X.Y.Z"`

Use the Edit tool for each file. Do NOT use sed — use precise string replacement.

## Step 4: Check README Files

For each SDK being released, read the README and check:

1. Does the install command still work? (package name hasn't changed)
2. Does the badge URL point to the correct registry?
3. Are there any version-specific references that need updating?

If any README needs changes, make them now.

Also check the root `README.md` — if it references specific versions, update those too.

## Step 5: Build

Build each SDK being released:

**C#:**
```bash
cd /home/najgeetsrev/ClariveSDK/sdks/csharp
dotnet pack src/ClariveSDK/ClariveSDK.csproj -c Release
```

**Python:**
```bash
cd /home/najgeetsrev/ClariveSDK/sdks/python
rm -rf dist/
uv run python -m build
```

**TypeScript:**
```bash
cd /home/najgeetsrev/ClariveSDK/sdks/typescript
pnpm run build
```

If any build fails, stop and report the error.

## Step 6: Publish

Push each SDK to its registry:

**C# → NuGet:**
```bash
dotnet nuget push sdks/csharp/src/ClariveSDK/bin/Release/ClariveSDK.{version}.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

**Python → PyPI:**
```bash
cd sdks/python
uv run twine upload dist/* --username __token__ --password "$PIPY_API_KEY"
```

**TypeScript → npm:**
```bash
cd sdks/typescript
echo "//registry.npmjs.org/:_authToken=${NPM_API_KEY}" > .npmrc
npm publish --access public
rm -f .npmrc
```

If any publish fails, stop and report. Do NOT continue to the next SDK —
the user needs to investigate before retrying.

## Step 7: Commit and Tag

After all selected SDKs are published:

1. Stage all changed files:
   ```bash
   git add -A
   ```

2. Create a single commit:
   ```
   chore: release v{version} ({list of SDKs released})

   Published:
   - ClariveSDK {version} → NuGet       (if C# was released)
   - clarive-sdk {version} → PyPI       (if Python was released)
   - clarive-sdk {version} → npm        (if TypeScript was released)
   ```

3. Create a git tag:
   ```bash
   git tag v{version}
   ```

4. Push commit and tag:
   ```bash
   git push origin main
   git push origin v{version}
   ```

## Step 8: Report

Print a summary:

```
## Release Complete: v{version}

| SDK | Registry | Package | Status |
|-----|----------|---------|--------|
| C# | NuGet | ClariveSDK {version} | Published |
| Python | PyPI | clarive-sdk {version} | Published |
| TypeScript | npm | clarive-sdk {version} | Published |

Commit: {short hash}
Tag: v{version}
```

## Constraints

- NEVER publish if tests fail
- NEVER publish if there are staged git files at the start
- NEVER leave `.npmrc` files with tokens after publishing
- If a publish fails partway through, report exactly what succeeded and what failed
- Always clean up build artifacts (Python dist/, .npmrc)
- The version bump commit should be the ONLY uncommitted change when publishing
