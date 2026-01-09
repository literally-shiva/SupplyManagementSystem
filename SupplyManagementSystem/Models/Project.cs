using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models;

public partial class Project : ObservableObject
{
    [ObservableProperty] private int _id;
    
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    
    public decimal MaterialNeed { get; set; }
}