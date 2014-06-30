using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    public enum Item : byte
    {
        Bombs = 0x02,
        Bow = 0x03,
        Slingshot = 0x06,
        Bombchus = 0x09,
        Hookshot = 0x0A,
        IceArrows = 0x0C,
        FaroresWind = 0x0D,
        EmptyBottle = 0x14,
        EyeBallFrog = 0x35,
        None = 0xFF
    }
}
