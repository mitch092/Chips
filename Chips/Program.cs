using Chips.Rendering;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace Chips
{
    public class Program
    {
        static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default with 
            {
                Size = new(800, 600),
                Title = "Chips"
            };
            IWindow window = Window.Create(options);

            Renderer renderer = new(window, new(800, 600));
            Stopwatch stopwatch = Stopwatch.StartNew();
            Scheduler<Chip8Event> scheduler = Scheduler<Chip8Event>.CreateChip8Scheduler();
            Emulator emulator = new();
            Driver driver = new(window, stopwatch, scheduler, emulator, renderer);

            window.Run();
        }
    }
}
