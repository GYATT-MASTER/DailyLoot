using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

using static Terraria.ModLoader.ModContent;

namespace DailyLoot
{
	public class DailyLoot : Mod
	{
        // not needed until i do the UI, will remain here for now

		public static DailyLoot Instance { get; private set; } = null;

        public override void Load()
        {
            Instance = this;
        }

        public override void Unload()
        {
            Instance = null;
        }
    }

    internal class DailyLootSystem : ModSystem
    {
        public static DailyLootSystem Instance => GetInstance<DailyLootSystem>();

        private int LastLogin = -1;

        private int SavedLoginTime = -69;

        private bool RewardsClaimable;

        private bool FirstLogin = true;

        public Dictionary<int, (int, int)> LootLookupTable = null;

        /// <summary>
        /// Maps dates to an item, and an amount of said item.
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, (int, int)> InitializeLoot() 
        {
            return DateTime.Now.Month switch
            {
                // for now, all months have the same loot.

                >= 0 and <= 12 => new Dictionary<int, (int, int)>
                {
                    [0] = (ItemID.Bunny, 1), // safeguard
                    
                    [1] = (ItemID.Wood, 500), 
                    [2] = GetModdedItems()[0],
                    [3] = (ItemID.Diamond, 10),
                    [4] = GetModdedItems()[1],
                    [5] = (ItemID.Ruby, 10),
                    [6] = GetModdedItems()[2], 
                    [7] = GetConditionalItems()[0],

                    [8] = (ItemID.Obsidian, 30),
                    [9] = GetBasicConditionalItems()[0],
                    [10] = (ItemID.Marble, 500),
                    [11] = (ItemID.Granite, 500),
                    [12] = GetBasicConditionalItems()[1],
                    [13] = (ItemID.PinkGel, 100),
                    [14] = GetConditionalItems()[1],

                    [15] = (ItemID.Topaz, 10),
                    [16] = GetModdedItems()[3],               
                    [17] = GetBasicConditionalItems()[2],
                    [18] = (ItemID.LifeCrystal, 3),
                    [19] = (ItemID.FallenStar, 50),
                    [20] = GetModdedItems()[3],
                    [21] = GetConditionalItems()[2],

                    [22] = (ItemID.Sapphire, 10),
                    [23] = GetModdedItems()[4],
                    [24] = (ItemID.LifeforcePotion, 5),
                    [25] = GetBasicConditionalItems()[3],
                    [26] = (ItemID.HerbBag, 20),
                    [27] = GetConditionalItems()[3],
                    [28] = GetConditionalItems()[4],

                    [29] = GetBasicConditionalItems()[4],
                    [30] = (ItemID.ManaCrystal, 5),
                    [31] = (ItemID.PotionOfReturn, 20),

                    [32] = (ItemID.PinkGel, 20), // safeguard
                },

                _ => []
            };
        }

        /// <summary>
        ///     Retrieve rewards that change based on some condition here, such as <see cref="Main.hardMode"/>.
        /// </summary>
        /// <returns></returns>
        public static (int, int)[] GetConditionalItems()
        {
            (int, int)[] loot = new (int, int)[5];

            // shorthands are pretty sigma...
            static (int, int) GetResult(bool condition, (int, int) fail, (int, int) succeed) => condition ? succeed : fail;

            loot[0] = GetResult(Main.hardMode, (ItemID.PlatinumBar, 10), (ItemID.MythrilBar, 5));
            loot[1] = GetResult(Main.hardMode, (ItemID.HellstoneBar, 10), (ItemID.TitaniumBar, 10));
            loot[2] = GetResult(NPC.downedMechBossAny, GetResult(NPC.downedBoss2, (ItemID.GoldBar, 15), (ItemID.TissueSample, 30)), (ItemID.HallowedBar, 15));
            loot[3] = GetResult(NPC.downedPlantBoss, GetResult(Main.hardMode, (ItemID.DemoniteBar, 20), (ItemID.MythrilBar, 20)), (ItemID.ChlorophyteBar, 25));
            loot[4] = GetResult(NPC.downedMoonlord, GetResult(Main.hardMode, (ItemID.PlatinumBar, 100), (ItemID.OrichalcumBar, 100)), (ItemID.LunarBar, 50));

            return loot;
        }

        /// <summary>
        ///     Retrieve rewards that change based on some condition here, such as <see cref="Main.hardMode"/>.
        /// <br>Some of these may be modded drops, and will default to a pair of vanilla items if the mod isnt active.</br>
        /// </summary>
        /// <returns></returns>
        public static (int, int)[] GetBasicConditionalItems()
        {
            (int, int)[] loot = new (int, int)[5];

            // shorthands are pretty sigma...
            static (int, int) GetResult(bool condition, (int, int) fail, (int, int) succeed) => condition ? succeed : fail;

            loot[0] = GetResult(Main.hardMode, (ItemID.Diamond, 10), (ItemID.CrystalShard, 50));

            if (ModLoader.TryGetMod("CalamityMod", out var calamity) && Main.hardMode)
            {
                if (calamity.TryFind<ModItem>("EssenceofEleum", out var essence1))
                    loot[1] = (essence1.Type, 15);

                if (calamity.TryFind<ModItem>("EssenceofHavoc", out var essence2))
                    loot[2] = (essence2.Type, 15);

                if (calamity.TryFind<ModItem>("EssenceofSunlight", out var essence3))
                    loot[3] = (essence3.Type, 15);

                if (calamity.TryFind<ModItem>("GalacticaSingularity", out var essence4) && NPC.downedMoonlord)
                    loot[4] = (essence4.Type, 15);
            }

            else //maybe change these to be more unique? idk
            {
                loot[1] = GetResult(NPC.downedBoss2, (ItemID.GoldCoin, 10), (ItemID.GoldCoin, 15));
                loot[2] = GetResult(NPC.downedBoss2, (ItemID.GoldCoin, 10), (ItemID.GoldCoin, 15));
                loot[3] = GetResult(NPC.downedBoss2, (ItemID.GoldCoin, 10), (ItemID.GoldCoin, 15));
                loot[4] = GetResult(NPC.downedMoonlord, (ItemID.GoldCoin, 10), (ItemID.PlatinumCoin, 1));
            }

            return loot;
        }

        /// <summary>
        ///     Retrieve modded items here. 
        /// <br>Vanilla items are provided as a fallback if the mods are disabled.</br>
        /// </summary>
        /// <returns></returns>
        public static (int, int)[] GetModdedItems()
        {
            (int, int)[] loot = new (int, int)[8];

            // calamity mod loot (other mods will be added later?)
            if (ModLoader.TryGetMod("CalamityMod", out var calamity))
            {
                if (calamity.TryFind<ModItem>("WulfrumMetalScrap", out var scrap))
                    loot[0] = (scrap.Type, 20);

                if (calamity.TryFind<ModItem>("SeaPrism", out var seaPrism))
                    loot[1] = (seaPrism.Type, 30);

                if (calamity.TryFind<ModItem>("PlantyMush", out var item3))
                    loot[2] = (item3.Type, 100);

                if (calamity.TryFind<ModItem>("AerialiteBar", out var item4) && NPC.downedBoss2)
                    loot[3] = (item4.Type, 25);

                if (calamity.TryFind<ModItem>("Lumenyl", out var item5) && NPC.downedMechBossAny)
                    loot[4] = (item5.Type, 30);
            }

            // this will have to be changed when support for other mods is added later.. :3

            // if the mods were not present, default to vanilla loot.
            else
            {
                for (int i = 0; i < loot.Length; i++)
                {
                    loot[i] = (ItemID.GoldCoin, 10);
                }
            }

            return loot;
        }

        public override void OnModLoad()
        {
            // initialize the first ever login.
            if (SavedLoginTime == -69)
                SavedLoginTime = DateTime.Now.Day;

            // additional initial login setup.
            if (FirstLogin != false)
            {
                RewardsClaimable = true;
                FirstLogin = false;
            }

            // always set this when logging in.
            LastLogin = DateTime.Now.Day;
        }

        public override void PostUpdateWorld()
        {
            // set up all the drops for the current month.
            // this is initialized here as some mods may load after this one, so this is here to ensure all attempts to retrieve modded items are as successful as possible.
            LootLookupTable ??= InitializeLoot();

            // refreshes the value when a new month starts
            if (LastLogin == 1 && SavedLoginTime > 0)
                SavedLoginTime = 0;

            // if you logged in yesterday, and claimed rewards, claim new rewards today.
            // uses >= in case of the player missing a day, or more.
            if (LastLogin >= SavedLoginTime + 1)
            {
                SavedLoginTime = LastLogin;
                RewardsClaimable = true;
            }

            if (RewardsClaimable)
            {
                // spawn an item for the current day!

                int drop = LootLookupTable[LastLogin].Item1;
                int amount = LootLookupTable[LastLogin].Item2;

                int item = Item.NewItem(Item.GetSource_None(), Main.LocalPlayer.getRect(), drop, amount);

                // sync the provided reward to all clients.
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);

                bool isPluralAmount = amount > 1;

                string itemText = ItemID.Search.GetName(drop);
                
                // check if the name ends in y. if so, replace with ie.
                if (isPluralAmount && itemText.EndsWith('y'))
                    itemText = string.Concat(itemText.AsSpan(0, itemText.Length - 1), "ie");
            
                // only the client should broadcast this.
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText($"Today's reward{(isPluralAmount ? "s are" : " is")} {(isPluralAmount ? $"{amount}" : "a")} {itemText}{(isPluralAmount ? "s" : "")}!".ColorString(Color.Cyan.LerpTo(Color.Violet, 0.15f)));

                RewardsClaimable = false;
            }

            //Main.NewText($"{LastLogin}, {SavedLoginTime}, {FirstLogin}, {RewardsClaimable}");
        }

        // Ensure that values that should be saved...get saved...

        public override void SaveWorldData(TagCompound tag)
        {
            tag.Add(nameof(SavedLoginTime), SavedLoginTime);
            tag.Add(nameof(FirstLogin), FirstLogin);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SavedLoginTime = tag.GetInt(nameof(SavedLoginTime));
            FirstLogin = tag.GetBool(nameof(FirstLogin));
        }

        // Sync changes

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(SavedLoginTime);
            writer.WriteFlags(FirstLogin);
        }

        public override void NetReceive(BinaryReader reader)
        {
            SavedLoginTime = reader.ReadInt32();
            reader.ReadFlags(out FirstLogin);
        }
    }
}
