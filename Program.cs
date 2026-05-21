using System.Text;
using TaskHub.UI;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "TaskHub";

using var ui = new ConsoleUI();
await ui.RunAsync();
