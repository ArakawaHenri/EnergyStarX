using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EnergyStar.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace EnergyStar.Views;

public sealed partial class PowerPlanPage : Page
{
    public PowerPlanViewModel ViewModel
    {
        get;
    }

    private static readonly ObservableCollection<RuleItem> rules;

    static PowerPlanPage()
    {
        rules = new ObservableCollection<RuleItem>()
        {
            new RuleItem("example.exe", "Example Rule"),
            //new RuleItem("New Bypass Task", "")
        };
    }

    public PowerPlanPage()
    {
        ViewModel = App.GetService<PowerPlanViewModel>();
        InitializeComponent();
        ListBox_Rule.ItemsSource = rules;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (RuleItem.LstRO.LstRO == false)
        {
            ListBox_Rule.SelectionMode = ListViewSelectionMode.Multiple;
            btnEdit.Content = "Confirm";
        }
        base.OnNavigatedTo(e);
    }

    public static void CheckTextChanged()
    {
        if (rules.Last().RuleItemNotified.TaskName != "New Bypass Task")
        {
            rules.Add(new RuleItem("New Bypass Task"));
        }
    }

    private void EditClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (RuleItem.LstRO.LstRO == true)
        {
            RuleItem.LstRO.LstRO = false;
            ListBox_Rule.SelectionMode = ListViewSelectionMode.Multiple;
            rules.Add(new RuleItem("New Bypass Task"));
            if (sender is Button btn)
            {
                btn.Content = "Confirm";
            }
        }
        else
        {
            RuleItem.LstRO.LstRO = true;
            ListBox_Rule.SelectionMode = ListViewSelectionMode.None;
            if (rules.Last().RuleItemNotified.TaskName == "New Bypass Task")
            {
                rules.RemoveAt(rules.Count - 1);
            }
            if (sender is Button btn)
            {
                btn.Content = "Edit";
            }
        }
    }
}

public class RuleItem
{
    static RuleItem()
    {
        lstRO = new();
    }
    public RuleItem(string taskName, string description="")
    {
        ruleItemNotified = new(taskName, description);
    }
    private static ListReadOnly lstRO;
    public static ListReadOnly LstRO
    {
        get => lstRO;
        set => lstRO = value;
    }

    private RuleItemNotified ruleItemNotified;
    public RuleItemNotified RuleItemNotified
    {
        get => ruleItemNotified;
        set => ruleItemNotified = value;
    }
}

public class ListReadOnly : INotifyPropertyChanged
{
    private bool lstRO;
    public bool LstRO
    {
        get => lstRO;
        set
        {
            if (value != lstRO)
            {
                lstRO = value;
                NotifyPropertyChanged();
            }
        }
    }

    public ListReadOnly()
    {
        lstRO = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RuleItemNotified : INotifyPropertyChanged
{
    private string taskName;
    private string description;
    public string TaskName
    {
        get => taskName;
        set
        {
            if (value != taskName)
            {
                taskName = value;
                NotifyPropertyChanged();
                PowerPlanPage.CheckTextChanged();
            }
        }
    }

    public string Description
    {
        get => description;
        set
        {
            if (value != description)
            {
                description = value;
                NotifyPropertyChanged();
            }
        }
    }

    public RuleItemNotified(string _taskName, string _description = "")
    {
        taskName = _taskName;
        description = _description;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
