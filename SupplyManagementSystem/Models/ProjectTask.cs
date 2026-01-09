using CommunityToolkit.Mvvm.ComponentModel;

namespace SupplyManagementSystem.Models;

public partial class ProjectTask : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Predecessors { get; set; } = string.Empty;
}