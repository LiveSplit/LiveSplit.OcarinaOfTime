using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum Equipable : byte
    {
        //6E
        KokiriSword = 0x1,
        MasterSword = 0x2,
        BiggoronSword = 0x4,

        DekuShield = 0x10,
        HylianShield = 0x20,
        MirrorShield = 0x40,

        //6F
        KokiriTunic = 0x1,
        GoronTunic = 0x2,
        ZoraTunic = 0x4,

        KokiriBoots = 0x10,
        IronBoots = 0x20,
        HoverBoots = 0x40
    }
}
