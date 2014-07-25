using System;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum TunicsAndBoots : byte
    {
        None = 0x0,
        KokiriTunic = 0x1,
        GoronTunic = 0x2,
        ZoraTunic = 0x4,
        KokiriBoots = 0x10,
        IronBoots = 0x20,
        HoverBoots = 0x40
    }
}
