using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SpawnAnchor.Players
{
    public class AnchorPlayer : ModPlayer
    {
        public Dictionary<string, Point16> Anchors = new();

        private static string WorldKey => Main.ActiveWorldFileData.UniqueId.ToString();

        public void SetAnchor(int tileX, int tileY, bool sync = true)
        {
            Anchors[WorldKey] = new Point16(tileX, tileY);
            if (sync && Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                SendAnchorPacket(-1, Main.myPlayer);
        }

        public bool TryGetAnchor(out Point16 anchor) => Anchors.TryGetValue(WorldKey, out anchor);

        public bool AnchorMatches(int x, int y)
            => Anchors.TryGetValue(WorldKey, out Point16 p) && p.X == x && p.Y == y;

        public void ClearAnchor(bool sync = true)
        {
            Anchors.Remove(WorldKey);
            if (sync && Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                SendAnchorPacket(-1, Main.myPlayer);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
            => SendAnchorPacket(toWho, fromWho);

        internal void SendAnchorPacket(int toWho, int fromWho)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)Player.whoAmI);
            bool has = Anchors.TryGetValue(WorldKey, out Point16 p);
            packet.Write(has);
            if (has)
            {
                packet.Write(p.X);
                packet.Write(p.Y);
            }
            packet.Send(toWho, fromWho);
        }

        internal static void HandleAnchorPacket(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadByte();
            bool has = reader.ReadBoolean();
            short x = 0, y = 0;
            if (has)
            {
                x = reader.ReadInt16();
                y = reader.ReadInt16();
            }

            AnchorPlayer ap = Main.player[playerIndex].GetModPlayer<AnchorPlayer>();
            if (has)
                ap.SetAnchor(x, y, sync: false);
            else
                ap.ClearAnchor(sync: false);

            if (Main.netMode == NetmodeID.Server)
                ap.SendAnchorPacket(-1, whoAmI);
        }

        public override void SaveData(TagCompound tag)
        {
            var list = new List<TagCompound>();
            foreach (var kv in Anchors)
                list.Add(new TagCompound { ["world"] = kv.Key, ["x"] = (int)kv.Value.X, ["y"] = (int)kv.Value.Y });
            tag["anchors"] = list;
        }

        public override void LoadData(TagCompound tag)
        {
            Anchors.Clear();
            foreach (TagCompound t in tag.GetList<TagCompound>("anchors"))
                Anchors[t.GetString("world")] = new Point16(t.GetInt("x"), t.GetInt("y"));
        }
    }
}
