using TaskHub.Models;
using TaskHub.Services;

namespace TaskHub.UI;

public class ConsoleUI : IDisposable
{
    private readonly TaskManager<TaskItem> _manager = new();
    private readonly FileService _fileService = new();
    private readonly DeadlineChecker _checker = new(intervalSeconds: 15);
    private const string DataFile = "tasks.json";

    public ConsoleUI()
    {
        _manager.OnTaskChanged += msg =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[INFO] {msg}");
            Console.ResetColor();
        };

        _checker.OnOverdueTask += task =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[!] ПРОСРОЧЕНА: {task.Title} (дедлайн: {task.Deadline:dd.MM.yyyy HH:mm})");
            Console.ResetColor();
        };
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Добро пожаловать в TaskHub!\n");

        try
        {
            var loaded = await _fileService.LoadAsync(DataFile);
            _manager.LoadFrom(loaded);
            if (loaded.Count > 0)
                Console.WriteLine($"Загружено задач: {loaded.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки: {ex.Message}");
        }

        _checker.Start(() => _manager.GetAll());

        bool running = true;
        while (running)
        {
            ShowMenu();
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1": CreateTask(); break;
                    case "2": ViewTasks(); break;
                    case "3": EditTask(); break;
                    case "4": DeleteTask(); break;
                    case "5": SearchTasks(); break;
                    case "6": StatisticsService.PrintStatistics(_manager.GetAll()); break;
                    case "7": await SaveTasksAsync(); break;
                    case "8": await LoadTasksAsync(); break;
                    case "0": running = false; break;
                    default:
                        Console.WriteLine("Неверный выбор. Введите число от 0 до 8.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nОшибка: {ex.Message}");
                Console.ResetColor();
            }

            if (running)
            {
                Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                Console.ReadKey(true);
                Console.Clear();
            }
        }

        _checker.Stop();
        await SaveTasksAsync();
        Console.WriteLine("\nЗадачи сохранены. До свидания!");
    }

    private void ShowMenu()
    {
        Console.WriteLine("╔══════════════════════════════╗");
        Console.WriteLine("║         TaskHub Menu         ║");
        Console.WriteLine("╠══════════════════════════════╣");
        Console.WriteLine("║  1. Создать задачу           ║");
        Console.WriteLine("║  2. Просмотр задач           ║");
        Console.WriteLine("║  3. Редактировать задачу     ║");
        Console.WriteLine("║  4. Удалить задачу           ║");
        Console.WriteLine("║  5. Поиск задач              ║");
        Console.WriteLine("║  6. Статистика               ║");
        Console.WriteLine("║  7. Сохранить в файл         ║");
        Console.WriteLine("║  8. Загрузить из файла       ║");
        Console.WriteLine("║  0. Выход                    ║");
        Console.WriteLine("╚══════════════════════════════╝");
        Console.Write("Выберите пункт: ");
    }

    private void CreateTask()
    {
        Console.WriteLine("\n=== Создание задачи ===");

        Console.Write("Название: ");
        var title = Console.ReadLine() ?? "";
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название не может быть пустым.");

        Console.Write("Описание: ");
        var description = Console.ReadLine() ?? "";

        var priority = ReadEnum<Priority>("Приоритет (Low/Medium/High): ");
        var status = ReadEnum<Status>("Статус (New/InProgress/Done): ");

        Console.Write("Дедлайн (дд.мм.гггг чч:мм): ");
        if (!DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy HH:mm",
            null, System.Globalization.DateTimeStyles.None, out var deadline))
            throw new FormatException("Неверный формат даты. Используйте: дд.мм.гггг чч:мм");

        _manager.Add(new TaskItem
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = status,
            Deadline = deadline
        });
    }

    private void ViewTasks()
    {
        Console.WriteLine("\n=== Просмотр задач ===");
        Console.WriteLine("1. Все задачи");
        Console.WriteLine("2. Выполненные");
        Console.WriteLine("3. Невыполненные");
        Console.WriteLine("4. Высокий приоритет");
        Console.Write("Выбор: ");

        var tasks = Console.ReadLine() switch
        {
            "1" => _manager.GetAll(),
            "2" => _manager.GetCompleted(),
            "3" => _manager.GetPending(),
            "4" => _manager.GetHighPriority(),
            _ => _manager.GetAll()
        };

        PrintTaskList(tasks);
    }

    private void EditTask()
    {
        Console.WriteLine("\n=== Редактирование задачи ===");

        Console.Write("ID задачи: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
            throw new FormatException("ID должен быть числом.");

        var task = _manager.GetById(id)
            ?? throw new KeyNotFoundException($"Задача с ID {id} не найдена.");

        Console.WriteLine($"\nРедактируем: {task}");
        Console.WriteLine("1. Название");
        Console.WriteLine("2. Описание");
        Console.WriteLine("3. Приоритет");
        Console.WriteLine("4. Статус");
        Console.Write("Что изменить: ");

        switch (Console.ReadLine())
        {
            case "1":
                Console.Write($"Новое название [{task.Title}]: ");
                var title = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(title)) task.Title = title;
                break;
            case "2":
                Console.Write($"Новое описание [{task.Description}]: ");
                task.Description = Console.ReadLine() ?? task.Description;
                break;
            case "3":
                task.Priority = ReadEnum<Priority>("Новый приоритет (Low/Medium/High): ");
                break;
            case "4":
                task.Status = ReadEnum<Status>("Новый статус (New/InProgress/Done): ");
                break;
            default:
                Console.WriteLine("Отменено.");
                return;
        }

        Console.WriteLine("Задача обновлена.");
    }

    private void DeleteTask()
    {
        Console.WriteLine("\n=== Удаление задачи ===");

        Console.Write("ID задачи: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
            throw new FormatException("ID должен быть числом.");

        if (!_manager.Remove(id))
            throw new KeyNotFoundException($"Задача с ID {id} не найдена.");
    }

    private void SearchTasks()
    {
        Console.WriteLine("\n=== Поиск задач ===");
        Console.WriteLine("1. По названию");
        Console.WriteLine("2. По статусу");
        Console.WriteLine("3. По приоритету");
        Console.Write("Выбор: ");

        var results = Console.ReadLine() switch
        {
            "1" => SearchByTitle(),
            "2" => SearchByStatus(),
            "3" => SearchByPriority(),
            _ => new List<TaskItem>()
        };

        PrintTaskList(results);
    }

    private List<TaskItem> SearchByTitle()
    {
        Console.Write("Введите название (или часть): ");
        return _manager.SearchByTitle(Console.ReadLine() ?? "");
    }

    private List<TaskItem> SearchByStatus()
    {
        var status = ReadEnum<Status>("Статус (New/InProgress/Done): ");
        return _manager.SearchByStatus(status);
    }

    private List<TaskItem> SearchByPriority()
    {
        var priority = ReadEnum<Priority>("Приоритет (Low/Medium/High): ");
        return _manager.SearchByPriority(priority);
    }

    private async Task SaveTasksAsync()
    {
        await _fileService.SaveAsync(_manager.GetAll(), DataFile);
        Console.WriteLine("Задачи сохранены.");
    }

    private async Task LoadTasksAsync()
    {
        var tasks = await _fileService.LoadAsync(DataFile);
        _manager.LoadFrom(tasks);
        Console.WriteLine($"Загружено задач: {tasks.Count}");
    }

    private void PrintTaskList(List<TaskItem> tasks)
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine("\nЗадачи не найдены.");
            return;
        }

        Console.WriteLine($"\nНайдено задач: {tasks.Count}");
        Console.WriteLine(new string('─', 65));

        foreach (var task in tasks)
        {
            Console.ForegroundColor = task.IsOverdue ? ConsoleColor.Red
                : task.Status == Status.Done ? ConsoleColor.Green
                : task.Priority == Priority.High ? ConsoleColor.Yellow
                : ConsoleColor.White;

            Console.WriteLine(task);
            if (!string.IsNullOrWhiteSpace(task.Description))
                Console.WriteLine($"    {task.Description}");

            Console.ResetColor();
        }

        Console.WriteLine(new string('─', 65));
    }

    private static T ReadEnum<T>(string prompt) where T : struct, Enum
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine() ?? "";
            if (Enum.TryParse<T>(input, ignoreCase: true, out var result))
                return result;
            Console.WriteLine($"  Допустимые значения: {string.Join(", ", Enum.GetNames<T>())}");
        }
    }

    public void Dispose() => _fileService.Dispose();
}
