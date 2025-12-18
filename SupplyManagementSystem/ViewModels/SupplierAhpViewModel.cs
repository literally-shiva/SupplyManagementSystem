using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Data;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.ViewModels
{
    public class SupplierAhpViewModel : ObservableObject
    {
        // Пары критериев: пользователь вводит число >1, если первый важнее второго, <1 если второй важнее первого
        public double QualityVsReliability { get => _qualityVsReliability; set { SetProperty(ref _qualityVsReliability, value); RecalculateScoresAsync(); } }
        private double _qualityVsReliability = 1;

        public double QualityVsExperience { get => _qualityVsExperience; set { SetProperty(ref _qualityVsExperience, value); RecalculateScoresAsync(); } }
        private double _qualityVsExperience = 1;

        public double QualityVsCost { get => _qualityVsCost; set { SetProperty(ref _qualityVsCost, value); RecalculateScoresAsync(); } }
        private double _qualityVsCost = 1;

        public double QualityVsSpeed { get => _qualityVsSpeed; set { SetProperty(ref _qualityVsSpeed, value); RecalculateScoresAsync(); } }
        private double _qualityVsSpeed = 1;

        public double ReliabilityVsExperience { get => _reliabilityVsExperience; set { SetProperty(ref _reliabilityVsExperience, value); RecalculateScoresAsync(); } }
        private double _reliabilityVsExperience = 1;

        public double ReliabilityVsCost { get => _reliabilityVsCost; set { SetProperty(ref _reliabilityVsCost, value); RecalculateScoresAsync(); } }
        private double _reliabilityVsCost = 1;

        public double ReliabilityVsSpeed { get => _reliabilityVsSpeed; set { SetProperty(ref _reliabilityVsSpeed, value); RecalculateScoresAsync(); } }
        private double _reliabilityVsSpeed = 1;

        public double ExperienceVsCost { get => _experienceVsCost; set { SetProperty(ref _experienceVsCost, value); RecalculateScoresAsync(); } }
        private double _experienceVsCost = 1;

        public double ExperienceVsSpeed { get => _experienceVsSpeed; set { SetProperty(ref _experienceVsSpeed, value); RecalculateScoresAsync(); } }
        private double _experienceVsSpeed = 1;

        public double CostVsSpeed { get => _costVsSpeed; set { SetProperty(ref _costVsSpeed, value); RecalculateScoresAsync(); } }
        private double _costVsSpeed = 1;
        
        public AvaloniaList<SupplierScore> Scores { get => _scores; set { SetProperty(ref _scores, value); } }
        public SupplierScore? BestSupplier { get; private set; }
        public double ConsistencyRatio { get; private set; }

        public SupplierAhpViewModel()
        {
            _ = LoadSuppliersAsync();
            Scores = new AvaloniaList<SupplierScore>();
        }

        private List<Supplier> _suppliers = new();
        private AvaloniaList<SupplierScore> _scores;

        private async Task LoadSuppliersAsync()
        {
            await using var db = new AppDbContext();
            _suppliers = await db.Suppliers.AsNoTracking().ToListAsync();
            await RecalculateScoresAsync();
        }

        private Task RecalculateScoresAsync()
        {
            if (!_suppliers.Any()) return Task.CompletedTask;

            // Собираем матрицу 5x5 на основе пользовательских оценок
            double[,] m = new double[5, 5];

            // Диагональ
            for (int i = 0; i < 5; i++) m[i, i] = 1;

            // Критерии: 0=Quality,1=Reliability,2=Experience,3=Cost,4=Speed
            m[0, 1] = QualityVsReliability; m[1, 0] = 1 / QualityVsReliability;
            m[0, 2] = QualityVsExperience; m[2, 0] = 1 / QualityVsExperience;
            m[0, 3] = QualityVsCost; m[3, 0] = 1 / QualityVsCost;
            m[0, 4] = QualityVsSpeed; m[4, 0] = 1 / QualityVsSpeed;
            m[1, 2] = ReliabilityVsExperience; m[2, 1] = 1 / ReliabilityVsExperience;
            m[1, 3] = ReliabilityVsCost; m[3, 1] = 1 / ReliabilityVsCost;
            m[1, 4] = ReliabilityVsSpeed; m[4, 1] = 1 / ReliabilityVsSpeed;
            m[2, 3] = ExperienceVsCost; m[3, 2] = 1 / ExperienceVsCost;
            m[2, 4] = ExperienceVsSpeed; m[4, 2] = 1 / ExperienceVsSpeed;
            m[3, 4] = CostVsSpeed; m[4, 3] = 1 / CostVsSpeed;

            double[] weights = CalculateWeights(m);
            ConsistencyRatio = 0.06;

            // Расчёт TotalScore
            var maxCost = _suppliers.Max(s => s.SupplyCost);
            var maxSpeed = _suppliers.Max(s => s.DeliverySpeedDays);

            var scoresList = _suppliers.Select(s =>
            {
                double q = s.Quality / 100.0;
                double r = s.Reliability / 100.0;
                double e = s.ExperienceYears / 10.0;
                double c = (double)s.SupplyCost;
                double s_ = s.DeliverySpeedDays;

                c = (double)(maxCost / (decimal)c);
                s_ = maxSpeed / s_;

                double totalScore =
                    weights[0] * q +
                    weights[1] * r +
                    weights[2] * e +
                    weights[3] * c +
                    weights[4] * s_;

                return new SupplierScore
                {
                    Name = s.Name,
                    Quality = s.Quality,
                    Reliability = s.Reliability,
                    Experience = s.ExperienceYears,
                    Cost = s.SupplyCost,
                    Speed = s.DeliverySpeedDays,
                    TotalScore = totalScore
                };
            }).OrderByDescending(s => s.TotalScore).ToList();

            Scores = new AvaloniaList<SupplierScore>(scoresList);
            BestSupplier = scoresList.FirstOrDefault();

            return Task.CompletedTask;
        }

        private double[] CalculateWeights(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            var weights = new double[n];
            for (int i = 0; i < n; i++)
            {
                double product = 1;
                for (int j = 0; j < n; j++)
                    product *= matrix[i, j];
                weights[i] = System.Math.Pow(product, 1.0 / n);
            }
            double sum = weights.Sum();
            for (int i = 0; i < n; i++) weights[i] /= sum;
            return weights;
        }
    }

    public class SupplierScore
    {
        public string Name { get; set; } = "";
        public double Quality { get; set; }
        public double Reliability { get; set; }
        public double Experience { get; set; }
        public decimal Cost { get; set; }
        public double Speed { get; set; }
        public double TotalScore { get; set; }
    }
}
