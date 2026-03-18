namespace ClariveSDK.Models;

public record TemplateField(
    string Name,
    string Type,
    IReadOnlyList<string>? EnumValues,
    string? DefaultValue,
    double? Min,
    double? Max);
