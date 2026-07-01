using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using SpawnAnchor.Players;

namespace SpawnAnchor.Items
{
    public class SpawnAnchorItem : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.MagicMirror;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useTurn = true;
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item6;
            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool? UseItem(Player player)
        {
            int tileX = (int)(player.Center.X / 16f);
            int tileY = (int)(player.Center.Y / 16f);

            if (player.whoAmI == Main.myPlayer)
            {
                player.ChangeSpawn(tileX, tileY);
                player.GetModPlayer<AnchorPlayer>().SetAnchor(tileX, tileY);
            }

            SpawnSetEffects(player);
            return true;
        }

        private void SpawnSetEffects(Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 center = player.Center;

            const int RingCount = 60;
            for (int i = 0; i < RingCount; i++)
            {
                float angle = MathHelper.TwoPi * i / RingCount;
                Vector2 velocity = angle.ToRotationVector2() * 7f;
                Dust d = Dust.NewDustPerfect(center, DustID.GoldCoin, velocity, 100, default, 2.0f);
                d.noGravity = true;
                d.fadeIn = 1.2f;
            }

            for (int i = 0; i < RingCount; i++)
            {
                float angle = MathHelper.TwoPi * (i + 0.5f) / RingCount;
                Dust d = Dust.NewDustPerfect(center, DustID.Electric, angle.ToRotationVector2() * 3.5f, 150, default, 1.1f);
                d.noGravity = true;
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 pos = player.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), 0f);
                Vector2 vel = new Vector2(0f, -Main.rand.NextFloat(3f, 9f)).RotatedByRandom(0.25f);
                Color color = Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.55f);
                Dust d = Dust.NewDustPerfect(pos, DustID.RainbowMk2, vel, 0, color, 1.6f);
                d.noGravity = true;
                d.fadeIn = 0.8f;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(player.position, player.width, player.height,
                    DustID.MagicMirror, 0f, 0f, 150, default, 1.4f);
                d.velocity *= 1.8f;
            }

            Lighting.AddLight(center, 1.2f, 1.0f, 0.4f);

            Main.instance.CameraModifiers.Add(
                new PunchCameraModifier(center, Main.rand.NextVector2Unit(), 6f, 8f, 20, 1000f, FullName));

            SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, center);
            SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.4f, Volume = 0.8f }, center);

            CombatText.NewText(player.getRect(), new Color(255, 220, 80),
                Language.GetTextValue("Mods.SpawnAnchor.Info.SpawnSet"), dramatic: true);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
        }
    }
}
