using System.Text.Json;
using System.Text.Json.Serialization;

namespace BydClient.Models;

/// <summary>
/// A permission scope granted to a shared user.
/// </summary>
public sealed class EmpowerRange
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<EmpowerRange> Children { get; set; } = [];

    [JsonPropertyName("childList")]
    public List<EmpowerRange>? ChildList
    {
        get => Children;
        set => Children = value ?? [];
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }

    public EmpowerRange[] GetChildren() => Children.ToArray();
}