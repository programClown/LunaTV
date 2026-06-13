using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunaTV.Converters;

public class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // Returning 0 for Null is fine for most fields (e.g., CoverX, CoverY),
            // but could be ambiguous for fields like Limit/Page where 0 is a valid non-null value.
            case JsonTokenType.Null:
                return 0;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.String:
                int.TryParse(reader.GetString(), out var value);
                return value;
        }

        throw new JsonException($"无法将{reader.GetString()}转换成整数");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                return reader.GetInt32() != 0;
            case JsonTokenType.String:
                var value = reader.GetString();
                if (bool.TryParse(value, out var boolValue)) return boolValue;
                if (int.TryParse(value, out var intValue)) return intValue != 0;
                return false;
        }

        throw new JsonException("无法转换成布尔值");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

public class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32().ToString();
            case JsonTokenType.String:
                return reader.GetString();
        }

        throw new JsonException($"无法将{reader.GetString()}转换成字符串");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}