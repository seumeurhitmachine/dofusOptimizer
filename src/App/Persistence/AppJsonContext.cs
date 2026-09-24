using System.Text.Json.Serialization;
using DofusSwitcher.Models;

namespace DofusSwitcher.Persistence;

/// <summary>
/// Contexte de sérialisation source-gen pour <see cref="AppConfig"/>.
/// [ARCH] Obligatoire : pas de réflexion dynamique, compatible publication single-file
/// (archi §Persistance). JSON indenté, camelCase, enums en chaînes (lisibilité — data-model).
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppConfig))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
