// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace SolarEngine.Features.Updates.Infrastructure;

[JsonSerializable(typeof(PersistedInstallationMetadata))]
[JsonSerializable(typeof(PersistedUpdateRequest))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = false,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class UpdateJsonContext : JsonSerializerContext;
