using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum Songs : byte
    {
        None = 0x0,
        SerenadeOfWater = 0x1,
        RequiemOfSpirit = 0x2,
        NocturneOfShadow = 0x4,
        PreludeOfLight = 0x8,
        ZeldasLullaby = 0x10,
        EponasSong = 0x20,
        SariasSong = 0x40,
        SunsSong = 0x80
    }
}