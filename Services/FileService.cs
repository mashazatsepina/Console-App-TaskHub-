using System.Text.Json;
using TaskHub.Models;

namespace TaskHub.Services;

public class FileService : IDisposable
{
    private StreamWriter? _logWriter;
    private bool _disposed;

    public FileService(string logPath = "taskhub.log")
    {
        _logWriter = new StreamWriter(logPath, append: true);
    }

    public async Task SaveAsync(List<TaskItem> tasks, string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(tasks, options);
        await File.WriteAllTextAsync(filePath, json);
        await LogAsync($"Сохранено {tasks.Count} задач в {filePath}");
    }

    public async Task<List<TaskItem>> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<TaskItem>();

        var json = await File.ReadAllTextAsync(filePath);
        var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        await LogAsync($"Загружено {tasks.Count} задач из {filePath}");
        return tasks;
    }

    private async Task LogAsync(string message)
    {
        if (_logWriter is not null)
        {
            await _logWriter.WriteLineAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            await _logWriter.FlushAsync();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logWriter?.Dispose();
            _logWriter = null;
            _disposed = true;
        }
    }
}
