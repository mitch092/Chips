using Silk.NET.Windowing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using static Chips.Scheduler;

namespace Chips
{
    public class Driver
    {
        private readonly IWindow m_Window;
        private readonly Chip8OpenGLRenderer m_Renderer;
        private readonly Stopwatch m_Stopwatch;
        private SchedulerState m_SchedulerState;
        private Emulator m_Emulator;

        private static long MaxDeltaTicks => (long)(Stopwatch.Frequency * 0.25);

        public Driver(IWindow window, Chip8OpenGLRenderer renderer)
        {
            m_Window = window;
            m_Renderer = renderer;
            m_Stopwatch = Stopwatch.StartNew();
            m_SchedulerState = SchedulerState.Initial;
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

            (IEnumerable<Event> events, m_SchedulerState) = Unfold2(m_SchedulerState.AddTicks(deltaTicks), SchedulerStep);
            m_Emulator = events.Aggregate(m_Emulator, Execute);
        }

        private Emulator Execute(Emulator state, Event evt) => evt switch 
        {
            Event.ExecuteInstruction => state.ExecuteInstruction,
            Event.TickTimer => state.TickTimer,
            Event.RenderFrame => Render(state),
            _ => state,
        };

        private static Emulator Render(Emulator state) 
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
