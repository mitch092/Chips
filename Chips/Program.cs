using Silk.NET.Windowing;

namespace Chips
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WindowOptions options = WindowOptions.Default with 
            {
                Size = new(800, 600),
                Title = "Chips"
            };

            IWindow window = Window.Create(options);
            window.Run();
        }
    }
}
