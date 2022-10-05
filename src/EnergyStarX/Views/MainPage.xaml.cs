using EnergyStarX.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace EnergyStarX.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
