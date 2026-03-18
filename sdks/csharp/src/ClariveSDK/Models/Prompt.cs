namespace ClariveSDK.Models;

/// <summary>
/// An individual prompt within a <see cref="PromptEntry"/>.
/// </summary>
/// <param name="Content">The prompt text. May contain <c>{{variable}}</c> placeholders when <paramref name="IsTemplate"/> is <c>true</c>.</param>
/// <param name="Order">The display/execution order of this prompt.</param>
/// <param name="IsTemplate">Whether this prompt contains template variables.</param>
/// <param name="TemplateFields">
/// Template variable definitions. Present only when <paramref name="IsTemplate"/> is <c>true</c>.
/// </param>
public record Prompt(
    string Content,
    int Order,
    bool IsTemplate,
    IReadOnlyList<TemplateField>? TemplateFields);
