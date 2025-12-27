using System;
using System.Collections.Generic;
using System.Text;
using static Chips.Emulator.Input;

namespace Chips
{
    public static class Emulator
    {
        public abstract record Input 
        {
            private Input() { }

            public sealed record ExecuteInstruction : Input 
            {
                private ExecuteInstruction() { }
                public static readonly ExecuteInstruction Instance = new();
            }

            public sealed record ElapseTimerTick : Input 
            {
                private ElapseTimerTick() { }
                public static readonly ElapseTimerTick Instance = new();
            }
        }

        public sealed record State(byte A, byte B, byte C);

        public static State Step(State state, Input input) => input switch 
        {
            ExecuteInstruction => state,
            ElapseTimerTick => state,
            _ => state,
        };
    }
}
