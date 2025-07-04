using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;

namespace MapTileDownloader;

[JsonSerializable(typeof(Configs))]
[JsonSerializable(typeof(List<TileSource>))]
[JsonSerializable(typeof(TileSource))]
[JsonSerializable(typeof(Coordinate))]
[JsonSerializable(typeof(Coordinate[]))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
internal partial class MapTileDownloaderJsonContext : JsonSerializerContext
{
    static MapTileDownloaderJsonContext()
    {
        Config = new MapTileDownloaderJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Converters = { new CoordinateConverter(), new CoordinateArrayConverter() } // 添加转换器
        });
    }

    public static MapTileDownloaderJsonContext Config { get; }
}

// 自定义 Coordinate 转换器
public class CoordinateConverter : JsonConverter<Coordinate>
{
    public override Coordinate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected object start");

        double x = 0, y = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Coordinate(x, y);

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            string propName = reader.GetString()!;
            reader.Read();

            switch (propName)
            {
                case "X":
                    x = reader.GetDouble();
                    break;
                case "Y":
                    y = reader.GetDouble();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, Coordinate value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}

// 自定义 Coordinate[] 转换器
public class CoordinateArrayConverter : JsonConverter<Coordinate[]>
{
    public override Coordinate[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");

        var coordinates = new List<Coordinate>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return coordinates.ToArray();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected object start");

            double x = 0, y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    coordinates.Add(new Coordinate(x, y));
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name");

                string propName = reader.GetString()!;
                reader.Read();

                switch (propName)
                {
                    case "X":
                        x = reader.GetDouble();
                        break;
                    case "Y":
                        y = reader.GetDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, Coordinate[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var coord in value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", coord.X);
            writer.WriteNumber("Y", coord.Y);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}