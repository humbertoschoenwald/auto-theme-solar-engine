using System.Text.Json.Serialization;
namespace SolarEngine.Infrastructure.Serialization;

[JsonSerializable(typeof(PersistedAppConfig))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = false,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class AppConfigJsonContext : JsonSerializerContext;
