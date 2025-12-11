using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models;

public partial class Supplier : ObservableObject
{
    [ObservableProperty]
    private int _id;
    
    [MaxLength(128)]
    public required string Name { get; set; }
    
    [MaxLength(64)]
    public required string Phone { get; set; }
    
    [MaxLength(128)]
    public required string Email { get; set; }

    public double Quality { get; set; }              // 0–100
    
    public double ExperienceYears { get; set; }      // годы с десятыми
    
    public double Reliability { get; set; }          // % поставок вовремя
    
    public decimal SupplyCost { get; set; }          // ₽
    
    public double DeliverySpeedDays { get; set; }    // дни доставки
}