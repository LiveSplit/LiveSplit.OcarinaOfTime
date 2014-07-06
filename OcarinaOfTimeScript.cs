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

        protected EmulatorBase Base { get; set; }

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

            Base = EmulatorBase.Mupen64;
            Rebuild((int)Base - ((int)module.BaseAddress));
        }

        private void RebuildProject64()
        {
            String version = ~new DeepPointer<String>(3, Game, 0x6aba2);

            if (version == "1.6")
                Base = EmulatorBase.Project64_16;
            else
                Base = EmulatorBase.Project64_17;
           
            Rebuild((int)Base);
        }

        private void Rebuild(int _base)
        {
            State.ValueDefinitions.Clear();

            AddPointer<GameData>("Data", _base, 0x11a5d0);
            AddPointer<int>("GameFrames", _base, 0x11f568);
            AddPointer<Scene>("Scene", _base, 0x1c8546);
            AddPointer<sbyte>("GohmasHealth", _base, 0x1e840c);
            AddPointer<sbyte>("GanonsHealth", _base, 0x1fa2dc);
            AddPointer<Animation>("GanonsAnimation", _base, 0x1fa374);
            AddPointer<Dialog>("Dialog", _base, 0x1d8872);
            AddPointer<ScreenType>("IsOnTitleScreenOrFileSelect", _base, 0x11b92c);
            AddPointer<float>("X", _base, 0x1c8714);
            AddPointer<float>("Y", _base, 0x1c8718);
            AddPointer<float>("Z", _base, 0x1c871c);
        }

        private void AddPointer<T>(String name, int _base, params int[] offsets)
        {
            AddPointer<T>(1, name, _base, offsets);
        }

        private void AddPointer<T>(int length, String name, int _base, params int[] offsets)
        {
            State.ValueDefinitions.Add(new ASLValueDefinition()
                {
                    Identifier = name,
                    Pointer = new DeepPointer<T>(length, Game, _base, offsets)
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

            current.LastActualDialog = Dialog.None;

            current.EyeBallFrogCount =
            current.LastActualDialogTime =
            current.HeartContainers =
                0;

            current.HasSword =
            current.HasBottle =
            current.HasIceArrows =
            current.HasFaroresWind =
            current.HasSlingshot =
            current.HasBombchus =
            current.HasHookshot =
            current.HasLongshot =
            current.HasBoomerang =
            current.HasIronBoots =
            current.HasHoverBoots =
            current.HasSongOfStorms =
            current.HasBombs =
            current.HasBoleroOfFire =
            current.HasRequiemOfSpirit =
            current.HasNocturneOfShadow =
            current.HasZeldasLullaby =
            current.HasEyeBallFrog =
            current.HasMasterSword =
            current.HasBiggoronSword =
            current.HasBow =
            current.IsInFairyOcarinaCutscene =
            current.DidMidoSkip =
                false;

            //Check for Timer Start
            return (old.Scene == Scene.FileSelect1 || old.Scene == Scene.FileSelect2)
                && !(current.Scene == Scene.FileSelect1 || current.Scene == Scene.FileSelect2);
        }

        public void RBA(Item bItemAfterUsing, Item cRightItem)
        {
            var ptr = new DeepPointer<Item>(Game, (int)Base, 0x11a644 + ((int)cRightItem ^ 0x3));
            ptr += bItemAfterUsing;
        }

        public void ReplaceGreenTunic()
        {
            var ptr = new DeepPointer<byte>(Game, (int)Base, 0x11a643);
            var oldValue = ~ptr;
            var currentTunic = oldValue & 0x7;
            if (currentTunic != 3 && currentTunic != 2)
            {
                ptr += (byte)((oldValue & ~0x7) | ((((dynamic)State.Data).GameFrames / (20 * 60 * 5)) % 3 + 0x4));
            }
        }

        public bool Split(LiveSplitState timer, dynamic old, dynamic current)
        {
            //Functions
            Func<byte, byte, bool> check = (x, y) => (x & y) != 0x0;
            Func<Inventory, Item> getInventoryItem = slot => current.Data.Inventory[(int)slot ^ 0x3];
            Action refreshHeartContainers = () => current.HeartContainers = current.Data.HeartContainers >> 4;
            Func<Entrance, Entrance, bool> checkEntrance = (x, y) => ((short)x | 0x3) == ((short)y | 0x3);

            //Check for Split
            var segment = timer.CurrentSplit.Name.ToLower();

            //Don't split on Title Screen or File Select because
            //Title Screen Link and Third File Link are loaded
            //and they might cause some splits to happen
            if (current.IsOnTitleScreenOrFileSelect != ScreenType.None)
            {
                //TODO Except some segments
                return false;
            }

            if (segment == "sword" || segment == "kokiri sword")
            {
                current.HasSword = current.Data.SwordsAndShields.HasFlag(SwordsAndShields.KokiriSword);
                return !old.HasSword && current.HasSword;
            }
            else if (segment == "master sword")
            {
                current.HasMasterSword = current.Data.SwordsAndShields.HasFlag(SwordsAndShields.MasterSword);
                return !old.HasMasterSword && current.HasMasterSword;
            }
            else if (segment == "biggoron sword" || segment == "biggoron's sword")
            {
                current.HasBiggoronSword = current.Data.SwordsAndShields.HasFlag(SwordsAndShields.BiggoronSword);
                return !old.HasBiggoronSword && current.HasBiggoronSword;
            }
            else if (segment == "hover boots")
            {
                current.HasHoverBoots = current.Data.TunicsAndBoots.HasFlag(TunicsAndBoots.HoverBoots);
                return !old.HasHoverBoots && current.HasHoverBoots;
            }
            else if (segment == "iron boots")
            {
                current.HasIronBoots = current.Data.TunicsAndBoots.HasFlag(TunicsAndBoots.IronBoots);
                return !old.HasIronBoots && current.HasIronBoots;
            }
            else if (segment == "song of storms")
            {
                current.HasSongOfStorms = current.Data.SongsAndEmeralds.HasFlag(SongsAndEmeralds.SongOfStorms);
                return old.Dialog == Dialog.SongOfStorms
                    && current.Dialog == Dialog.None
                    && current.HasSongOfStorms;
            }
            else if (segment == "bolero of fire")
            {
                current.HasBoleroOfFire = current.Data.MedallionsAndSongs.HasFlag(MedallionsAndSongs.BoleroOfFire);
                return !old.HasBoleroOfFire && current.HasBoleroOfFire;
            }
            else if (segment.Contains("lullaby"))
            {
                current.HasZeldasLullaby = current.Data.Songs.HasFlag(Songs.ZeldasLullaby);
                return !old.HasZeldasLullaby && current.HasZeldasLullaby;
                //TODO Include casual version (probably after some textbox)
            }
            else if (segment == "requiem of spirit")
            {
                current.HasRequiemOfSpirit = current.Data.Songs.HasFlag(Songs.RequiemOfSpirit);
                return old.Dialog == Dialog.RequiemOfSpirit
                    && current.Dialog == Dialog.None
                    && current.HasRequiemOfSpirit;
            }
            else if (segment == "nocturne of shadow")
            {
                current.HasNocturneOfShadow = current.Data.Songs.HasFlag(Songs.NocturneOfShadow);
                return old.Dialog == Dialog.NocturneOfShadow
                    && current.Dialog == Dialog.None
                    && current.HasNocturneOfShadow;
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
            else if (segment == "boomerang")
            {
                current.HasBoomerang = getInventoryItem(Inventory.Boomerang) == Item.Boomerang;
                return !old.HasBoomerang && current.HasBoomerang;
                //TODO Test
            }
            else if (segment == "bombchus" || segment == "spirit chus" || segment == "spirit bombchus")
            {
                current.HasBombchus = getInventoryItem(Inventory.Bombchus) == Item.Bombchus;
                return !old.HasBombchus && current.HasBombchus;
            }
            else if (segment == "hookshot")
            {
                current.HasHookshot = getInventoryItem(Inventory.Hookshot) == Item.Hookshot;
                return !old.HasHookshot && current.HasHookshot;
            }
            else if (segment == "longshot")
            {
                current.HasLongshot = getInventoryItem(Inventory.Hookshot) == Item.Longshot;
                return !old.HasLongshot && current.HasLongshot;
                //TODO Test
            }
            else if (segment == "bombs")
            {
                current.HasBombs = getInventoryItem(Inventory.Bombs) == Item.Bombs;
                return !old.HasBombs && current.HasBombs;
            }
            else if (segment == "forest escape" || segment == "escape")
            {
                var escapedToRiver =
                    !checkEntrance(old.Data.Entrance, Entrance.ForestToRiver)
                    && checkEntrance(current.Data.Entrance, Entrance.ForestToRiver);

                current.IsInFairyOcarinaCutscene =
                    checkEntrance(current.Data.Entrance, Entrance.BridgeBetweenFieldAndForest)
                    && current.Data.Cutscene == Cutscene.FairyOcarina;

                var escapedToSaria =
                    !old.IsInFairyOcarinaCutscene
                    && current.IsInFairyOcarinaCutscene;

                return escapedToRiver || escapedToSaria;
            }
            else if (segment == "child 2")
            {
                return old.Data.Cutscene == Cutscene.None
                    && current.Data.Cutscene == Cutscene.MasterSword
                    && current.Scene == Scene.TempleOfTime;
                //TODO Test (might screw up when doing the weird suns song thing)
            }
            else if (segment == "kakariko")
            {
                return old.Scene != Scene.Kakariko && current.Scene == Scene.Kakariko;
            }
            else if (segment == "mido skip")
            {
                current.DidMidoSkip =
                    current.Scene == Scene.KokiriForest
                    && current.X > 1600
                    && current.Y >= 0;

                return !old.DidMidoSkip && current.DidMidoSkip;
            }
            else if (segment == "deku tree")
            {
                return old.Scene == Scene.KokiriForest && current.Scene == Scene.DekuTree;
            }
            else if (segment.Contains("dampe"))
            {
                return old.Scene == Scene.Graveyard && current.Scene != Scene.Graveyard;
                //TODO Test
            }
            else if (segment == "gohma")
            {
                return current.Scene == Scene.Gohma
                    && old.GohmasHealth > 0
                    && current.GohmasHealth <= 0;
            }
            else if (segment == "ganondorf" || segment == "wrong warp")
            {
                return checkEntrance(old.Data.Entrance, Entrance.WrongWarp)
                    && !checkEntrance(current.Data.Entrance, Entrance.WrongWarp);
            }
            else if (segment == "fire temple")
            {
                return checkEntrance(old.Data.Entrance, Entrance.VolvagiaBattle)
                    && !checkEntrance(current.Data.Entrance, Entrance.VolvagiaBattle);
                //TODO Test with wrong warp
            }
            else if (segment == "collapse" || segment == "tower collapse")
            {
                return !checkEntrance(old.Data.Entrance, Entrance.GanonBattle)
                    && checkEntrance(current.Data.Entrance, Entrance.GanonBattle);
            }
            else if (segment == "fishing")
            {
                return checkEntrance(old.Data.Entrance, Entrance.FishingPond)
                    && !checkEntrance(current.Data.Entrance, Entrance.FishingPond);
            }
            else if (segment.StartsWith("enter jabu"))
            {
                return !checkEntrance(old.Data.Entrance, Entrance.InsideJabuJabusBelly)
                    && checkEntrance(current.Data.Entrance, Entrance.InsideJabuJabusBelly);
            }
            else if (segment.EndsWith("warp in fire") || segment.StartsWith("all fire"))
            {
                if (current.Dialog != Dialog.None)
                {
                    current.LastActualDialog = current.Dialog;
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

                //TODO Test All Fire Gold Skulltulas
            }
            else if (segment.EndsWith("dodongo hc") || segment.EndsWith("dodongo heart container"))
            {
                refreshHeartContainers();
                return checkEntrance(current.Data.Entrance, Entrance.DodongoBattle)
                    && current.HeartContainers > old.HeartContainers;
            }
            else if (segment == "forest temple")
            {
                refreshHeartContainers();
                return checkEntrance(current.Data.Entrance, Entrance.ForestTempleBoss)
                    && current.HeartContainers > old.HeartContainers;
            }
            else if (segment == "water temple" || segment.Contains("morpha") && (segment.Contains("heart container") || segment.Contains("hc")))
            {
                refreshHeartContainers();
                return checkEntrance(current.Data.Entrance, Entrance.WaterTempleBoss)
                    && current.HeartContainers > old.HeartContainers;
            }
            else if (segment == "ganon")
            {
                return checkEntrance(current.Data.Entrance, Entrance.GanonBattle)
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
