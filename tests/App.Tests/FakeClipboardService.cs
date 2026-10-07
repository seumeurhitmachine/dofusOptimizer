using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="IClipboardService"/> : mémorise le dernier texte copié, sans toucher au
/// presse-papier système. Permet d'assertion sur la formule <c>/invite …</c> (Axe 13).
/// </summary>
public sealed class FakeClipboardService : IClipboardService
{
    /// <summary>Dernier texte passé à <see cref="SetText"/> (<c>null</c> si jamais appelé).</summary>
    public string? LastText { get; private set; }

    public void SetText(string text) => LastText = text;
}
