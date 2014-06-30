using LiveSplit.Model;
using LiveSplit.OcarinaOfTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.ASL
{
    public class OcarinaOfTimeScript
    {
        protected TimerModel Model { get; set; }
        protected Process Game { get; set; }
        public ASLState OldState { get; set; }
        public ASLState State { get; set; }

        public OcarinaOfTimeScript()
        {
            State = new ASLState();
        }

        protected void TryConnect()
        {
            if (Game == null || Game.HasExited)
            {
                Game = null;
                var process = Process.GetProcessesByName("Project64").FirstOrDefault();
                if (process != null)
                {
                    Game = process;
                    RebuildProject64();
                    State.RefreshValues(Game);
                    OldState = State;
                }
                else
                {
                    process = Process.GetProcessesByName("mupen64").FirstOrDefault();
                    if (process != null)
                    {
                        Game = process;
                        RebuildMupen();
                        State.RefreshValues(Game);
                        OldState = State;
                    }
                }
            }
        }

        private void RebuildMupen()
        {
            ProcessModule module = Game.MainModule;

            Rebuild(0x46d010 - ((int)module.BaseAddress));
        }

        private void RebuildProject64()
        {
            String version;
            new DeepPointer(0x6aba2).Deref(Game, out version, 3);

            if (version == "1.6")
                Rebuild(0x000D6A1C);
            else
                Rebuild(0x001002FC);
        }

        private void Rebuild(int _base)
        {
            State.ValueDefinitions.Clear();

            AddPointer("byte400", "Data", _base, 0x11a570);
            AddPointer("int", "GameFrames", _base, 0x11f568);
            AddPointer("byte", "SceneID", _base, 0x1c8546);
            AddPointer("sbyte", "GohmasHealth", _base, 0x1e840c);
            AddPointer("sbyte", "GanonsHealth", _base, 0x1fa2dc);
            AddPointer("short", "GanonsAnimation", _base, 0x1fa374);
            AddPointer("short", "DialogID", _base, 0x1d8872);
            AddPointer("byte", "IsOnTitleScreenOrFileSelect", _base, 0x11b92c);
            AddPointer("float", "X", _base, 0x1c8714);
            AddPointer("float", "Y", _base, 0x1c8718);
            AddPointer("float", "Z", _base, 0x1c871c);
        }

        private void AddPointer(String type, String name, int _base, params int[] offsets)
        {
            State.ValueDefinitions.Add(new ASLValueDefinition()
                {
                    Type = type,
                    Identifier = name,
                    Pointer = new DeepPointer(_base, offsets)
                });
        }

        public void Update(LiveSplitState lsState)
        {
            if (Game != null && !Game.HasExited)
            {
                OldState = State.RefreshValues(Game);

                if (lsState.CurrentPhase == TimerPhase.NotRunning)
                {
                    if (Start(lsState, OldState.Data, State.Data))
                    {
                        Model.Start();
                    }
                }
                else if (lsState.CurrentPhase == TimerPhase.Running || lsState.CurrentPhase == TimerPhase.Paused)
                {
                    if (Reset(lsState, OldState.Data, State.Data))
                    {
                        Model.Reset();
                        return;
                    }
                    else if (Split(lsState, OldState.Data, State.Data))
                    {
                        Model.Split();
                    }

                    var isLoading = IsLoading(lsState, OldState.Data, State.Data);
                    if (isLoading != null)
                        lsState.IsLoading = isLoading;

                    var gameTime = GameTime(lsState, OldState.Data, State.Data);
                    if (gameTime != null)
                        lsState.SetGameTime(gameTime);
                }
            }
            else
            {
                if (Model == null)
                {
                    Model = new TimerModel() { CurrentState = lsState };
                }
                TryConnect();
            }
        }

        public bool Start(LiveSplitState timer, dynamic old, dynamic current)
        {
            //Set Start Up State
            current.GameTime = TimeSpan.Zero;
            current.AccumulatedFrames = -current.GameFrames;

            current.EntranceID = 0xFFFF;
            current.CutsceneID = 0x0;

            current.EyeBallFrogCount =
            current.LastActualDialog =
            current.LastActualDialogTime =
                0;

            current.HasSword =
            current.HasBottle =
            current.HasIceArrows =
            current.HasFaroresWind =
            current.HasSlingshot =
            current.HasBombchus =
            current.HasHookshot =
            current.HasIronBoots =
            current.HasHoverBoots =
            current.HasSongOfStorms =
            current.HasBombs =
            current.HasBoleroOfFire =
            current.HasRequiemOfSpirit =
            current.HasEyeBallFrog =
            current.HasMasterSword =
            current.HasBiggoronSword =
            current.HasBow =
            current.IsInFairyOcarinaCutscene =
            current.DidMidoSkip =
                false;

            //Check for Timer Start
            return (old.SceneID == 0x17 || old.SceneID == 0x18)
                && !(current.SceneID == 0x17 || current.SceneID == 0x18);
        }

        public bool Split(LiveSplitState timer, dynamic old, dynamic current)
        {
            //Functions
            Func<byte, byte, bool> check = (x, y) => (x & y) != 0x0;
            Func<int, byte> readFixed = x => current.Data[x ^ 0x3];
            Func<Inventory, Item> getInventoryItem = slot => (Item)readFixed((int)Offset.Inventory + (int)slot);
            Func<Entrance> getEntrance = () => (Entrance)(current.Data[Offset.EntranceID + 1] << 8 | current.Data[Offset.EntranceID]);
            Func<Cutscene> getCutscene = () => (Cutscene)(current.Data[Offset.CutsceneID + 1] << 8 | current.Data[Offset.CutsceneID]);
            Func<Entrance, Entrance, bool> checkEntrance = (x, y) => ((short)x | 0x3) == ((short)y | 0x3);

            //Check for Split
            var segment = timer.CurrentSplit.Name.ToLower();

            //Don't split on Title Screen or File Select because
            //Title Screen Link and Third File Link are loaded
            //and they might cause some splits to happen
            if (current.IsOnTitleScreenOrFileSelect != 0x0)
            {
                //TODO Except some segments
                return false;
            }

            if (segment == "sword" || segment == "kokiri sword")
            {
                var swordsAndShieldsUnlocked = (Equipable)current.Data[Offset.SwordsAndShields];
                current.HasSword = swordsAndShieldsUnlocked.HasFlag(Equipable.KokiriSword);
                return !old.HasSword && current.HasSword;
            }
            else if (segment == "master sword")
            {
                var swordsAndShieldsUnlocked = (Equipable)current.Data[Offset.SwordsAndShields];
                current.HasMasterSword = swordsAndShieldsUnlocked.HasFlag(Equipable.MasterSword);
                return !old.HasMasterSword && current.HasMasterSword;
            }
            else if (segment == "biggoron sword" || segment == "biggoron's sword")
            {
                var swordsAndShieldsUnlocked = (Equipable)current.Data[Offset.SwordsAndShields];
                current.HasBiggoronSword = swordsAndShieldsUnlocked.HasFlag(Equipable.BiggoronSword);
                return !old.HasBiggoronSword && current.HasBiggoronSword;
            }
            else if (segment == "hover boots")
            {
                var bootsAndTunicsUnlocked = (Equipable)current.Data[Offset.BootsAndTunics];
                current.HasHoverBoots = bootsAndTunicsUnlocked.HasFlag(Equipable.HoverBoots);
                return !old.HasHoverBoots && current.HasHoverBoots;
            }
            else if (segment == "iron boots")
            {
                var bootsAndTunicsUnlocked = (Equipable)current.Data[Offset.BootsAndTunics];
                current.HasIronBoots = bootsAndTunicsUnlocked.HasFlag(Equipable.IronBoots);
                return !old.HasIronBoots && current.HasIronBoots;
            }
            else if (segment == "song of storms")
            {
                var songsAndEmeraldsUnlocked = (Song)current.Data[Offset.SongsAndEmeralds];
                current.HasSongOfStorms = songsAndEmeraldsUnlocked.HasFlag(Song.SongOfStorms);
                return old.DialogID == Dialog.SongOfStorms
                    && current.DialogID == Dialog.None
                    && current.HasSongOfStorms;
            }
            else if (segment == "bolero of fire")
            {
                var songsAndMedallionsUnlocked = (Song)current.Data[Offset.SongsAndMedallions];
                current.HasBoleroOfFire = songsAndMedallionsUnlocked.HasFlag(Song.BoleroOfFire);
                return !old.HasBoleroOfFire && current.HasBoleroOfFire;
            }
            else if (segment == "requiem of spirit")
            {
                var songsUnlocked = (Song)current.Data[Offset.Songs];
                current.HasRequiemOfSpirit = songsUnlocked.HasFlag(Song.RequiemOfSpirit);
                return old.DialogID == Dialog.RequiemOfSpirit
                    && current.DialogID == Dialog.None
                    && current.HasRequiemOfSpirit;
            }
            else if (segment == "bottle")
            {
                current.HasBottle = getInventoryItem(Inventory.Bottle1) == Item.EmptyBottle;
                return !old.HasBottle && current.HasBottle;
            }
            else if (segment.StartsWith("ice arrow"))
            {
                current.HasIceArrows = getInventoryItem(Inventory.IceArrows) == Item.IceArrows;
                return !old.HasIceArrows && current.HasIceArrows;
            }
            else if (segment.StartsWith("farore's wind"))
            {
                current.HasFaroresWind = getInventoryItem(Inventory.FaroresWind) == Item.FaroresWind;
                return !old.HasFaroresWind && current.HasFaroresWind;
            }
            else if (segment.EndsWith("bow"))
            {
                current.HasBow = getInventoryItem(Inventory.Bow) == Item.Bow;
                return !old.HasBow && current.HasBow;
                //TODO Test
            }
            else if (segment.EndsWith("frog"))
            {
                current.HasEyeBallFrog = getInventoryItem(Inventory.AdultTradeItem) == Item.EyeBallFrog;
                if (!old.HasEyeBallFrog && current.HasEyeBallFrog)
                    current.EyeBallFrogCount++;
                return current.EyeBallFrogCount == 2 && old.EyeBallFrogCount < 2;
            }
            else if (segment == "slingshot")
            {
                current.HasSlingshot = getInventoryItem(Inventory.Slingshot) == Item.Slingshot;
                return !old.HasSlingshot && current.HasSlingshot;
            }
            else if (segment == "bombchus")
            {
                current.HasBombchus = getInventoryItem(Inventory.Bombchus) == Item.Bombchus;
                return !old.HasBombchus && current.HasBombchus;
            }
            else if (segment == "hookshot")
            {
                current.HasHookshot = getInventoryItem(Inventory.Hookshot) == Item.Hookshot;
                return !old.HasHookshot && current.HasHookshot;
            }
            else if (segment == "bombs")
            {
                current.HasBombs = getInventoryItem(Inventory.Bombs) == Item.Bombs;
                return !old.HasBombs && current.HasBombs;
            }
            else if (segment == "forest escape" || segment == "escape")
            {
                current.EntranceID = getEntrance();
                current.CutsceneID = getCutscene();

                var escapedToRiver =
                    !checkEntrance(old.EntranceID, Entrance.ForestToRiver)
                    && checkEntrance(current.EntranceID, Entrance.ForestToRiver);

                current.IsInFairyOcarinaCutscene =
                    checkEntrance(current.EntranceID, Entrance.BridgeBetweenFieldAndForest)
                    && current.CutsceneID == Cutscene.FairyOcarina;

                var escapedToSaria =
                    !old.IsInFairyOcarinaCutscene
                    && current.IsInFairyOcarinaCutscene;

                return escapedToRiver || escapedToSaria;
            }
            else if (segment == "kakariko")
            {
                return old.SceneID != Scene.Kakariko && current.SceneID == Scene.Kakariko;
            }
            else if (segment == "mido skip")
            {
                current.DidMidoSkip =
                    current.SceneID == Scene.KokiriForest
                    && current.X > 1600
                    && current.Y >= 0;

                return !old.DidMidoSkip && current.DidMidoSkip;
            }
            else if (segment == "deku tree")
            {
                return old.SceneID == Scene.KokiriForest && current.SceneID == Scene.DekuTree;
            }
            else if (segment == "gohma")
            {
                return current.SceneID == Scene.Gohma
                    && old.GohmasHealth > 0
                    && current.GohmasHealth <= 0;
            }
            else if (segment == "ganondorf" || segment == "wrong warp")
            {
                current.EntranceID = getEntrance();
                return checkEntrance(old.EntranceID, Entrance.WrongWarp)
                    && !checkEntrance(current.EntranceID, Entrance.WrongWarp);
            }
            else if (segment == "fire temple")
            {
                current.EntranceID = getEntrance();
                return checkEntrance(old.EntranceID, Entrance.VolvagiaBattle)
                    && !checkEntrance(current.EntranceID, Entrance.VolvagiaBattle);
                //TODO Test with wrong warp
            }
            else if (segment == "collapse" || segment == "tower collapse")
            {
                current.EntranceID = getEntrance();
                return !checkEntrance(old.EntranceID, Entrance.GanonBattle)
                    && checkEntrance(current.EntranceID, Entrance.GanonBattle);
            }
            else if (segment.EndsWith("warp in fire"))
            {
                if (current.DialogID != Dialog.None)
                {
                    current.LastActualDialog = (Dialog)current.DialogID;
                    current.LastActualDialogTime = current.GameFrames;
                }

                if (current.LastActualDialog == Dialog.FaroresWind)
                {
                    var delta = current.GameFrames - current.LastActualDialogTime;

                    if (delta >= 30)
                    {
                        current.LastActualDialog = Dialog.None;
                    }

                    return current.X == 0
                        && current.Y == 0
                        && current.Z == 0;
                }
            }
            else if (segment.EndsWith("dodongo hc") || segment.EndsWith("dodongo heart container"))
            {
                current.EntranceID = getEntrance();
                current.HeartContainers = current.Data[Offset.HeartContainers] >> 4;
                return checkEntrance(current.EntranceID, Entrance.DodongoBattle)
                    && current.HeartContainers > old.HeartContainers;
            }
            else if (segment == "ganon")
            {
                current.EntranceID = getEntrance();
                return checkEntrance(current.EntranceID, Entrance.GanonBattle)
                    && current.GanonsHealth <= 0
                    && old.GanonsAnimation != Animation.GanonFinalHit
                    && current.GanonsAnimation == Animation.GanonFinalHit;
            }

            return false;
        }

        public bool Reset(LiveSplitState timer, dynamic old, dynamic current)
        {
            return false;
        }

        public bool IsLoading(LiveSplitState timer, dynamic old, dynamic current)
        {
            return true;
        }

        public TimeSpan? GameTime(LiveSplitState timer, dynamic old, dynamic current)
        {
            if (current.GameFrames < old.GameFrames)
                current.AccumulatedFrames += old.GameFrames;

            return TimeSpan.FromSeconds((current.GameFrames + current.AccumulatedFrames) / 20.0f);
        }
    }
}
