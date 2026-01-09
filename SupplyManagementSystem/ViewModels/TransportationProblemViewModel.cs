using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.OrTools.LinearSolver;
using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Data;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.ViewModels
{
    public partial class TransportationViewModel : ObservableObject
    {
        private readonly AppDbContext _db = new AppDbContext();

        public TransportationViewModel()
        {
            Warehouses = new ObservableCollection<Warehouse>();
            Projects = new ObservableCollection<Project>();
            TransportationCosts = new ObservableCollection<TransportationCost>();
            Results = new ObservableCollection<TransportationResult>();

            LoadDataCommand.Execute(null);
        }

        // =======================
        // Исходные данные
        // =======================
        [ObservableProperty] private ObservableCollection<Warehouse> _warehouses;
        [ObservableProperty] private ObservableCollection<Project> _projects;
        [ObservableProperty] private ObservableCollection<TransportationCost> _transportationCosts;

        // =======================
        // Результат расчёта
        // =======================
        [ObservableProperty] private ObservableCollection<TransportationResult> _results;

        // =======================
        // Команды
        // =======================
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            Warehouses = new ObservableCollection<Warehouse>(
                await _db.Warehouses.AsNoTracking().ToListAsync());

            Projects = new ObservableCollection<Project>(
                await _db.Projects.AsNoTracking().ToListAsync());

            TransportationCosts = new ObservableCollection<TransportationCost>(
                await _db.TransportationCosts.AsNoTracking().ToListAsync());
        }

        [RelayCommand]
        private void SolveTransportation()
        {
            Results.Clear();

            if (!Warehouses.Any() || !Projects.Any())
                return;

            // Определяем текущий сезон
            string currentSeason = GetCurrentSeason();

            // Фильтруем стоимости только для текущего сезона
            var costsForSeason = TransportationCosts
                .Where(c => c.Season == currentSeason)
                .ToList();

            if (!costsForSeason.Any())
                return;

            var solver = Solver.CreateSolver("SCIP");
            if (solver == null)
                return;

            // Переменные x[i,j]
            var x = new Dictionary<(int, int), Variable>();
            foreach (var w in Warehouses)
                foreach (var p in Projects)
                    x[(w.Id, p.Id)] = solver.MakeNumVar(0, double.PositiveInfinity, $"x_{w.Id}_{p.Id}");

            // Ограничения по складам
            foreach (var w in Warehouses)
            {
                var constraint = solver.MakeConstraint(0, (double)w.MaterialAmount);
                foreach (var p in Projects)
                    constraint.SetCoefficient(x[(w.Id, p.Id)], 1);
            }

            // Ограничения по объектам
            foreach (var p in Projects)
            {
                var constraint = solver.MakeConstraint((double)p.MaterialNeed, (double)p.MaterialNeed);
                foreach (var w in Warehouses)
                    constraint.SetCoefficient(x[(w.Id, p.Id)], 1);
            }

            // Целевая функция
            var objective = solver.Objective();
            foreach (var cost in costsForSeason)
                objective.SetCoefficient(x[(cost.WarehouseId, cost.ProjectId)], (double)cost.Cost);

            objective.SetMinimization();

            var status = solver.Solve();
            if (status != Solver.ResultStatus.OPTIMAL)
                return;

            // Формируем результат с учётом сезона
            foreach (var w in Warehouses)
            {
                foreach (var p in Projects)
                {
                    var value = x[(w.Id, p.Id)].SolutionValue();
                    if (value > 0.0001)
                    {
                        var cost = costsForSeason.First(c => c.WarehouseId == w.Id && c.ProjectId == p.Id);
                        Results.Add(new TransportationResult
                        {
                            Warehouse = w.Name,
                            Project = p.Name,
                            Amount = value,
                            Cost = value * (double)cost.Cost,
                            Season = currentSeason
                        });
                    }
                }
            }
        }

        // Метод для определения текущего сезона
        private string GetCurrentSeason()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Autumn",
                _ => "Winter"
            };
        }
    }
}
