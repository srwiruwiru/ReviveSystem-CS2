using System.Text.Json.Serialization;

namespace ReviveSystem.Configs;

public sealed class PermissionsConfig
{
    [JsonPropertyName("CanReviveOthersFlag")]
    public string CanReviveOthersFlag { get; set; } = "@css/vip";

    [JsonPropertyName("CanBeRevivedFlag")]
    public string CanBeRevivedFlag { get; set; } = "";
}