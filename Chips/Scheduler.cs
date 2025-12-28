using Silk.NET.OpenGL;
using System;
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
            while (deltaTicks > 0) 
            {
                IEnumerable<ScheduledEvent> nextEvents = m_EventStreams
                    .Select(s => new ScheduledEvent(s, ticksPerSecond))
                    .Where(s => s.MinTicksUntil <= deltaTicks);
                if (nextEvents.Any()) 
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
    }
    //public static class Scheduler
    //{
    //    public enum Event
    //    {
    //        ExecuteInstruction,
    //        TickTimer,
    //        RenderFrame
    //    }

    //    public static Dictionary<Event, long> EventRates { get; } = new()
    //    {
    //        // 600 instructions per second
    //        [Event.ExecuteInstruction] = 600,
    //        // 60 CHIP-8 timer ticks per second
    //        [Event.TickTimer] = 60,
    //        // 60 frames per second
    //        [Event.RenderFrame] = 60,
    //    };

    //    public record SchedulerState(long RemainingTicks, Dictionary<Event, long> Phase)
    //    {
    //        public static SchedulerState Initial => new(0, Enum.GetValues<Event>().ToDictionary(key => key, key => (long)0));
    //        public SchedulerState AddTicks(long ticks) => this with { RemainingTicks = RemainingTicks + ticks };
    //        public SchedulerState RemoveTicks(long ticks) => this with { RemainingTicks = RemainingTicks - ticks };
    //    }

    //    public static IEnumerable<(T, TState)> Unfold<T, TState>(TState initial, Func<TState, (T item, TState next)?> step)
    //    {
    //        var state = initial;
    //        (T item, TState next)? res;
    //        while ((res = step(state)) != null)
    //        {
    //            yield return res.Value;
    //            state = res.Value.next;
    //        }
    //    }

    //    public static (IEnumerable<T>, TState) Unfold2<T, TState>(TState initial, Func<TState, (T item, TState next)?> step)
    //    {
    //        var output = Unfold(initial, step);
    //        return (output.Select(item => item.Item1), output.Last().Item2);
    //    }

    //    public static (Event, SchedulerState)? SchedulerStep(IReadOnlyDictionary<Event, long> rates, long ticksPerSecond, SchedulerState state)
    //    {
    //        Event? nextEvent = null;
    //        long minTicks = long.MaxValue;

    //        foreach ((Event evt, long rate) in rates)
    //        {
    //            long phase = state.Phase[evt];
    //            long ticksUntil = (ticksPerSecond - phase + rate - 1) / rate;
    //            if (ticksUntil <= state.RemainingTicks && ticksUntil < minTicks)
    //            {
    //                minTicks = ticksUntil;
    //                nextEvent = evt;
    //            }
    //        }

    //        if (nextEvent is null)
    //        {
    //            return null;
    //        }

    //        foreach ((Event evt, long rate) in rates)
    //        {
    //            state.Phase[evt] += minTicks * rate;
    //        }
    //        state.Phase[nextEvent.Value] -= ticksPerSecond;

    //        return (nextEvent.Value, state.RemoveTicks(minTicks));
    //    }

    //    public static (Event, SchedulerState)? SchedulerStep(SchedulerState state) => SchedulerStep(EventRates, Stopwatch.Frequency, state);
    //}
}
