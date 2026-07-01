using System.IO;
using Terraria.ModLoader;
using SpawnAnchor.Players;

namespace SpawnAnchor
{
    public class SpawnAnchor : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            AnchorPlayer.HandleAnchorPacket(reader, whoAmI);
        }
    }
}
