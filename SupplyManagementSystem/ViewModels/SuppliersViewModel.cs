using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Collections;
using SupplyManagementSystem.Models;
using SupplyManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SupplyManagementSystem.ViewModels;

public class SuppliersViewModel : ObservableObject
{
    public AvaloniaList<Supplier> Suppliers { get; } = new();

    public SuppliersViewModel()
    {
        _ = LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        await using var db = new AppDbContext();
        var data = await db.Suppliers.AsNoTracking().ToListAsync();
        Suppliers.Clear();
        Suppliers.AddRange(data);
    }
}