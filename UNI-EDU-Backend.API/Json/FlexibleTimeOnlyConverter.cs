using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UNI_EDU_Backend.API.Json;

/// <summary>
/// Accepts time strings in either "HH:mm" or "HH:mm:ss[.fff]" form on the wire.
/// The default System.Text.Json TimeOnly converter only accepts "HH:mm:ss" minimum,
/// which is awkward for UI schedule pickers that emit "19:00".
/// Serializes back as "HH:mm:ss".
/// </summary>
public class FlexibleTimeOnlyConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] AcceptedFormats =
    {
        "HH:mm",
        "HH:mm:ss",
        "HH:mm:ss.f",
        "HH:mm:ss.ff",
        "HH:mm:ss.fff",
        "HH:mm:ss.ffff",
        "HH:mm:ss.fffff",
        "HH:mm:ss.ffffff",
        "HH:mm:ss.fffffff"
    };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected string for TimeOnly, got {reader.TokenType}.");

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            throw new JsonException("TimeOnly value is empty.");

        // Strip trailing UTC marker some clients send (e.g. "08:57:14.602Z" from a JS Date).
        // TimeOnly is timezone-naive, so the 'Z' is meaningless here.
        var candidate = raw.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ? raw[..^1] : raw;

        if (TimeOnly.TryParseExact(candidate, AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            return t;

        throw new JsonException($"Invalid time '{raw}'. Expected 'HH:mm' or 'HH:mm:ss'.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}
