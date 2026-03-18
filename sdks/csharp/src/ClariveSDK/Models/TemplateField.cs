namespace ClariveSDK.Models;

/// <summary>
/// Defines a template variable within a <see cref="Prompt"/>.
/// </summary>
/// <param name="Name">The variable name, matching <c>{{Name}}</c> in the prompt content.</param>
/// <param name="Type">The variable type: <c>string</c>, <c>int</c>, <c>float</c>, or <c>enum</c>.</param>
/// <param name="EnumValues">Allowed values when <paramref name="Type"/> is <c>enum</c>.</param>
/// <param name="DefaultValue">Default value if not provided by the caller.</param>
/// <param name="Min">Minimum value constraint for <c>int</c> and <c>float</c> types.</param>
/// <param name="Max">Maximum value constraint for <c>int</c> and <c>float</c> types.</param>
public record TemplateField(
    string Name,
    string Type,
    IReadOnlyList<string>? EnumValues,
    string? DefaultValue,
    double? Min,
    double? Max);
