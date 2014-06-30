using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    public enum Offset : int
    {
        EntranceID = 0x60,
        CutsceneID = 0x68,
        HeartContainers = 0x8C,
        Inventory = 0xD4,
        SwordsAndShields = 0xFE,
        BootsAndTunics = 0xFF,
        SongsAndMedallions = 0x104,
        Songs = 0x105,
        SongsAndEmeralds = 0x106,
    }
}
