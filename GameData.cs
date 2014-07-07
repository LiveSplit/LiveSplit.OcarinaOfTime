using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LiveSplit.OcarinaOfTime
{
    [StructLayout(LayoutKind.Explicit)]
    public struct GameData
    {
        [FieldOffset(0x0)]
        public Entrance Entrance;

        [FieldOffset(0x4)]
        public bool IsChild;

        [FieldOffset(0x8)]
        public Cutscene Cutscene;

        [FieldOffset(0x20)]
        public ushort Deaths;

        [FieldOffset(0x2C)]
        public byte HeartContainers;

        [FieldOffset(0x30)]
        public byte Magic;

        [FieldOffset(0x32)]
        public ushort Hearts;

        [FieldOffset(0x36)]
        public ushort Rupees;

        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x3F)]
        public bool HasDoubleMagic;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        [FieldOffset(0x74)]
        public Item[] Inventory;

        [FieldOffset(0x9E)]
        public SwordsAndShields SwordsAndShields;
        [FieldOffset(0x9F)]
        public TunicsAndBoots TunicsAndBoots;

        [FieldOffset(0xA4)]
        public MedallionsAndSongs MedallionsAndSongs;
        [FieldOffset(0xA5)]
        public Songs Songs;
        [FieldOffset(0xA6)]
        public SongsAndEmeralds SongsAndEmeralds;

        [FieldOffset(0xA7)]
        public byte HeartPieces;
    }
}
