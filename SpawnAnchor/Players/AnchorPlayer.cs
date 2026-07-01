using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SpawnAnchor.Players
{
    public class AnchorPlayer : ModPlayer
    {
        public Dictionary<string, Point16> Anchors = new();

        public void SetAnchor(int tileX, int tileY)
        {
            Anchors[Main.ActiveWorldFileData.UniqueId.ToString()] = new Point16(tileX, tileY);
        }

        public bool AnchorMatches(int x, int y)
        {
            return Anchors.TryGetValue(Main.ActiveWorldFileData.UniqueId.ToString(), out Point16 p)
                && p.X == x && p.Y == y;
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
