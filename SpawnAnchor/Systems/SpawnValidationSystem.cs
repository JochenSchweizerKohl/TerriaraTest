using Terraria;
using Terraria.ModLoader;
using SpawnAnchor.Players;

namespace SpawnAnchor.Systems
{
    public class SpawnValidationSystem : ModSystem
    {
        public override void Load()
        {
            On_Player.CheckSpawn += CheckSpawn_AllowAnchor;
        }

        private static bool CheckSpawn_AllowAnchor(On_Player.orig_CheckSpawn orig, int x, int y)
        {
            if (orig(x, y))
                return true;

            if (!AnchorAreaIsClear(x, y))
                return false;

            // CheckSpawn is static; identify the spawning player by the coords passed
            // (Player.Spawn passes that player's SpawnX/SpawnY). Works on server and
            // remote clients once anchors are synced.
            foreach (Player p in Main.ActivePlayers)
            {
                if (p.SpawnX == x && p.SpawnY == y
                    && p.TryGetModPlayer(out AnchorPlayer ap)
                    && ap.AnchorMatches(x, y))
                {
                    return true;
                }
            }
            return false;
        }

        // Vanilla spawn placement puts the player's feet at the top of tile row y.
        // The player hitbox fits in a 3-wide, 3-tall tile area above that row. If the
        // area was filled in after anchoring, fail so vanilla falls back to world spawn.
        private static bool AnchorAreaIsClear(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 10))
                return false;
            for (int i = x - 1; i <= x + 1; i++)
                for (int j = y - 3; j < y; j++)
                    if (WorldGen.SolidTile(i, j, false))
                        return false;
            return true;
        }
    }
}
