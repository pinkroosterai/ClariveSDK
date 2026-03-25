"""Example usage of the Clarive Python SDK.

Demonstrates both async and sync client usage, including
list entries, list tags, get entry, and generate.
"""

import asyncio
from uuid import UUID

from clarive import (
    ClariveClient,
    ClariveClientSync,
    GenerateRequest,
    ListEntriesOptions,
)


async def async_example() -> None:
    """Async client usage with 'async with' context manager."""
    print("=== Async Client ===")

    async with ClariveClient(
        api_key="cl_your_api_key_here",
        base_url="https://demo.clarive.app",
    ) as client:
        # List published entries with filtering and pagination
        print("\n--- List Entries ---")
        entries = await client.list_entries(ListEntriesOptions(page_size=5, sort_by="recent"))
        print(f"Total: {entries.total_count} (page {entries.page}, {len(entries.items)} shown)")
        for summary in entries.items:
            tags = ", ".join(summary.tags) if summary.tags else "none"
            print(
                f"  {summary.title} (v{summary.version})"
                f" — {summary.prompt_count} prompt(s), tags: [{tags}]"
            )

        # List all tags
        print("\n--- List Tags ---")
        tags_list = await client.list_tags()
        for tag in tags_list:
            print(f"  {tag.name}: {tag.entry_count} entries")

        # Get a single entry (use first from list, or a known ID)
        entry_id = (
            entries.items[0].id if entries.items else UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        )

        print("\n--- Get Entry ---")
        entry = await client.get_entry(entry_id)
        print(f"Title: {entry.title}")
        print(f"Version: {entry.version}")
        print(f"System Message: {entry.system_message or '(none)'}")
        print(f"Tags: [{', '.join(entry.tags)}]")
        print(f"Updated: {entry.updated_at}")
        print(f"Published: {entry.published_at or '(not published)'}")
        print(f"Prompts: {len(entry.prompts)}")

        for prompt in entry.prompts:
            label = "[template]" if prompt.is_template else "[static]"
            print(f"\n  Prompt #{prompt.order}: {label}")
            print(f"  Content: {prompt.content[:80]}...")

            if prompt.template_fields:
                for field in prompt.template_fields:
                    print(f"    Field: {field.name} ({field.type})")

        # Generate rendered prompts with template variable values
        print("\n--- Generate ---")
        request = GenerateRequest(
            fields={
                "companyName": "Acme Corp",
                "customerMessage": "I need help with my order #12345",
            }
        )
        result = await client.generate(entry_id, request)
        print(f"Title: {result.title}")
        print(f"System Message: {result.system_message or '(none)'}")

        for rendered in result.rendered_prompts:
            print(f"\n  Rendered Prompt #{rendered.order}:")
            print(f"  {rendered.content}")

        # List tabs for the entry
        print("\n--- List Tabs ---")
        tabs_list = await client.list_tabs(entry_id)
        print(f"Tabs: {len(tabs_list)}")
        for tab in tabs_list:
            forked = tab.forked_from_version or "n/a"
            print(f"  {tab.name} (main: {tab.is_main_tab}, forked from: {forked})")

        # Get a specific tab and generate from it
        if tabs_list:
            tab_id = tabs_list[0].id
            print(f"\n--- Get Tab: {tabs_list[0].name} ---")
            tab_entry = await client.get_tab(entry_id, tab_id)
            print(f"Title: {tab_entry.title}, Version: {tab_entry.version}")

            print("\n--- Generate from Tab ---")
            tab_result = await client.generate_tab(entry_id, tab_id, request)
            for rendered in tab_result.rendered_prompts:
                print(f"  Rendered: {rendered.content[:80]}...")


def sync_example() -> None:
    """Sync client usage with 'with' context manager."""
    print("\n\n=== Sync Client ===")

    with ClariveClientSync(
        api_key="cl_your_api_key_here",
        base_url="https://demo.clarive.app",
    ) as client:
        # List entries
        entries = client.list_entries(ListEntriesOptions(page_size=3))
        print(f"Total: {entries.total_count} entries")
        for s in entries.items:
            print(f"  {s.title} (v{s.version})")

        # List tags
        tags = client.list_tags()
        print(f"Tags: {', '.join(t.name for t in tags)}")

        # Get + generate
        if entries.items:
            entry = client.get_entry(entries.items[0].id)
            print(f"\nEntry: {entry.title}, tags: [{', '.join(entry.tags)}]")

            result = client.generate(
                entry.id,
                GenerateRequest(fields={"companyName": "Acme Corp"}),
            )
            for rendered in result.rendered_prompts:
                print(f"  Rendered: {rendered.content[:80]}...")


if __name__ == "__main__":
    asyncio.run(async_example())
    sync_example()
