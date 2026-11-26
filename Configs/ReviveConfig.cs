using System.Text.Json.Serialization;

namespace ReviveSystem.Configs;

public sealed class ReviveConfig
{
    [JsonPropertyName("Button")]
    public string Button { get; set; } = "Use";

    [JsonPropertyName("HoldTime")]
    public int HoldTime { get; set; } = 15;

    [JsonPropertyName("Range")]
    public float Range { get; set; } = 100.0f;

    [JsonPropertyName("MaxPerRound")]
    public int MaxPerRound { get; set; } = 1;
}