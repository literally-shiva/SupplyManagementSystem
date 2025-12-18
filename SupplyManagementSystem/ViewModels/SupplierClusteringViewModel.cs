using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using SkiaSharp;
using SupplyManagementSystem.Data;

namespace SupplyManagementSystem.ViewModels;

public partial class SupplierClusteringViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ISeries> _series = new();

    public SupplierClusteringViewModel()
    {
        _ = LoadAndClusterAsync();
    }

    private async Task LoadAndClusterAsync()
    {
        await using var db = new AppDbContext();
        var suppliers = await db.Suppliers.AsNoTracking().ToListAsync();

        if (suppliers.Count == 0)
            return;

        // ML.NET clustering
        var ml = new MLContext();

        var data = ml.Data.LoadFromEnumerable(
            suppliers.Select(s => new SupplierFeatures
            {
                Quality = (float)s.Quality,
                Experience = (float)s.ExperienceYears,
                Reliability = (float)s.Reliability,
                Cost = (float)s.SupplyCost,
                Speed = (float)s.DeliverySpeedDays
            })
        );

        var pipeline = ml.Transforms
            .Concatenate("Features",
                nameof(SupplierFeatures.Quality),
                nameof(SupplierFeatures.Experience),
                nameof(SupplierFeatures.Reliability),
                nameof(SupplierFeatures.Cost),
                nameof(SupplierFeatures.Speed))
            .Append(ml.Clustering.Trainers.KMeans("Features", numberOfClusters: 3));

        var model = pipeline.Fit(data);
        var predictor = ml.Model.CreatePredictionEngine<SupplierFeatures, SupplierClusterPrediction>(model);

        // Build cluster points
        var points = suppliers.Select(s =>
        {
            var pred = predictor.Predict(new SupplierFeatures
            {
                Quality = (float)s.Quality,
                Experience = (float)s.ExperienceYears,
                Reliability = (float)s.Reliability,
                Cost = (float)s.SupplyCost,
                Speed = (float)s.DeliverySpeedDays
            });

            return new ClusterPoint
            {
                X = s.Quality,
                Y = s.Reliability,
                Cluster = pred.PredictedClusterId
            };
        }).ToList();

        var grouped = points.GroupBy(p => p.Cluster);

        SKColor[] palette =
        {
            SKColors.Red, SKColors.Blue, SKColors.Green, SKColors.Orange,
            SKColors.Purple, SKColors.Yellow
        };

        var newSeries = new ObservableCollection<ISeries>();

        foreach (var group in grouped)
        {
            newSeries.Add(new ScatterSeries<ClusterPoint>
            {
                Name = $"Кластер {group.Key}",
                Values = group.ToList(),
                Mapping = (cp, index) => new Coordinate(cp.X, cp.Y),
                GeometrySize = 10,
                Fill = new SolidColorPaint(palette[(int)group.Key % palette.Length])
            });
        }

        Series = newSeries;
    }
}

public class SupplierFeatures
{
    public float Quality { get; set; }
    public float Experience { get; set; }
    public float Reliability { get; set; }
    public float Cost { get; set; }
    public float Speed { get; set; }
}

public class SupplierClusterPrediction
{
    [ColumnName("PredictedLabel")] public uint PredictedClusterId { get; set; }
}

public class ClusterPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public uint Cluster { get; set; }
}
