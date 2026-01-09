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
        Tabs.Add(new TabModel { Title = "Поставщики", Icon = "🏢", Content = new SuppliersView() });
        Tabs.Add(new TabModel { Title = "Диффузия", Icon = "🌊", Content = new MarketDiffusionView() });
        Tabs.Add(new TabModel { Title = "Кластеризация", Icon = "🧩", Content = new SupplierClusteringView() });
        Tabs.Add(new TabModel { Title = "МАИ-анализ", Icon = "🧠", Content = new SupplierAhpView() });
        Tabs.Add(new TabModel { Title = "Линейное программирование", Icon = "📊", Content = new TransportationProblemView() });
        Tabs.Add(new TabModel { Title = "Дисперсия", Icon = "📈", Content = new AnovaView() });
        Tabs.Add(new TabModel { Title = "Сетевое планирование", Icon = "📅", Content = new NetworkGraphTabView() });

        SelectedTab = Tabs[0];
    }
}

public partial class TabModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _icon = string.Empty;
    [ObservableProperty] private UserControl? _content;
}