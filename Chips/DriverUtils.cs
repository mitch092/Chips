using System;

namespace Chips
{
    public static class DriverUtils
    {
        public static (long TargetUnits, long Remainder) ConvertUnits(
            long targetUnitsPerBaseUnit, long sourceUnitsPerBaseUnit, long sourceUnits, long remainder)
        {
            long val = remainder + sourceUnits * targetUnitsPerBaseUnit;
            long targetUnits = Math.DivRem(val, sourceUnitsPerBaseUnit, out long newRemainder);
            return (targetUnits, newRemainder);
        }
    }
}
