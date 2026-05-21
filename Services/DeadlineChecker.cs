using TaskHub.Models;

namespace TaskHub.Services;

public class DeadlineChecker
{
    private CancellationTokenSource? _cts;
    private readonly int _intervalSeconds;

    public delegate void OverdueNotificationHandler(TaskItem task);
    public event OverdueNotificationHandler? OnOverdueTask;

    public DeadlineChecker(int intervalSeconds = 15)
    {
        _intervalSeconds = intervalSeconds;
    }

    public void Start(Func<List<TaskItem>> getTasksFunc)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_intervalSeconds * 1000, token);

                    var overdue = getTasksFunc()
                        .Where(t => t.Status != Status.Done && t.Deadline < DateTime.Now)
                        .ToList();

                    foreach (var task in overdue)
                        OnOverdueTask?.Invoke(task);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void Stop() => _cts?.Cancel();
}
