using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models;

public partial class Warehouse :  ObservableObject
{
    [ObservableProperty]
    private int _id;
    
    [MaxLength(128)] 
    public string Name { get; set; } = String.Empty;
    
    public decimal MaterialAmount { get; set; }
}