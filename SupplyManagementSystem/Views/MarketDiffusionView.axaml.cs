using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Threading;
using SupplyManagementSystem.ViewModels;

namespace SupplyManagementSystem.Views;

public partial class MarketDiffusionView : UserControl
{
    private DispatcherTimer? _invalidateTimer;

    public MarketDiffusionView()
    {
        InitializeComponent();

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not MarketDiffusionViewModel vm)
            return;

        vm.Start();

        // 🔥 Таймер ТОЛЬКО для перерисовки
        _invalidateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };

        _invalidateTimer.Tick += (_, _) =>
        {
            DiffusionImage.InvalidateVisual();
        };

        _invalidateTimer.Start();
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _invalidateTimer?.Stop();
        _invalidateTimer = null;

        (DataContext as MarketDiffusionViewModel)?.Stop();
    }
}