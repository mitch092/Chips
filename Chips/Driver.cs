using Silk.NET.Windowing;
using System.Diagnostics;

namespace Chips
{
    public class Driver
    {
        private readonly IWindow m_Window;
        private readonly Chip8OpenGLRenderer m_Renderer;
        private readonly Emulator m_Emulator;
        private readonly Stopwatch m_Stopwatch;

        private long m_InstructionsRemainder = 0;
        private long m_Chip8TimerTicksRemainder = 0;
        private long m_FramesRemainder = 0;

        private static long FramesPerSecond => 60;
        private static long Chip8TimerTicksPerSecond => 60;
        private static long InstructionsPerFrame => 10;
        private static long InstructionsPerSecond => InstructionsPerFrame * FramesPerSecond;

        public Driver(IWindow window, Chip8OpenGLRenderer renderer, Emulator emulator)
        {
            m_Window = window;
            m_Renderer = renderer;
            m_Emulator = emulator;
            m_Stopwatch = Stopwatch.StartNew();

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

            (long instructions, m_InstructionsRemainder) =
                DriverUtils.ConvertUnits(InstructionsPerSecond, Stopwatch.Frequency, deltaTicks, m_InstructionsRemainder);
            for (int i = 0; i < instructions; ++i)
            {
                // Execute instruction.

                (long chip8TimerTicks, m_Chip8TimerTicksRemainder) =
                    DriverUtils.ConvertUnits(Chip8TimerTicksPerSecond, InstructionsPerSecond, 1, m_Chip8TimerTicksRemainder);
                for (int j = 0; j < chip8TimerTicks; ++j)
                {
                    // Execute timer tick.
                }
            }

            (long frames, m_FramesRemainder) =
                DriverUtils.ConvertUnits(FramesPerSecond, Stopwatch.Frequency, deltaTicks, m_FramesRemainder);
            if (frames > 0)
            {
                // Render frame.
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
