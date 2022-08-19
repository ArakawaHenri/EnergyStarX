using EnergyStar.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace EnergyStar.Views;

public sealed partial class PowerPlanPage : Page
{
    public PowerPlanViewModel ViewModel
    {
        get;
    }

    public PowerPlanPage()
    {
        ViewModel = App.GetService<PowerPlanViewModel>();
        InitializeComponent();
    }
}
