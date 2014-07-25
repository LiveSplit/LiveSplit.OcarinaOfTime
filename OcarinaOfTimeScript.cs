using LiveSplit.Model;
using LiveSplit.OcarinaOfTime;
using System;

namespace LiveSplit.ASL
{
    public class OcarinaOfTimeScript
    {
        protected TimerModel Model { get; set; }
        protected Emulator Emulator { get; set; }
        public ASLState OldState { get; set; }
        public ASLState State { get; set; }

        public OcarinaOfTimeScript()
        {
            State = new ASLState();
        }

        protected void TryConnect()
        {
            if (Emulator == null || Emulator.Process.HasExited)
            {
                Emulator = Emulator.TryConnect();
                if (Emulator != null)
                {
                    Rebuild();
                    State.RefreshValues();
                    OldState = State;
                }
            }
        }

        private void Rebuild()
        {
            State.ValueDefinitions.Clear();

            var gameVersion = GetGameVersion();

            switch (gameVersion)
            {
                case GameVersion.NTSC10: RebuildNTSC10(); break;
                case GameVersion.NTSC12: RebuildNTSC12(); break;
                default: Emulator = null; break;
            }
        }

        private void RebuildNTSC10()
        {
            AddPointer<GameData>("Data", 0x11a5d0);
            AddPointer<byte>("FPSDenominator", 0x1c6fa2);
            AddPointer<int>("GameFrames", 0x11f568);
            AddPointer<Scene>("Scene", 0x1c8546);
            AddPointer<sbyte>("GohmasHealth", 0x1e840c);
            AddPointer<sbyte>("GanonsHealth", 0x1fa2dc);
            AddPointer<sbyte>("GanondorfsHealth", 0x20b5cc);
            AddPointer<Animation>("GanonsAnimation", 0x1fa374);
            AddPointer<Dialog>("Dialog", 0x1d8872);
            AddPointer<ScreenType>("IsOnTitleScreenOrFileSelect", 0x11b92c);
            AddPointer<float>("X", 0x1c8714);
            AddPointer<float>("Y", 0x1c8718);
            AddPointer<float>("Z", 0x1c871c);
            AddPointer<byte>("WarpAnimationPlaying", 0x1c87cc);
        }
        private void RebuildNTSC12()
        {
            AddPointer<GameData>("Data", 0x11ac80);
            AddPointer<byte>("FPSDenominator", 0x1c7862);
            AddPointer<int>("GameFrames", 0x11fc30);
            AddPointer<Scene>("Scene", 0x1c8e06);
            AddPointer<sbyte>("GohmasHealth", 0x1e8ccc);
            AddPointer<sbyte>("GanonsHealth", 0x1fabac);
            AddPointer<sbyte>("GanondorfsHealth", 0x20be7c);
            AddPointer<Animation>("GanonsAnimation", 0x1fac44);
            AddPointer<Dialog>("Dialog", 0x1d9132);
            AddPointer<ScreenType>("IsOnTitleScreenOrFileSelect", 0x11bfdc);
            AddPointer<float>("X", 0x1db314);
            AddPointer<float>("Y", 0x1db318);
            AddPointer<float>("Z", 0x1db31c);
            AddPointer<byte>("WarpAnimationPlaying", 0x1c908c);
        }

        private GameVersion GetGameVersion()
        {
            var correctChecksum = "DLEZ";

            //Check for NTSC 1.0
            var gameDataCheck = ~Emulator.CreatePointer<String>(4, 0x11a5ec);

            if (gameDataCheck == correctChecksum)
                return GameVersion.NTSC10;

            //Check for NTSC 1.2
            gameDataCheck = ~Emulator.CreatePointer<String>(4, 0x11ac9c);

            if (gameDataCheck == correctChecksum)
                return GameVersion.NTSC12;

            return GameVersion.Unknown;
        }

        private void AddPointer<T>(String name, int address)
        {
            AddPointer<T>(1, name, address);
        }

        private void AddPointer<T>(int length, String name, int address)
        {
            State.ValueDefinitions.Add(new ASLValueDefinition()
                {
                    Identifier = name,
                    Pointer = Emulator.CreatePointer<T>(length, address)
                });
        }

        public void Update(LiveSplitState lsState)
        {
            if (Emulator != null && !Emulator.Process.HasExited)
            {
                OldState = State.RefreshValues();

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

                    var isPaused = IsPaused(lsState, OldState.Data, State.Data);
                    if (isPaused != null)
                        lsState.IsGameTimePaused = isPaused;

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

            current.AccumulatedFrames20 =
            current.AccumulatedFrames30 =
            current.AccumulatedFrames60 =
                0;

            current.LastActualDialog = Dialog.None;

            current.Quiver = Quiver.None;
            current.BombBag = BombBag.None;
            current.Gauntlet = Gauntlet.None;
            current.Scale = Scale.None;
            current.Wallet = Wallet.DefaultWallet;
            current.BulletBag = BulletBag.None;
            current.DekuSticks = DekuSticks.None;
            current.DekuNuts = DekuNuts.None;

            current.ChildTradeItem = Item.None;

            current.EyeBallFrogCount =
            current.LastActualDialogTime =
            current.HeartContainers =
            current.HeartPieces =
            current.PoeCount =
                0;

            current.HasSword =
            current.HasMirrorShield =
            current.HasBottle =
            current.HasBottle4 =
            current.HasIceArrows =
            current.HasFireArrows =
            current.HasFaroresWind =
            current.HasDinsFire =
            current.HasSlingshot =
            current.HasBombchus =
            current.HasHookshot =
            current.HasLongshot =
            current.HasBoomerang =
            current.HasOcarinaOfTime =
            current.HasIronBoots =
            current.HasHoverBoots =
            current.HasBombs =
            current.HasLensOfTruth =
            current.HasGerudoMemberShipCard =
            current.HasSongOfStorms =
            current.HasBoleroOfFire =
            current.HasRequiemOfSpirit =
            current.HasNocturneOfShadow =
            current.HasZeldasLullaby =
            current.HasCojiro =
            current.HasOddMushroom =
            current.HasOddPotion =
            current.HasPoachersSaw =
            current.HasBrokenGoronsSword =
            current.HasEyeBallFrog =
            current.HasMasterSword =
            current.HasBiggoronSword =
            current.HasBow =
            current.HasSpookyMask =
            current.HasMaskOfTruth =
            current.IsInFairyOcarinaCutscene =
            current.DidMidoSkip =
                false;

            //Check for Timer Start
            return (old.Scene == Scene.FileSelect1 || old.Scene == Scene.FileSelect2)
                && !(current.Scene == Scene.FileSelect1 || current.Scene == Scene.FileSelect2);
        }

        /*public void RBA(Item bItemAfterUsing, Item cRightItem)
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
        }*/

        public bool Split(LiveSplitState timer, dynamic old, dynamic current)
        {
            //Functions
            Func<Inventory, Item> getInventoryItem = slot => current.Data.Inventory[(int)slot ^ 0x3];
            Action<Inventory> checkPoe = slot => { if (getInventoryItem(slot) == Item.BigPoe && old.Data.Inventory[(int)slot ^ 0x3] != Item.BigPoe) current.PoeCount++; };
            Action refreshHeartContainers = () => current.HeartContainers = current.Data.HeartContainers >> 4;
            Action refreshHeartPieces = () => current.HeartPieces = current.Data.HeartPieces >> 4;
            Func<Upgrade, uint> getUpgrade = x => (uint)((current.Data.Upgrades >> (int)x) & 0x7);
            Action refreshQuiver = () => current.Quiver = (Quiver)getUpgrade(Upgrade.Quiver);
            Action refreshBombBag = () => current.BombBag = (BombBag)getUpgrade(Upgrade.BombBag);
            Action refreshGauntlet = () => current.Gauntlet = (Gauntlet)getUpgrade(Upgrade.Gauntlet);
            Action refreshScale = () => current.Scale = (Scale)getUpgrade(Upgrade.Scale);
            Action refreshWallet = () => current.Wallet = (Wallet)(getUpgrade(Upgrade.Wallet) & 0x3);
            Action refreshBulletBag = () => current.BulletBag = (BulletBag)getUpgrade(Upgrade.BulletBag);
            Action refreshDekuSticks = () => current.DekuSticks = (DekuSticks)getUpgrade(Upgrade.DekuSticks);
            Action refreshDekuNuts = () => current.DekuNuts = (DekuNuts)getUpgrade(Upgrade.DekuNuts);
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

            //Checking Poes
            checkPoe(Inventory.Bottle1);
            checkPoe(Inventory.Bottle2);
            checkPoe(Inventory.Bottle3);
            checkPoe(Inventory.Bottle4);

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
            else if (segment == "mirror shield")
            {
                current.HasMirrorShield = current.Data.SwordsAndShields.HasFlag(SwordsAndShields.MirrorShield);
                return !old.HasMirrorShield && current.HasMirrorShield;
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
            else if (segment.Contains("gerudo") && segment.Contains("card"))
            {
                current.HasGerudoMemberShipCard = current.Data.SongsAndEmeralds.HasFlag(SongsAndEmeralds.GerudoMemberShipCard);
                return !old.HasGerudoMemberShipCard && current.HasGerudoMemberShipCard;
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
            else if (segment == "magic")
            {
                return old.Dialog == Dialog.None
                    && current.Dialog == Dialog.Magic
                    && current.Data.HasMagic;
            }
            else if (segment == "double magic")
            {
                return old.Dialog == Dialog.DoubleMagic
                    && current.Dialog == Dialog.None
                    && current.Data.HasDoubleMagic;
            }
            else if (segment == "3 poes")
            {
                return current.PoeCount >= 3 
                    && old.Dialog == Dialog.BigPoe 
                    && current.Dialog == Dialog.None;
            }
            else if (segment == "6 poes")
            {
                return current.PoeCount >= 6
                    && old.Dialog == Dialog.BigPoe
                    && current.Dialog == Dialog.None;
            }
            else if (segment == "bottle")
            {
                current.HasBottle = getInventoryItem(Inventory.Bottle1) == Item.EmptyBottle;
                return !old.HasBottle && current.HasBottle;
            }
            else if (segment.Contains("bottle") && (segment.Contains("last") || segment.Contains("final") || segment.Contains("4th") || segment.Contains("fourth")))
            {
                current.HasBottle4 = getInventoryItem(Inventory.Bottle4) == Item.EmptyBottle;
                return !old.HasBottle4 && current.HasBottle4;
            }
            else if (segment.StartsWith("ice arrow"))
            {
                current.HasIceArrows = getInventoryItem(Inventory.IceArrows) == Item.IceArrows;
                return !old.HasIceArrows && current.HasIceArrows;
            }
            else if (segment.StartsWith("fire arrow"))
            {
                current.HasFireArrows = getInventoryItem(Inventory.FireArrows) == Item.FireArrows;
                return !old.HasFireArrows && current.HasFireArrows;
            }
            else if (segment.Contains("din") && segment.Contains("fire"))
            {
                current.HasDinsFire = getInventoryItem(Inventory.DinsFire) == Item.DinsFire;
                return !old.HasDinsFire && current.HasDinsFire;
            }
            else if (segment.EndsWith("bow"))
            {
                current.HasBow = getInventoryItem(Inventory.Bow) == Item.Bow;
                return !old.HasBow && current.HasBow;
                //TODO Test
            }
            else if (segment == "cojiro")
            {
                current.HasCojiro = getInventoryItem(Inventory.AdultTradeItem) == Item.Cojiro;
                return !old.HasCojiro && current.HasCojiro;
            }
            else if (segment == "odd mushroom")
            {
                current.HasOddMushroom = getInventoryItem(Inventory.AdultTradeItem) == Item.OddMushroom;
                return !old.HasOddMushroom && current.HasOddMushroom;
            }
            else if (segment == "odd potion")
            {
                current.HasOddPotion = getInventoryItem(Inventory.AdultTradeItem) == Item.OddPotion;
                return !old.HasOddPotion && current.HasOddPotion;
            }
            else if (segment.Contains("poacher") || segment.Contains("saw"))
            {
                current.HasPoachersSaw = getInventoryItem(Inventory.AdultTradeItem) == Item.PoachersSaw;
                return !old.HasPoachersSaw && current.HasPoachersSaw;
            }
            else if (segment.Contains("broken") && segment.Contains("sword"))
            {
                current.HasBrokenGoronsSword = getInventoryItem(Inventory.AdultTradeItem) == Item.BrokenGoronsSword;
                return !old.HasBrokenGoronsSword && current.HasBrokenGoronsSword;
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
            else if (segment.Contains("lens of truth"))
            {
                current.HasLensOfTruth = getInventoryItem(Inventory.LensOfTruth) == Item.LensOfTruth;
                return !old.HasLensOfTruth && current.HasLensOfTruth;
            }
            else if (segment == "bombs")
            {
                current.HasBombs = getInventoryItem(Inventory.Bombs) == Item.Bombs;
                return !old.HasBombs && current.HasBombs;
            }
            else if (segment == "ocarina of time")
            {
                current.HasOcarinaOfTime = getInventoryItem(Inventory.Ocarina) == Item.OcarinaOfTime;
                return !old.HasOcarinaOfTime && current.HasOcarinaOfTime;
            }
            else if (segment == "spooky mask")
            {
                current.HasSpookyMask = getInventoryItem(Inventory.ChildTradeItem) == Item.SpookyMask;
                return !old.HasSpookyMask && current.HasSpookyMask;
            }
            else if (segment == "mask of truth")
            {
                current.HasMaskOfTruth = getInventoryItem(Inventory.ChildTradeItem) == Item.MaskOfTruth;
                return !old.HasMaskOfTruth && current.HasMaskOfTruth;
            }
            else if (segment.Contains("sell") && segment.Contains("bunny"))
            {
                current.ChildTradeItem = getInventoryItem(Inventory.ChildTradeItem);
                return old.ChildTradeItem == Item.BunnyHood 
                    && current.ChildTradeItem == Item.SoldOut;
            }
            else if (segment == "big bomb bag")
            {
                refreshBombBag();
                return old.BombBag != BombBag.BigBombBag
                    && current.BombBag == BombBag.BigBombBag;
            }
            else if ((segment.Contains("2nd") || segment.Contains("second")) && segment.Contains("stick"))
            {
                refreshDekuSticks();
                return old.DekuSticks != DekuSticks.DekuSticks30
                    && current.DekuSticks == DekuSticks.DekuSticks30;
            }
            else if (segment.Contains("nut") && segment.Contains("upgrade"))
            {
                refreshDekuNuts();
                return old.DekuNuts != DekuNuts.DekuNuts40
                    && current.DekuNuts == DekuNuts.DekuNuts40;
            }
            else if (segment.Contains("golden gauntlet"))
            {
                refreshGauntlet();
                return old.Gauntlet != Gauntlet.GoldenGauntlets
                    && current.Gauntlet == Gauntlet.GoldenGauntlets;
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
            else if (segment == "child 2" || segment == "adult 2")
            {
                return old.Data.Cutscene == Cutscene.None
                    && current.Data.Cutscene == Cutscene.MasterSword
                    && current.Scene == Scene.TempleOfTime;
                //TODO Test (might screw up when doing the weird suns song thing)
                //TODO Test Adult 2
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
            else if (segment == "scrubs")
            {
                return old.Scene == Scene.DekuTree && current.Scene == Scene.Gohma;
            }
            else if (segment.Contains("dampe"))
            {
                if (old.Scene != current.Scene)
                {
                    refreshHeartContainers();
                    refreshHeartPieces();

                    if (old.Scene == Scene.Graveyard
                        && (old.HeartPieces != current.HeartPieces 
                            || old.HeartContainers != current.HeartContainers))
                        return true;
                }

                return false;
            }
            else if (segment == "gohma")
            {
                return current.Scene == Scene.Gohma
                    && old.GohmasHealth > 0
                    && current.GohmasHealth <= 0;
            }
            else if (segment == "ganondorf" || segment == "wrong warp")
            {
                var wrongWarp = checkEntrance(old.Data.Entrance, Entrance.WrongWarp)
                    && !checkEntrance(current.Data.Entrance, Entrance.WrongWarp);

                var ganondorfDead = current.Scene == Scene.GanondorfBattle
                    && current.GanondorfsHealth <= 0
                    && old.GanondorfsHealth > 0;

                return wrongWarp || ganondorfDead;
            }
            else if (segment.Contains("spirit") && segment.Contains("boss door"))
            {
                return !checkEntrance(old.Data.Entrance, Entrance.SpiritTempleBoss)
                    && checkEntrance(current.Data.Entrance, Entrance.SpiritTempleBoss);
            }
            else if (segment == "fire temple")
            {
                return checkEntrance(old.Data.Entrance, Entrance.VolvagiaBattle)
                    && !checkEntrance(current.Data.Entrance, Entrance.VolvagiaBattle);
                //TODO Test with wrong warp
            }
            else if (segment == "collapse" || segment == "tower collapse" || segment == "castle escape" || segment == "tower escape")
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
            else if (segment == "jabu")
            {
                return checkEntrance(old.Data.Entrance, Entrance.InsideJabuJabusBellyBoss)
                    && checkEntrance(current.Data.Entrance, Entrance.ZorasFountain2);
            }
            else if (segment.Contains("warp in fire") || segment.StartsWith("all fire") || segment.Contains("warp in shadow"))
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
                //TODO Test warp in shadow temple
            }
            else if (segment.Contains("farore") && segment.Contains("wind"))
            {
                current.HasFaroresWind = getInventoryItem(Inventory.FaroresWind) == Item.FaroresWind;
                return !old.HasFaroresWind && current.HasFaroresWind;
            }
            else if (segment.Contains("lon lon") && (segment.Contains("hp") || segment.Contains("heart piece")))
            {
                refreshHeartPieces();
                return checkEntrance(current.Data.Entrance, Entrance.LonLonWindMill)
                       && current.HeartPieces != old.HeartPieces;
            }
            else if (segment.Contains("richardz"))
            {
                refreshHeartPieces();
                return checkEntrance(current.Data.Entrance, Entrance.RichardZHouse)
                       && current.HeartPieces != old.HeartPieces;
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
            else if (segment == "shadow temple")
            {
                return checkEntrance(current.Data.Entrance, Entrance.ShadowTempleBoss)
                    && old.WarpAnimationPlaying == 0
                    && current.WarpAnimationPlaying == 1;
            }
            else if (segment == "spirit temple")
            {
                return checkEntrance(current.Data.Entrance, Entrance.SpiritTempleBoss)
                    && old.WarpAnimationPlaying == 0
                    && current.WarpAnimationPlaying == 1;
            }

            return false;
        }

        public bool Reset(LiveSplitState timer, dynamic old, dynamic current)
        {
            return false;
        }

        public bool IsPaused(LiveSplitState timer, dynamic old, dynamic current)
        {
            return true;
        }

        public TimeSpan? GameTime(LiveSplitState timer, dynamic old, dynamic current)
        {
            var delta = (current.GameFrames >= old.GameFrames)
                ? current.GameFrames - old.GameFrames
                : current.GameFrames;

            switch ((int)current.FPSDenominator)
            {
                case 1: current.AccumulatedFrames60 += delta; break;
                case 2: current.AccumulatedFrames30 += delta; break;
                case 3: current.AccumulatedFrames20 += delta; break;
            }

            var seconds = current.AccumulatedFrames20 / 20.0f 
                + current.AccumulatedFrames30 / 30.0f 
                + current.AccumulatedFrames60 / 60.0f;

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
