namespace ClariveSDK.Models;

public record Prompt(
    string Content,
    int Order,
    bool IsTemplate,
    IReadOnlyList<TemplateField>? TemplateFields);
