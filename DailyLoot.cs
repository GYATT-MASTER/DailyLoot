using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
                    [3] = (ItemID.Diamond, 5),
                    [4] = GetModdedItems()[1],
                    [5] = (ItemID.Ruby, 10),
                    [6] = GetModdedItems()[2], 
                    [7] = GetConditionalItems()[0],

                    [8] = (ItemID.Obsidian, 30),
                    [9] = (ItemID.DemoniteBar, 5),
                    [10] = (ItemID.PinkGel, 20),
                    [11] = (ItemID.PinkGel, 20),
                    [12] = (ItemID.PinkGel, 20),
                    [13] = (ItemID.PinkGel, 20),
                    [14] = GetConditionalItems()[1],

                    [15] = (ItemID.PinkGel, 20),
                    [16] = (ItemID.PinkGel, 20),                 
                    [17] = (ItemID.PinkGel, 20),
                    [18] = (ItemID.PinkGel, 20),
                    [19] = (ItemID.PinkGel, 20),
                    [20] = (ItemID.PinkGel, 20),
                    [21] = GetConditionalItems()[2],

                    [22] = (ItemID.PinkGel, 20),
                    [23] = (ItemID.PinkGel, 20),
                    [24] = (ItemID.PinkGel, 20),
                    [25] = (ItemID.PinkGel, 20),
                    [26] = (ItemID.PinkGel, 20),
                    [27] = (ItemID.PinkGel, 20),
                    [28] = GetConditionalItems()[3],

                    [29] = (ItemID.PinkGel, 20),
                    [30] = (ItemID.PinkGel, 20),
                    [31] = GetConditionalItems()[4],

                    [32] = (ItemID.PinkGel, 20), // safeguard
                },

                _ => []
            };
        }

        /// <summary>
        ///     Retrieve rewards that change based on some condition here, such as <see cref="Main.hardMode"/>.
        /// <br>Some of these may be modded drops, and will default to a pair of vanilla items if the mod isnt active.</br>
        /// </summary>
        /// <returns></returns>
        public static (int, int)[] GetConditionalItems()
        {
            (int, int)[] loot = new (int, int)[5];

            // shorthands are pretty sigma...
            static (int, int) GetResult(bool condition, (int, int) fail, (int, int) succeed) => condition ? succeed : fail;

            loot[0] = GetResult(Main.hardMode, (ItemID.PlatinumBar, 10), (ItemID.MythrilBar, 5));
            loot[1] = GetResult(NPC.downedMechBossAny, GetResult(NPC.downedBoss2, (ItemID.GoldBar, 15), (ItemID.TissueSample, 30)), (ItemID.HallowedBar, 15));
            loot[2] = GetResult(NPC.downedPlantBoss, GetResult(Main.hardMode, (ItemID.GoldBar, 15), (ItemID.TissueSample, 30)), (ItemID.MythrilBar, 5));
            loot[3] = GetResult(Main.hardMode, (ItemID.PlatinumBar, 10), (ItemID.MythrilBar, 5));
            loot[4] = GetResult(Main.hardMode, (ItemID.PlatinumBar, 10), (ItemID.MythrilBar, 5));

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
                {
                    loot[0] = (scrap.Type, 10);
                }

                if (calamity.TryFind<ModItem>("SeaPrism", out var seaPrism))
                {
                    loot[1] = (seaPrism.Type, 15);
                }
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

                // only the client should broadcast this.
                if (Main.netMode != NetmodeID.Server)
                    Main.NewText($"Today's reward{(isPluralAmount ? "s are" : " is")} {(isPluralAmount ? $"{amount}" : "a")} {ItemID.Search.GetName(drop)}{(isPluralAmount ? "s" : "")}!".ColorString(Color.Cyan.LerpTo(Color.Violet, 0.15f)));

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
