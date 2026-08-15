using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using InfiniteDubhe.Core;

namespace InfiniteDubhe.Resources;

/// <summary>
/// 场景/组件属性值用到的自定义 JSON 转换器。
/// <see cref="Vector2"/>/<see cref="Color"/>/<see cref="Rectangle"/> 均为字段型 readonly struct，
/// System.Text.Json 默认无法序列化，故逐个显式读写字段。
/// </summary>
public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float x = 0, y = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var name = reader.GetString();
            reader.Read();
            if (name == "X") x = reader.GetSingle();
            else if (name == "Y") y = reader.GetSingle();
        }
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float r = 0, g = 0, b = 0, a = 1;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var name = reader.GetString();
            reader.Read();
            if (name == "R") r = reader.GetSingle();
            else if (name == "G") g = reader.GetSingle();
            else if (name == "B") b = reader.GetSingle();
            else if (name == "A") a = reader.GetSingle();
        }
        return new Color(r, g, b, a);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("R", value.R);
        writer.WriteNumber("G", value.G);
        writer.WriteNumber("B", value.B);
        writer.WriteNumber("A", value.A);
        writer.WriteEndObject();
    }
}

public sealed class RectangleJsonConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int x = 0, y = 0, w = 0, h = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var name = reader.GetString();
            reader.Read();
            if (name == "X") x = reader.GetInt32();
            else if (name == "Y") y = reader.GetInt32();
            else if (name == "Width") w = reader.GetInt32();
            else if (name == "Height") h = reader.GetInt32();
        }
        return new Rectangle(x, y, w, h);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Width", value.Width);
        writer.WriteNumber("Height", value.Height);
        writer.WriteEndObject();
    }
}
