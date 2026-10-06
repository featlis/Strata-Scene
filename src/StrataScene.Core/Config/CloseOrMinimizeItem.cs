using System.Text.Json;
using System.Text.Json.Serialization;

namespace StrataScene.Core.Config;

public enum WindowCloseMode
{
    Minimize,
    Close
}

[JsonConverter(typeof(CloseOrMinimizeItemConverter))]
public sealed record CloseOrMinimizeItem(string Process, WindowCloseMode Mode = WindowCloseMode.Minimize);

public sealed class CloseOrMinimizeItemConverter : JsonConverter<CloseOrMinimizeItem>
{
    public override CloseOrMinimizeItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var process = reader.GetString() ?? string.Empty;
            return new CloseOrMinimizeItem(process, WindowCloseMode.Minimize);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? process = null;
            var mode = WindowCloseMode.Minimize;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propName = reader.GetString();
                    reader.Read();

                    if (string.Equals(propName, "process", StringComparison.OrdinalIgnoreCase))
                    {
                        process = reader.GetString();
                    }
                    else if (string.Equals(propName, "mode", StringComparison.OrdinalIgnoreCase))
                    {
                        var modeStr = reader.GetString();
                        if (string.Equals(modeStr, "close", StringComparison.OrdinalIgnoreCase))
                        {
                            mode = WindowCloseMode.Close;
                        }
                        else
                        {
                            mode = WindowCloseMode.Minimize;
                        }
                    }
                }
            }

            return new CloseOrMinimizeItem(process ?? string.Empty, mode);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for CloseOrMinimizeItem");
    }

    public override void Write(Utf8JsonWriter writer, CloseOrMinimizeItem value, JsonSerializerOptions options)
    {
        if (value.Mode == WindowCloseMode.Minimize)
        {
            writer.WriteStringValue(value.Process);
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteString("process", value.Process);
            writer.WriteString("mode", "close");
            writer.WriteEndObject();
        }
    }
}
