using System;

namespace LiveSplit.OcarinaOfTime
{
    [Flags]
    public enum MedallionsAndSongs : byte
    {
        None = 0x0,
        ForestMedallion = 0x1,
        FireMedallion = 0x2,
        WaterMedallion = 0x4,
        SpiritMedallion = 0x8,
        ShadowMedallion = 0x10,
        LightMedallion = 0x20,
        MinuetOfTheForest = 0x40,
        BoleroOfFire = 0x80
    }
}
