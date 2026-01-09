using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models
{
    public partial class AnovaResult : ObservableObject
    {
        public int WarehouseId { get; set; }
        
        public string WarehouseName { get; set; } = String.Empty;
        
        public string Factor { get; set; }  // например, "Season"
        
        public double FStatistic { get; set; }
        
        public double PValue { get; set; }
        
        public bool Significant { get; set; }
    }
}