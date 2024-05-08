using System.Text.Json;

namespace mhwiki.cli.Utililty;
internal static class JsonSerializerOptionsProvider
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static readonly JsonSerializerOptions RelaxedDateTime = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    static JsonSerializerOptionsProvider()
    {
        RelaxedDateTime.Converters.Add(new DateTimeConverterUsingDateTimeParse());
    }
}
