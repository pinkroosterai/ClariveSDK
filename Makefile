.PHONY: build test clean build-csharp build-python build-typescript test-csharp test-python test-typescript

# --- All SDKs ---

build: build-csharp build-python build-typescript

test: test-csharp test-python test-typescript

clean:
	rm -rf sdks/csharp/bin sdks/csharp/obj sdks/csharp/src/ClariveSDK/bin sdks/csharp/src/ClariveSDK/obj
	rm -rf sdks/csharp/tests/ClariveSDK.Tests/bin sdks/csharp/tests/ClariveSDK.Tests/obj
	rm -rf sdks/csharp/example/ClariveSDK.Example/bin sdks/csharp/example/ClariveSDK.Example/obj
	rm -rf sdks/python/.venv sdks/python/dist sdks/python/src/*.egg-info
	rm -rf sdks/typescript/node_modules sdks/typescript/dist

# --- C# ---

build-csharp:
	cd sdks/csharp && dotnet build

test-csharp:
	cd sdks/csharp && dotnet test

# --- Python ---

build-python:
	cd sdks/python && uv venv --quiet 2>/dev/null; uv pip install -e ".[dev]" --quiet

test-python:
	cd sdks/python && uv run pytest

# --- TypeScript ---

build-typescript:
	cd sdks/typescript && pnpm install --frozen-lockfile 2>/dev/null || pnpm install && pnpm run build

test-typescript:
	cd sdks/typescript && pnpm test
