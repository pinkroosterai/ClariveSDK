using System.Text.Json;
using ClariveSDK.Models;

namespace ClariveSDK.Tests;

public class ModelDeserializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void PromptEntry_Deserializes_FromApiJson()
    {
        var json = """
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "title": "Customer Support Reply",
          "systemMessage": "You are a helpful customer support agent for {{companyName}}.",
          "version": 3,
          "prompts": [
            {
              "content": "Draft a reply to the following customer inquiry:\n\n{{customerMessage}}",
              "order": 1,
              "isTemplate": true,
              "templateFields": [
                {
                  "id": "00000000-0000-0000-0000-000000000001",
                  "promptId": "00000000-0000-0000-0000-000000000002",
                  "name": "customerMessage",
                  "type": "string",
                  "enumValues": null,
                  "defaultValue": null,
                  "min": null,
                  "max": null
                }
              ]
            }
          ],
          "tags": ["test"],
          "updatedAt": "2026-03-18T10:00:00Z",
          "publishedAt": "2026-03-18T10:00:00Z",
          "tabs": [
            { "id": "00000000-0000-0000-0000-000000000010", "name": "Main", "isMainTab": true, "forkedFromVersion": null }
          ],
          "tabCount": 1
        }
        """;

        var entry = JsonSerializer.Deserialize<PromptEntry>(json, JsonOptions);

        Assert.NotNull(entry);
        Assert.Equal(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), entry.Id);
        Assert.Equal("Customer Support Reply", entry.Title);
        Assert.Equal("You are a helpful customer support agent for {{companyName}}.", entry.SystemMessage);
        Assert.Equal(3, entry.Version);
        Assert.Single(entry.Prompts);

        var prompt = entry.Prompts[0];
        Assert.Contains("{{customerMessage}}", prompt.Content);
        Assert.Equal(1, prompt.Order);
        Assert.True(prompt.IsTemplate);
        Assert.NotNull(prompt.TemplateFields);
        Assert.Single(prompt.TemplateFields);

        var field = prompt.TemplateFields[0];
        Assert.Equal("customerMessage", field.Name);
        Assert.Equal("string", field.Type);
        Assert.Null(field.EnumValues);
        Assert.Null(field.DefaultValue);
        Assert.Null(field.Min);
        Assert.Null(field.Max);

        Assert.Single(entry.Tabs);
        Assert.Equal("Main", entry.Tabs[0].Name);
        Assert.True(entry.Tabs[0].IsMainTab);
        Assert.Null(entry.Tabs[0].ForkedFromVersion);
        Assert.Equal(1, entry.TabCount);
    }

    [Fact]
    public void PromptEntry_Deserializes_WithNullSystemMessage()
    {
        var json = """
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "title": "Simple Prompt",
          "systemMessage": null,
          "version": 1,
          "prompts": [],
          "tags": ["test"],
          "updatedAt": "2026-03-18T10:00:00Z",
          "publishedAt": "2026-03-18T10:00:00Z",
          "tabs": [],
          "tabCount": 0
        }
        """;

        var entry = JsonSerializer.Deserialize<PromptEntry>(json, JsonOptions);

        Assert.NotNull(entry);
        Assert.Null(entry.SystemMessage);
        Assert.Empty(entry.Prompts);
    }

    [Fact]
    public void Prompt_Deserializes_WithNullTemplateFields()
    {
        var json = """
        {
          "content": "Hello world",
          "order": 1,
          "isTemplate": false,
          "templateFields": null
        }
        """;

        var prompt = JsonSerializer.Deserialize<Prompt>(json, JsonOptions);

        Assert.NotNull(prompt);
        Assert.False(prompt.IsTemplate);
        Assert.Null(prompt.TemplateFields);
    }

    [Fact]
    public void TemplateField_Deserializes_WithEnumValues()
    {
        var json = """
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "promptId": "00000000-0000-0000-0000-000000000002",
          "name": "color",
          "type": "enum",
          "enumValues": ["red", "green", "blue"],
          "defaultValue": "red",
          "min": null,
          "max": null
        }
        """;

        var field = JsonSerializer.Deserialize<TemplateField>(json, JsonOptions);

        Assert.NotNull(field);
        Assert.Equal("color", field.Name);
        Assert.Equal("enum", field.Type);
        Assert.NotNull(field.EnumValues);
        Assert.Equal(3, field.EnumValues.Count);
        Assert.Equal("red", field.DefaultValue);
    }

    [Fact]
    public void TemplateField_Deserializes_WithMinMax()
    {
        var json = """
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "promptId": "00000000-0000-0000-0000-000000000002",
          "name": "temperature",
          "type": "float",
          "enumValues": null,
          "defaultValue": null,
          "min": 0.0,
          "max": 1.0
        }
        """;

        var field = JsonSerializer.Deserialize<TemplateField>(json, JsonOptions);

        Assert.NotNull(field);
        Assert.Equal(0.0, field.Min);
        Assert.Equal(1.0, field.Max);
    }

    [Fact]
    public void GenerateResponse_Deserializes_FromApiJson()
    {
        var json = """
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "title": "Customer Support Reply",
          "version": 3,
          "systemMessage": "You are a helpful customer support agent for Acme Corp.",
          "renderedPrompts": [
            {
              "content": "Draft a reply to the following customer inquiry:\n\nI need help with my order #12345",
              "order": 1
            }
          ]
        }
        """;

        var response = JsonSerializer.Deserialize<GenerateResponse>(json, JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), response.Id);
        Assert.Equal("Customer Support Reply", response.Title);
        Assert.Equal(3, response.Version);
        Assert.Equal("You are a helpful customer support agent for Acme Corp.", response.SystemMessage);
        Assert.Single(response.RenderedPrompts);

        var rendered = response.RenderedPrompts[0];
        Assert.Contains("order #12345", rendered.Content);
        Assert.Equal(1, rendered.Order);
    }
}
