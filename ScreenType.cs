using System;

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
