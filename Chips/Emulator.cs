namespace Chips
{
    public sealed record Emulator(byte A, byte B, byte C)
    {
        public Emulator ExecuteInstruction => this;
        public Emulator TickTimer => this;
    }
}
