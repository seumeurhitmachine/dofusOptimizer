using DofusSwitcher.Models;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests smoke de l'Axe 1 : le socle MVVM s'instancie et se comporte comme attendu.
/// Aucune dépendance UI (les Views ne sont pas testées unitairement — recette manuelle).
/// </summary>
public class SmokeTests
{
    [Fact]
    public void MainViewModel_ExposeSonTitre()
    {
        var vm = new MainViewModel(AppConfig.Default);
        Assert.Equal("Dofus Window Switcher", vm.Title);
    }

    [Fact]
    public void RelayCommand_ExecuteInvoqueLAction()
    {
        var invoque = false;
        var command = new RelayCommand(() => invoque = true);

        Assert.True(command.CanExecute(null));
        command.Execute(null);

        Assert.True(invoque);
    }

    [Fact]
    public void RelayCommand_CanExecuteRespecteLePredicat()
    {
        var autorise = false;
        var command = new RelayCommand(() => { }, () => autorise);

        Assert.False(command.CanExecute(null));

        autorise = true;
        Assert.True(command.CanExecute(null));
    }
}
