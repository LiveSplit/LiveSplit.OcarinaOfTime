using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    public enum Inventory : byte
    {
        Width = 6,

        DekuSticks = 0 + 0 * Width,
        DekuNuts = 1 + 0 * Width,
        Bombs = 2 + 0 * Width,
        Bow = 3 + 0 * Width,
        FireArrows = 4 + 0 * Width,
        DinsFire = 6 + 0 * Width,

        Slingshot = 0 + 1 * Width,
        Ocarina = 1 + 1 * Width,
        Bombchus = 2 + 1 * Width,
        Hookshot = 3 + 1 * Width,
        IceArrows = 4 + 1 * Width,
        FaroresWind = 5 + 1 * Width,

        Boomerang = 0 + 2 * Width,
        LensOfTruth = 1 + 2 * Width,
        MagicBeans = 2 + 2 * Width,
        MegatonHammer = 3 + 2 * Width,
        LightArrows = 4 + 2 * Width,
        NayrusLove = 5 + 2 * Width,

        Bottle1 = 0 + 3 * Width,
        Bottle2 = 1 + 3 * Width,
        Bottle3 = 2 + 3 * Width,
        Bottle4 = 3 + 3 * Width,
        AdultTradeItem = 4 + 3 * Width,
        ChildTradeItem = 5 + 3 * Width
    }
}
