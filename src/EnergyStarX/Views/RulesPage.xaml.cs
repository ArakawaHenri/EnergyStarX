using EnergyStarX.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace EnergyStarX.Views;

public sealed partial class RulesPage : Page
{
    public RulesViewModel ViewModel
    {
        get;
    }

    public RulesPage()
    {
        ViewModel = App.GetService<RulesViewModel>();
        InitializeComponent();
    }
}
