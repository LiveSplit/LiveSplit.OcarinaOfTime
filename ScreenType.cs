using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum ScreenType : byte
    {
        None = 0x0,
        TitleScreen = 0x1,
        FileSelect = 0x2
    }
}
