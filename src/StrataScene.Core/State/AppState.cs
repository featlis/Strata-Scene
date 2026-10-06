using System.Text.Json.Serialization;

namespace StrataScene.Core.State;

public sealed class AppState
{
    [JsonPropertyName("current_scene_id")]
    public string? CurrentSceneId { get; set; }

    [JsonPropertyName("scratchpad_text")]
    public string ScratchpadText { get; set; } = string.Empty;
}
