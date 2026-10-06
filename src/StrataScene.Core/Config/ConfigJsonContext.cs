using System.Text.Json;
using System.Text.Json.Serialization;
using StrataScene.Core.State;

namespace StrataScene.Core.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(AppState))]
public sealed partial class ConfigJsonContext : JsonSerializerContext
{
}
