using Silk.NET.Windowing;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chips
{
    public class Driver
    {
        private readonly IWindow m_Window;
        private readonly OpenGLRenderer m_Renderer;
        private readonly Stopwatch m_Stopwatch;
        private readonly Scheduler<Chip8Event> m_Scheduler;
        private readonly Emulator m_Emulator;

        public Driver(IWindow window, OpenGLRenderer renderer)
        {
            m_Window = window;
            m_Renderer = renderer;
            m_Stopwatch = Stopwatch.StartNew();
            m_Scheduler = Scheduler<Chip8Event>.CreateChip8Scheduler();
            m_Emulator = new();

            m_Window.Load += OnLoad;
            m_Window.Update += OnUpdate;
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

        private void OnClose()
        {
            m_Window.Load -= OnLoad;
            m_Window.Update -= OnUpdate;
            m_Window.Closing -= OnClose;
        }
    }
}
