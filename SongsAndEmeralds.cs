using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum SongsAndEmeralds : byte
    {
        None = 0x0,
        SongOfTime = 0x1,
        SongOfStorms = 0x2,
        KokirisEmerald = 0x4,
        GoronsRuby = 0x8,
        ZorasSapphire = 0x10,
        StoneOfAgony = 0x20,
        GerudoMemberShipCard = 0x40,
        HasCollectedAGoldSkulltula = 0x80
    }
}
