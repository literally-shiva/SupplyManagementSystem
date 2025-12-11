using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using SupplyManagementSystem.Views;

namespace SupplyManagementSystem.ViewModels;

public partial class MainWindowControlViewModel : ObservableObject
{
    [ObservableProperty]
    private TabModel? _selectedTab;

    public AvaloniaList<TabModel> Tabs { get; } = new();

    public MainWindowControlViewModel()
    {
        InitializeTabs();
    }

    private void InitializeTabs()
    {
        Tabs.Add(new TabModel { Title = "Дашборд", Icon = "📊", Content = new UserControl() });
        Tabs.Add(new TabModel { Title = "Заказы", Icon = "📦", Content = new UserControl() });
        Tabs.Add(new TabModel { Title = "Поставщики", Icon = "🏢", Content = new SuppliersView() { DataContext = new SuppliersViewModel() }});
        Tabs.Add(new TabModel { Title = "Склад", Icon = "🏬", Content = new UserControl() });
        Tabs.Add(new TabModel { Title = "Отчёты", Icon = "📈", Content = new UserControl() });
        Tabs.Add(new TabModel { Title = "Настройки", Icon = "⚙️", Content = new UserControl() });

        SelectedTab = Tabs[0];
    }
}

public partial class TabModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _icon = string.Empty;
    [ObservableProperty] private UserControl? _content;
}