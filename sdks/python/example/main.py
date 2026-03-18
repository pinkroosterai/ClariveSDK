"""Example usage of the Clarive Python SDK.

Demonstrates both async and sync client usage.
"""

import asyncio
from uuid import UUID

from clarive import (
    ClariveClient,
    ClariveClientSync,
    GenerateRequest,
)


# Replace with a real entry ID from your Clarive instance
ENTRY_ID = UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")


async def async_example() -> None:
    """Async client usage with 'async with' context manager."""
    print("=== Async Client ===")

    async with ClariveClient(
        api_key="cl_your_api_key_here",
        base_url="https://demo.clarive.app",
    ) as client:
        # Retrieve a published prompt entry
        entry = await client.get_entry(ENTRY_ID)
        print(f"Title: {entry.title}")
        print(f"Version: {entry.version}")
        print(f"System Message: {entry.system_message or '(none)'}")
        print(f"Prompts: {len(entry.prompts)}")

        for prompt in entry.prompts:
            print(f"\n  Prompt #{prompt.order}: {'[template]' if prompt.is_template else '[static]'}")
            print(f"  Content: {prompt.content[:80]}...")

            if prompt.template_fields:
                for field in prompt.template_fields:
                    print(f"    Field: {field.name} ({field.type})")

        # Generate rendered prompts with template variable values
        print("\n=== Generate ===")
        request = GenerateRequest(
            fields={
                "companyName": "Acme Corp",
                "customerMessage": "I need help with my order #12345",
            }
        )
        result = await client.generate(ENTRY_ID, request)
        print(f"Title: {result.title}")
        print(f"System Message: {result.system_message or '(none)'}")

        for rendered in result.rendered_prompts:
            print(f"\n  Rendered Prompt #{rendered.order}:")
            print(f"  {rendered.content}")


def sync_example() -> None:
    """Sync client usage with 'with' context manager."""
    print("\n=== Sync Client ===")

    with ClariveClientSync(
        api_key="cl_your_api_key_here",
        base_url="https://demo.clarive.app",
    ) as client:
        entry = client.get_entry(ENTRY_ID)
        print(f"Title: {entry.title}")
        print(f"Version: {entry.version}")
        print(f"Prompts: {len(entry.prompts)}")

        result = client.generate(
            ENTRY_ID,
            GenerateRequest(fields={"companyName": "Acme Corp"}),
        )
        for rendered in result.rendered_prompts:
            print(f"  Rendered: {rendered.content}")


if __name__ == "__main__":
    asyncio.run(async_example())
    sync_example()
