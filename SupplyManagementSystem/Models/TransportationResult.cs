using System;

namespace SupplyManagementSystem.Models;

public class TransportationResult
{
    public string Warehouse { get; set; } = String.Empty;
    
    public string Project { get; set; } = String.Empty;
    
    public double Amount { get; set; }
    
    public double Cost { get; set; }
    
    public string Season { get; set; } = String.Empty;
}