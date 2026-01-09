using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IronPython.Hosting;
using Microsoft.EntityFrameworkCore;
using SupplyManagementSystem.Data;
using SupplyManagementSystem.Models;

namespace SupplyManagementSystem.ViewModels
{
    public partial class NetworkGraphTabViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext = new AppDbContext();

        public AvaloniaList<ProjectTask> Tasks { get; } = new AvaloniaList<ProjectTask>();
        public AvaloniaList<NetworkCalculationResult> Results { get; } = new AvaloniaList<NetworkCalculationResult>();

        [ObservableProperty]
        private ProjectTask? _selectedTask;

        public NetworkGraphTabViewModel()
        {
            LoadTasksCommand.Execute(null);
        }

        [RelayCommand]
        private async void LoadTasks()
        {
            Tasks.Clear();
            Results.Clear();

            var tasksFromDb = await _dbContext.ProjectTasks.ToListAsync();
            foreach (var t in tasksFromDb)
                Tasks.Add(t);

            if (Tasks.Any())
                CalculateNetworkIronPython();
        }

        private void CalculateNetworkIronPython()
        {
            var pyEngine = Python.CreateEngine();
            var pyScope = pyEngine.CreateScope();

            // Подготовка данных для Python: список кортежей (id, name, duration, predecessors)
            var pyTasks = Tasks.Select(t => new object[]
            {
                t.Id,
                t.Name,
                t.Duration,
                string.IsNullOrWhiteSpace(t.Predecessors) ? new int[0] : t.Predecessors.Split(',').Select(int.Parse).ToArray()
            }).ToArray();

            pyScope.SetVariable("tasks", pyTasks);

            string pyScript = @"
def cpm(tasks):
    # tasks: [id, name, duration, predecessors[]]

    ids = [t[0] for t in tasks]
    duration = {t[0]: t[2] for t in tasks}
    preds = {t[0]: list(t[3]) for t in tasks}

    # successors
    succs = {i: [] for i in ids}
    for i in ids:
        for p in preds[i]:
            succs[p].append(i)

    # --- Topological sort ---
    indegree = {i: len(preds[i]) for i in ids}
    queue = [i for i in ids if indegree[i] == 0]
    topo = []

    while queue:
        n = queue.pop(0)
        topo.append(n)
        for s in succs[n]:
            indegree[s] -= 1
            if indegree[s] == 0:
                queue.append(s)

    # --- Forward pass (ES, EF) ---
    ES = {i: 0 for i in ids}
    EF = {}

    for i in topo:
        ES[i] = max([EF[p] for p in preds[i]] or [0])
        EF[i] = ES[i] + duration[i]

    project_duration = max(EF.values())

    # --- Backward pass (LS, LF) ---
    LF = {i: project_duration for i in ids}
    LS = {}

    for i in reversed(topo):
        if succs[i]:
            LF[i] = min(LS[s] for s in succs[i])
        LS[i] = LF[i] - duration[i]

    # --- Floats ---
    results = []
    for i in ids:
        total_float = LS[i] - ES[i]
        free_float = (
            min(ES[s] for s in succs[i]) - EF[i]
            if succs[i] else
            project_duration - EF[i]
        )

        results.append({
            ""Id"": i,
            ""Name"": next(t[1] for t in tasks if t[0] == i),
            ""Duration"": duration[i],
            ""ES"": ES[i],
            ""EF"": EF[i],
            ""LS"": LS[i],
            ""LF"": LF[i],
            ""TotalFloat"": total_float,
            ""FreeFloat"": free_float,
            ""IsCritical"": total_float == 0
        })

    return results

results = cpm(tasks)
";

            pyEngine.Execute(pyScript, pyScope);
            dynamic pyResults = pyScope.GetVariable("results");

            Results.Clear();
            foreach (var r in pyResults)
            {
                Results.Add(new NetworkCalculationResult
                {
                    Id = (int)r["Id"],
                    Name = (string)r["Name"],
                    Duration = (int)r["Duration"],
                    EarliestStart = (int)r["ES"],
                    EarliestFinish = (int)r["EF"],
                    LatestStart = (int)r["LS"],
                    LatestFinish = (int)r["LF"],
                    TotalFloat = (int)r["TotalFloat"],
                    FreeFloat = (int)r["FreeFloat"],
                    IsCritical = (bool)r["IsCritical"]
                });

            }
        }
    }

    public class NetworkCalculationResult : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int EarliestStart { get; set; }
        public int EarliestFinish { get; set; }
        public int LatestStart { get; set; }
        public int LatestFinish { get; set; }
        public int TotalFloat { get; set; }
        public int FreeFloat { get; set; }
        public bool IsCritical { get; set; }
    }
}
