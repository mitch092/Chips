using Chips.Rendering;
using Silk.NET.Windowing;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chips
{
    public class Driver
    {
        private readonly IWindow m_Window;
        private readonly Stopwatch m_Stopwatch;
        private readonly Scheduler<Chip8Event> m_Scheduler;
        private readonly Emulator m_Emulator;
        private readonly Renderer m_Renderer;

        public Driver(IWindow window, Stopwatch stopwatch, Scheduler<Chip8Event> scheduler, Emulator emulator, Renderer renderer)
        {
            m_Window = window;
            m_Stopwatch = stopwatch;
            m_Scheduler = scheduler;
            m_Emulator = emulator;
            m_Renderer = renderer;
            m_Window.Load += OnLoad;
            m_Window.Update += OnUpdate;
            m_Window.Render += OnRender;
            m_Window.Closing += OnClose;
        }

        private void OnLoad()
        {
            m_Stopwatch.Restart();
        }

        private void OnUpdate(double obj)
        {
            long deltaTicks = m_Stopwatch.ElapsedTicks;
            m_Stopwatch.Restart();
            IEnumerable<Chip8Event> events = m_Scheduler.Advance(deltaTicks);
            foreach (Chip8Event chip8Event in events)
            {
                switch (chip8Event)
                {
                    case Chip8Event.ExecuteInstruction:
                        m_Emulator.ExecuteInstruction();
                        break;
                    case Chip8Event.TickTimer:
                        m_Emulator.TickTimer();
                        break;
                    case Chip8Event.RenderFrame:
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnRender(double obj)
        {
            var size = m_Renderer.Size;
            var fb = m_Renderer.Framebuffer;
            for (uint y = 0; y < size.Y; ++y) 
            {
                for (uint x = 0; x < size.X; ++x) 
                {
                    int ndx = (int)(y * size.X + x);
                    uint color = 0xFF000000u | (uint)(x * 255 / size.X) | ((uint)(y * 255 / size.Y) << 8);
                    fb[ndx] = color;
                }
            }
            m_Renderer.Present();
        }

        private void OnClose()
        {
            m_Window.Load -= OnLoad;
            m_Window.Update -= OnUpdate;
            m_Window.Closing -= OnClose;
        }
    }
}
