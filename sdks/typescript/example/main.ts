import { ClariveClient } from "../src/index.js";
import type { GenerateRequest } from "../src/index.js";

// Replace with a real entry ID from your Clarive instance
const entryId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

const client = new ClariveClient({
  apiKey: "cl_your_api_key_here",
  baseUrl: "https://demo.clarive.app",
});

// Retrieve a published prompt entry
console.log("=== Get Entry ===");
const entry = await client.getEntry(entryId);
console.log(`Title: ${entry.title}`);
console.log(`Version: ${entry.version}`);
console.log(`System Message: ${entry.systemMessage ?? "(none)"}`);
console.log(`Prompts: ${entry.prompts.length}`);

for (const prompt of entry.prompts) {
  const tag = prompt.isTemplate ? "[template]" : "[static]";
  console.log(`\n  Prompt #${prompt.order}: ${tag}`);
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
