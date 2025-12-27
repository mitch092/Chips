using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chips
{
    public static class Scheduler
    {
        public enum Event
        {
            ExecuteInstruction,
            TickTimer,
            RenderFrame
        }

        public static Dictionary<Event, long> EventRates { get; } = new()
        {
            // 600 instructions per second
            [Event.ExecuteInstruction] = 600,
            // 60 CHIP-8 timer ticks per second
            [Event.TickTimer] = 60,
            // 60 frames per second
            [Event.RenderFrame] = 60,
        };

        public record SchedulerState(long RemainingTicks, Dictionary<Event, long> Phase)
        {
            public static SchedulerState Initial =>
                new(0, new() { [Event.ExecuteInstruction] = 0, [Event.TickTimer] = 0, [Event.RenderFrame] = 0 });
            public SchedulerState AddTicks(long ticks) => this with { RemainingTicks = RemainingTicks + ticks };
            public SchedulerState RemoveTicks(long ticks) => this with { RemainingTicks = RemainingTicks - ticks };
        }

        public static IEnumerable<(T, TState)> Unfold<T, TState>(TState initial, Func<TState, (T item, TState next)?> step)
        {
            var state = initial;
            (T item, TState next)? res;
            while ((res = step(state)) != null)
            {
                yield return res.Value;
                state = res.Value.next;
            }
        }

        public static (IEnumerable<T>, TState) Unfold2<T, TState>(TState initial, Func<TState, (T item, TState next)?> step) 
        {
            var output = Unfold(initial, step);
            return (output.Select(item => item.Item1), output.Last().Item2);
        }

        public static (Event, SchedulerState)? SchedulerStep(IReadOnlyDictionary<Event, long> rates, long ticksPerSecond, SchedulerState state)
        {
            Event? nextEvent = null;
            long minTicks = long.MaxValue;

            foreach ((Event evt, long rate) in rates)
            {
                long phase = state.Phase[evt];
                long ticksUntil = (ticksPerSecond - phase + rate - 1) / rate;
                if (ticksUntil <= state.RemainingTicks && ticksUntil < minTicks)
                {
                    minTicks = ticksUntil;
                    nextEvent = evt;
                }
            }

            if (nextEvent is null)
            {
                return null;
            }

            foreach ((Event evt, long rate) in rates)
            {
                state.Phase[evt] += minTicks * rate;
            }
            state.Phase[nextEvent.Value] -= ticksPerSecond;

            return (nextEvent.Value, state.RemoveTicks(minTicks));
        }

        public static (Event, SchedulerState)? SchedulerStep(SchedulerState state) => SchedulerStep(EventRates, Stopwatch.Frequency, state);
    }
}
