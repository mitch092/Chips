using Silk.NET.Windowing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chips
{
    public class Driver
    {
        private readonly IWindow m_Window;
        private readonly OpenGLRenderer m_Renderer;
        private readonly Stopwatch m_Stopwatch;
        private readonly Scheduler<Chip8Event> m_Scheduler;
        private Emulator m_Emulator;

        private static long MaxDeltaTicks => (long)(Stopwatch.Frequency * 0.25);

        public Driver(IWindow window, OpenGLRenderer renderer)
        {
            m_Window = window;
            m_Renderer = renderer;
            m_Stopwatch = Stopwatch.StartNew();
            m_Scheduler = Scheduler<Chip8Event>.CreateChip8Scheduler();
            m_Emulator = new(0, 0, 0);

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
            if (deltaTicks > MaxDeltaTicks)
            {
                deltaTicks = MaxDeltaTicks;
            }
            IEnumerable<Chip8Event> events = m_Scheduler.Advance(deltaTicks);
            m_Emulator = events.Aggregate(m_Emulator, Execute);
        }

        private Emulator Execute(Emulator state, Chip8Event evt) => evt switch
        {
            Chip8Event.ExecuteInstruction => state.ExecuteInstruction,
            Chip8Event.TickTimer => state.TickTimer,
            Chip8Event.RenderFrame => Render(state),
            _ => state,
        };

        private Emulator Render(Emulator state)
        {
            // Render frame
            return state;
        }

        private void OnClose()
        {
            m_Window.Load -= OnLoad;
            m_Window.Update -= OnUpdate;
            m_Window.Closing -= OnClose;
        }
    }
}
