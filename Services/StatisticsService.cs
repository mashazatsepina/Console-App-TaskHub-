using TaskHub.Models;

namespace TaskHub.Services;

public static class StatisticsService
{
    public static int GetTotal(List<TaskItem> tasks) => tasks.Count;

    public static int GetCompletedCount(List<TaskItem> tasks) =>
        tasks.Count(t => t.Status == Status.Done);

    public static int GetOverdueCount(List<TaskItem> tasks) =>
        tasks.Count(t => t.Status != Status.Done && t.Deadline < DateTime.Now);

    public static Dictionary<Priority, int> GetByPriority(List<TaskItem> tasks) =>
        tasks.GroupBy(t => t.Priority)
             .ToDictionary(g => g.Key, g => g.Count());

    public static void PrintStatistics(List<TaskItem> tasks)
    {
        Console.WriteLine("\n=== Статистика ===");
        Console.WriteLine($"Всего задач:   {GetTotal(tasks)}");
        Console.WriteLine($"Выполнено:     {GetCompletedCount(tasks)}");
        Console.WriteLine($"Просрочено:    {GetOverdueCount(tasks)}");
        Console.WriteLine("\nПо приоритетам:");

        var byPriority = GetByPriority(tasks);
        foreach (Priority p in Enum.GetValues<Priority>())
        {
            int count = byPriority.TryGetValue(p, out var val) ? val : 0;
            Console.WriteLine($"  {p,-8}: {count}");
        }
    }
}
