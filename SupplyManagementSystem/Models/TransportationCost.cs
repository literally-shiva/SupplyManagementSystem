using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models;

public partial class TransportationCost : ObservableObject
{
    [ObservableProperty] 
    private int _id;
    
    public int WarehouseId { get; set; }
    
    public int ProjectId { get; set; }
    
    public decimal Cost { get; set; }
    
    [MaxLength(64)]
    public string Season { get; set; } = "Winter";
}