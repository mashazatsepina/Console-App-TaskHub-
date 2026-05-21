using TaskHub.Models;

namespace TaskHub.Services;

public class TaskManager<T> where T : TaskItem
{
    private readonly List<T> _tasks = new();
    private int _nextId = 1;

    public delegate void TaskChangedHandler(string message);
    public event TaskChangedHandler? OnTaskChanged;

    public void Add(T task)
    {
        task.Id = _nextId++;
        _tasks.Add(task);
        OnTaskChanged?.Invoke($"Задача '{task.Title}' добавлена.");
    }

    public bool Remove(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null) return false;
        _tasks.Remove(task);
        OnTaskChanged?.Invoke($"Задача '{task.Title}' удалена.");
        return true;
    }

    public T? GetById(int id) => _tasks.FirstOrDefault(t => t.Id == id);

    public List<T> GetAll() => new(_tasks);

    public List<T> GetCompleted() => _tasks.Where(t => t.Status == Status.Done).ToList();

    public List<T> GetPending() => _tasks.Where(t => t.Status != Status.Done).ToList();

    public List<T> GetHighPriority() => _tasks.Where(t => t.Priority == Priority.High).ToList();

    public List<T> SearchByTitle(string title) =>
        _tasks.Where(t => t.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<T> SearchByStatus(Status status) => _tasks.Where(t => t.Status == status).ToList();

    public List<T> SearchByPriority(Priority priority) => _tasks.Where(t => t.Priority == priority).ToList();

    public List<T> GetOverdue() =>
        _tasks.Where(t => t.Status != Status.Done && t.Deadline < DateTime.Now).ToList();

    public void LoadFrom(List<T> tasks)
    {
        _tasks.Clear();
        _tasks.AddRange(tasks);
        _nextId = tasks.Count > 0 ? tasks.Max(t => t.Id) + 1 : 1;
    }
}
