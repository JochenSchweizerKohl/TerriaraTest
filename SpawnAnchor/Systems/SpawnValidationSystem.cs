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
            if (Main.dedServ)
                return false;
            Player local = Main.LocalPlayer;
            return local != null && local.active
                && local.TryGetModPlayer(out AnchorPlayer ap)
                && ap.AnchorMatches(x, y);
        }
    }
}
