import { ClariveClient } from "clarive-sdk";
import type { GenerateRequest } from "clarive-sdk";

const client = new ClariveClient({
  apiKey: "cl_your_api_key_here",
  baseUrl: "https://demo.clarive.app",
});

// List published entries with filtering and pagination
console.log("=== List Entries ===");
const list = await client.listEntries({ pageSize: 5, sortBy: "recent" });
console.log(`Total: ${list.totalCount} (page ${list.page}, ${list.items.length} shown)`);

for (const summary of list.items) {
  const tags = summary.tags.length > 0 ? summary.tags.join(", ") : "none";
  console.log(`  ${summary.title} (v${summary.version}) — ${summary.promptCount} prompt(s), tags: [${tags}]`);
}

// List all tags
console.log("\n=== List Tags ===");
const tags = await client.listTags();
for (const tag of tags) {
  console.log(`  ${tag.name}: ${tag.entryCount} entries`);
}

// Get a single entry (use first from list, or a known ID)
const entryId = list.items.length > 0 ? list.items[0].id : "3fa85f64-5717-4562-b3fc-2c963f66afa6";

console.log("\n=== Get Entry ===");
const entry = await client.getEntry(entryId);
console.log(`Title: ${entry.title}`);
console.log(`Version: ${entry.version}`);
console.log(`System Message: ${entry.systemMessage ?? "(none)"}`);
console.log(`Tags: [${entry.tags.join(", ")}]`);
console.log(`Updated: ${entry.updatedAt}`);
console.log(`Published: ${entry.publishedAt ?? "(not published)"}`);
console.log(`Prompts: ${entry.prompts.length}`);

for (const prompt of entry.prompts) {
  const label = prompt.isTemplate ? "[template]" : "[static]";
  console.log(`\n  Prompt #${prompt.order}: ${label}`);
  console.log(`  Content: ${prompt.content.slice(0, 80)}...`);

  if (prompt.templateFields) {
    for (const field of prompt.templateFields) {
      console.log(`    Field: ${field.name} (${field.type})`);
    }
  }
}

// Generate rendered prompts with template variable values
console.log("\n=== Generate ===");
const request: GenerateRequest = {
  fields: {
    companyName: "Acme Corp",
    customerMessage: "I need help with my order #12345",
  },
};

const result = await client.generate(entryId, request);
console.log(`Title: ${result.title}`);
console.log(`System Message: ${result.systemMessage ?? "(none)"}`);

for (const rendered of result.renderedPrompts) {
  console.log(`\n  Rendered Prompt #${rendered.order}:`);
  console.log(`  ${rendered.content}`);
}

// List tabs for the entry
console.log("\n=== List Tabs ===");
const tabsList = await client.listTabs(entryId);
console.log(`Tabs: ${tabsList.length}`);
for (const tab of tabsList) {
  console.log(`  ${tab.name} (main: ${tab.isMainTab}, forked from: ${tab.forkedFromVersion ?? "n/a"})`);
}

// Get a specific tab and generate from it
if (tabsList.length > 0) {
  const tabId = tabsList[0].id;
  console.log(`\n=== Get Tab: ${tabsList[0].name} ===`);
  const tabEntry = await client.getTab(entryId, tabId);
  console.log(`Title: ${tabEntry.title}, Version: ${tabEntry.version}, Prompts: ${tabEntry.prompts.length}`);

  console.log("\n=== Generate from Tab ===");
  const tabResult = await client.generateTab(entryId, tabId, request);
  for (const rendered of tabResult.renderedPrompts) {
    console.log(`  Rendered: ${rendered.content.slice(0, 80)}...`);
  }
}
