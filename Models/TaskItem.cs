namespace TaskHub.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime Deadline { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsOverdue => Status != Status.Done && Deadline < DateTime.Now;

    public override string ToString() =>
        $"[{Id}] {Title,-25} | {Priority,-6} | {Status,-10} | Дедлайн: {Deadline:dd.MM.yyyy HH:mm}";
}
