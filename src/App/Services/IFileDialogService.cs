namespace DofusSwitcher.Services;

/// <summary>
/// Abstraction des boîtes de dialogue de fichier (export/import de config, US-P04). Isole les types WPF
/// (<c>Microsoft.Win32.*Dialog</c>) hors des ViewModels — ceux-ci restent testables (ref [DT-014]).
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Demande un chemin de sauvegarde. <paramref name="suggestedFileName"/> pré-remplit le nom.
    /// Retourne le chemin choisi, ou <c>null</c> si l'utilisateur annule.
    /// </summary>
    string? AskSavePath(string suggestedFileName);

    /// <summary>
    /// Demande un fichier à ouvrir avec le <paramref name="filter"/> de types donné (ex. config JSON ou
    /// exécutable du launcher). Retourne le chemin choisi, ou <c>null</c> si annulé.
    /// </summary>
    string? AskOpenPath(string filter);
}
