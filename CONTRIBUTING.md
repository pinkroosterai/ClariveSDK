# Contributing to ClariveSDK

Thanks for your interest in contributing. Here's how to get started.

## Setup

Each SDK has its own toolchain. Pick the one you're working on:

```bash
# C# (.NET 9+)
cd sdks/csharp && dotnet build && dotnet test

# Python (3.10+, uv recommended)
cd sdks/python && uv venv && uv pip install -e ".[dev]" && uv run pytest

# TypeScript (Node 18+, pnpm)
cd sdks/typescript && pnpm install && pnpm run build && pnpm test
```

Or build and test everything at once:

```bash
make build
make test
```

## Code style

Each SDK enforces its own style rules:

- **C#**: Standard .NET naming (PascalCase types/methods, camelCase locals). Build warnings are errors.
- **Python**: Enforced by `ruff` (lint + format) and `mypy --strict`. Run `uv run ruff check src/ tests/` and `uv run mypy --strict src/` before committing.
- **TypeScript**: Enforced by `biome` (lint + format) and `tsc --noEmit`. Run `pnpm run check` before committing.

Python and TypeScript have pre-commit hooks configured (pre-commit and lefthook respectively) that run these checks automatically.

## Making changes

1. Fork the repository and create a branch from `main`.
2. Make your changes in the appropriate SDK directory.
3. Add or update tests. All SDKs mock HTTP responses in unit tests — never call the real API.
4. Run the full test suite for the SDK you changed.
5. Open a pull request against `main`.

## Guidelines

- **Keep SDKs independent.** Each SDK should be idiomatic to its language. Don't port patterns across languages just for consistency.
- **No new runtime dependencies** without discussion. The TypeScript SDK has zero runtime deps; the Python SDK uses only httpx and tenacity; the C# SDK uses only Microsoft.Extensions packages.
- **Models must match the API.** All SDK model types should reflect the response shapes documented in `CLARIVE_PUBLIC_API.md`.
- **Tests are required.** No PR will be merged without test coverage for the changes.

## Reporting issues

Open a GitHub issue. Include which SDK is affected and the version you're using.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
