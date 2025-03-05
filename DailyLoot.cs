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
        // not needed until i do the UI, will remain for now

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

        private bool FirstLogin;

        public Dictionary<int, (int, int)> LootLookupTable = [];

        private static Dictionary<int, (int, int)> InitializeLoot() 
        {
            return DateTime.Now.Month switch
            {
                //for now, all months have the same loot.

                >= 0 and <= 12 => new Dictionary<int, (int, int)>
                {
                    [0] = (ItemID.Bunny, 1),
                    [1] = (ItemID.Wood, 150),
                    [2] = GetModdedItems()[0],
                    [3] = (ItemID.Diamond, 5),
                    [4] = GetModdedItems()[1],
                    [5] = (ItemID.PinkGel, 20),
                    [6] = GetModdedItems()[2],
                    [7] = (ItemID.PinkGel, 20),
                    [8] = (ItemID.PinkGel, 20),
                    [9] = (ItemID.PinkGel, 20),
                    [10] = (ItemID.PinkGel, 20),
                    [11] = (ItemID.PinkGel, 20),
                    [12] = (ItemID.PinkGel, 20),
                    [13] = (ItemID.PinkGel, 20),
                    [14] = (ItemID.PinkGel, 20),
                    [15] = (ItemID.PinkGel, 20),
                    [16] = (ItemID.PinkGel, 20),
                    [17] = (ItemID.PinkGel, 20),
                    [18] = (ItemID.PinkGel, 20),
                    [19] = (ItemID.PinkGel, 20),
                    [20] = (ItemID.PinkGel, 20),
                    [21] = (ItemID.PinkGel, 20),
                    [22] = (ItemID.PinkGel, 20),
                    [23] = (ItemID.PinkGel, 20),
                    [24] = (ItemID.PinkGel, 20),
                    [25] = (ItemID.PinkGel, 20),
                    [26] = (ItemID.PinkGel, 20),
                    [27] = (ItemID.PinkGel, 20),
                    [28] = (ItemID.PinkGel, 20),
                    [29] = (ItemID.PinkGel, 20),
                    [30] = (ItemID.PinkGel, 20),
                    [31] = (ItemID.PinkGel, 20),
                    [32] = (ItemID.PinkGel, 20),
                },

                _ => []
            };
        }

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

            // set up the drops for each day of the current month. (though all months currently have the same loot)
            LootLookupTable = InitializeLoot();

            // always set this when logging in.
            LastLogin = DateTime.Now.Day;
        }

        public override void PostUpdateWorld()
        {
            if (LastLogin == SavedLoginTime + 1)
            {
                SavedLoginTime = LastLogin;
                RewardsClaimable = true;
            }

            if (RewardsClaimable)
            {
                //spawn an item for the current day!

                int drop = LootLookupTable[LastLogin].Item1;
                int amount = LootLookupTable[LastLogin].Item2;

                int item = Item.NewItem(Item.GetSource_None(), Main.LocalPlayer.getRect(), drop, amount);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item, 1f);

                bool isPluralAmount = amount > 1;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Main.NewText($"Today's reward{(isPluralAmount ? "s are" : " is")} {(isPluralAmount ? $"{amount}" : "a")} {drop}{(isPluralAmount ? "s" : "")}!".ColorString(Color.LightYellow.LerpTo(Color.Violet, 0.1f)));

                RewardsClaimable = false;
            }
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
