using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Data;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.ViewModels
{
    public partial class AnovaViewModel : ObservableObject
    {
        private readonly AppDbContext _db = new AppDbContext();

        public AnovaViewModel()
        {
            TransportationCosts = new ObservableCollection<TransportationCost>();
            AnovaResults = new ObservableCollection<AnovaResult>();

            LoadDataCommand.Execute(null);
        }

        [ObservableProperty]
        private ObservableCollection<TransportationCost> _transportationCosts;
        
        public ObservableCollection<AnovaResult> AnovaResults { get; set; }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            TransportationCosts = new ObservableCollection<TransportationCost>(
                await _db.TransportationCosts.AsNoTracking().ToListAsync());
        }

        [RelayCommand]
        private void RunAnova()
        {
            AnovaResults.Clear();

            if (!TransportationCosts.Any())
                return;

            // Группировка по складам
            var warehousesGroups = TransportationCosts.GroupBy(t => t.WarehouseId);

            foreach (var warehouseGroup in warehousesGroups)
            {
                int warehouseId = warehouseGroup.Key;
                int warehouseName = warehouseGroup.First().WarehouseId; // можно заменить на имя через _db.Warehouses

                // Группировка по сезону
                var seasonGroups = warehouseGroup.GroupBy(t => t.Season)
                                                .ToDictionary(g => g.Key, g => g.Select(x => (double)x.Cost).ToList());

                if (seasonGroups.Count < 2)
                    continue; // для ANOVA нужно хотя бы 2 группы

                // Общее количество наблюдений
                int n = seasonGroups.Sum(g => g.Value.Count);

                // Среднее по всем наблюдениям
                double grandMean = seasonGroups.SelectMany(g => g.Value).Average();

                // Межгрупповая сумма квадратов (SSB)
                double ssBetween = seasonGroups.Sum(g =>
                    g.Value.Count * Math.Pow(g.Value.Average() - grandMean, 2));

                // Внутригрупповая сумма квадратов (SSW)
                double ssWithin = seasonGroups.Sum(g =>
                    g.Value.Sum(v => Math.Pow(v - g.Value.Average(), 2)));

                int dfBetween = seasonGroups.Count - 1;
                int dfWithin = n - seasonGroups.Count;

                double msBetween = ssBetween / dfBetween;
                double msWithin = ssWithin / dfWithin;

                double fStat = msBetween / msWithin;

                // p-value через MathNet
                double pValue = 1.0 - SpecialFunctions.BetaRegularized(dfBetween / 2.0, dfWithin / 2.0, (dfBetween * fStat) / (dfBetween * fStat + dfWithin));

                AnovaResults.Add(new AnovaResult
                {
                    WarehouseId = warehouseId,
                    WarehouseName = _db.Warehouses.FirstOrDefault(w => w.Id == warehouseId)?.Name ?? $"Warehouse {warehouseId}",
                    FStatistic = fStat,
                    PValue = pValue,
                    IsSignificant = pValue < 0.05
                });
            }
        }
    }

    public class AnovaResult
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = "";
        public double FStatistic { get; set; }
        public double PValue { get; set; }
        public bool IsSignificant { get; set; }
    }
}
