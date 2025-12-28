using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chips
{
    public sealed class Scheduler<TEvent>(long ticksPerSecond, IEnumerable<(TEvent evt, long rate)> events) where TEvent : notnull
    {
        private readonly List<EventStream> m_EventStreams = events.Select(item => new EventStream(item.evt, item.rate)).ToList();

        public IEnumerable<TEvent> Advance(long deltaTicks)
        {
            IEnumerable<ScheduledEvent> nextEvents;
            while (deltaTicks > 0 && (nextEvents = m_EventStreams.Select(s => new ScheduledEvent(s, ticksPerSecond)).Where(s => s.MinTicksUntil <= deltaTicks)).Any())
            {
                ScheduledEvent nextScheduledEvent = nextEvents.MinBy(s => s.MinTicksUntil);
                for (int i = 0; i < m_EventStreams.Count; ++i)
                {
                    m_EventStreams[i].Phase += nextScheduledEvent.MinTicksUntil * m_EventStreams[i].Rate;
                }
                nextScheduledEvent.EventStream.Phase -= ticksPerSecond;
                deltaTicks -= nextScheduledEvent.MinTicksUntil;
                yield return nextScheduledEvent.EventStream.Event;
            }
        }

        private struct ScheduledEvent(EventStream eventStream, long ticksPerSecond)
        {
            public EventStream EventStream = eventStream;
            public readonly long MinTicksUntil = (ticksPerSecond - eventStream.Phase + eventStream.Rate - 1) / eventStream.Rate;
        }

        private class EventStream(TEvent evt, long rate)
        {
            public readonly TEvent Event = evt;
            public readonly long Rate = rate;
            public long Phase = 0;
        }

        public static Scheduler<Chip8Event> CreateChip8Scheduler() =>
            new(Stopwatch.Frequency, [(Chip8Event.ExecuteInstruction, 600), (Chip8Event.TickTimer, 60), (Chip8Event.RenderFrame, 60)]);
    }

    public enum Chip8Event
    {
        // 600 instructions per second
        ExecuteInstruction,
        // 60 CHIP-8 timer ticks per second
        TickTimer,
        // 60 frames per second
        RenderFrame
    }
}
