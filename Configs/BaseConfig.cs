using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace ReviveSystem.Configs;

public sealed class BaseConfig : BasePluginConfig
{
    [JsonPropertyName("Revive")]
    public ReviveConfig Revive { get; set; } = new();

    [JsonPropertyName("Permissions")]
    public PermissionsConfig Permissions { get; set; } = new();

    [JsonPropertyName("EnableDebug")]
    public bool EnableDebug { get; set; } = false;

    [JsonPropertyName("ConfigVersion")]
    public override int Version { get; set; } = 1;
}