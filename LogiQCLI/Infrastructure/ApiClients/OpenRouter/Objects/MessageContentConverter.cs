using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class MessageContentConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                return JsonSerializer.Deserialize<List<TextContentPart>>(ref reader, options);
            }

            return JsonDocument.ParseValue(ref reader).RootElement.Clone();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case string stringValue:
                    writer.WriteStringValue(stringValue);
                    break;
                case List<TextContentPart> textParts:
                    JsonSerializer.Serialize(writer, textParts, options);
                    break;
                case IEnumerable<MessageContentPart> parts:
                    JsonSerializer.Serialize(writer, parts, options);
                    break;
                default:
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                    break;
            }
        }
    }
}