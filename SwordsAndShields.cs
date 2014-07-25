using System;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum SwordsAndShields : byte
    {
        None = 0x0,
        KokiriSword = 0x1,
        MasterSword = 0x2,
        BiggoronSword = 0x4,
        DekuShield = 0x10,
        HylianShield = 0x20,
        MirrorShield = 0x40
    }
}
