using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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

        public static LocalizedText SpawnSetText { get; private set; }
        public static LocalizedText SpawnSetFailedText { get; private set; }

        public override void SetStaticDefaults()
        {
            SpawnSetText = Mod.GetLocalization("Info.SpawnSet");
            SpawnSetFailedText = Mod.GetLocalization("Info.SpawnSetFailed");
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
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool? UseItem(Player player)
        {
            // UseItem is called every frame of the use animation; act only on the first frame.
            if (!player.ItemAnimationJustStarted)
                return true;

            // Right-click: clear the anchor and restore the default spawn.
            if (player.altFunctionUse == 2)
            {
                if (player.whoAmI == Main.myPlayer)
                    ClearAnchorLocal(player);
                return true;
            }

            int tileX = (int)(player.Center.X / 16f);
            // Player.Spawn puts the player's feet at the top of tile SpawnY.
            int tileY = (int)(player.Bottom.Y / 16f);

            if (!WorldGen.InWorld(tileX, tileY, 10))
            {
                if (player.whoAmI == Main.myPlayer)
                    CombatText.NewText(player.getRect(), Color.OrangeRed, SpawnSetFailedText.Value, dramatic: true);
                return true;
            }

            if (player.whoAmI == Main.myPlayer)
            {
                player.ChangeSpawn(tileX, tileY);
                player.GetModPlayer<AnchorPlayer>().SetAnchor(tileX, tileY);
                Main.NewText(Language.GetTextValue("Mods.SpawnAnchor.Info.SpawnSetDetail", tileX, tileY),
                    new Color(255, 220, 80));
            }

            SpawnSetEffects(player);
            return true;
        }

        private static void ClearAnchorLocal(Player player)
        {
            AnchorPlayer ap = player.GetModPlayer<AnchorPlayer>();
            if (ap.TryGetAnchor(out Point16 anchor))
            {
                // Only reset the vanilla spawn if the anchor still owns it (a bed used
                // after anchoring would have overwritten SpawnX/SpawnY).
                if (player.SpawnX == anchor.X && player.SpawnY == anchor.Y)
                {
                    player.SpawnX = -1;
                    player.SpawnY = -1;
                }
                ap.ClearAnchor();
                Main.NewText(Language.GetTextValue("Mods.SpawnAnchor.Info.SpawnCleared"), Color.LightGray);
            }
            else
            {
                Main.NewText(Language.GetTextValue("Mods.SpawnAnchor.Info.NoAnchorToClear"), Color.Gray);
            }
        }

        // While held: faint cool glow + occasional rising sparkle near the hand.
        public override void HoldItem(Player player)
        {
            if (Main.netMode == NetmodeID.Server || Main.gamePaused)
                return;

            Lighting.AddLight(player.Center, 0.20f, 0.28f, 0.45f);

            if (Main.rand.NextBool(6))
            {
                Vector2 pos = player.Center
                    + new Vector2(player.direction * 10f, 0f)
                    + Main.rand.NextVector2Circular(12f, 14f);
                Dust d = Dust.NewDustPerfect(pos, DustID.TreasureSparkle,
                    new Vector2(0f, -0.4f), 120, default, 0.95f);
                d.noGravity = true;
                d.fadeIn = 0.7f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Rainbow-cycling item name (4-second hue lap).
            foreach (TooltipLine line in tooltips)
            {
                if (line.Mod == "Terraria" && line.Name == "ItemName")
                    line.OverrideColor = Main.hslToRgb(Main.GameUpdateCount % 240 / 240f, 1f, 0.7f);
            }

            // Anchor status line (ModifyTooltips runs on the local client only).
            if (Main.gameMenu || !Main.LocalPlayer.TryGetModPlayer(out AnchorPlayer ap))
                return;

            string text = ap.TryGetAnchor(out Point16 anchor)
                ? Language.GetTextValue("Mods.SpawnAnchor.Items.SpawnAnchorItem.AnchorStatus", anchor.X, anchor.Y)
                : Language.GetTextValue("Mods.SpawnAnchor.Items.SpawnAnchorItem.NoAnchorStatus");

            tooltips.Add(new TooltipLine(Mod, "AnchorStatus", text)
            {
                OverrideColor = new Color(255, 220, 80)
            });
        }

        private void SpawnSetEffects(Player player)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 center = player.Center;

            // Layer 1: core flash.
            for (int i = 0; i < 14; i++)
            {
                Dust d = Dust.NewDustPerfect(center, DustID.GoldFlame,
                    Main.rand.NextVector2Circular(1.6f, 1.6f), 0, default, 2.6f);
                d.noGravity = true;
                d.fadeIn = 1.6f;
            }

            // Layer 2: triple hue-rotating shockwave.
            SpawnShockwaveRing(center, 60, 9.0f, 0.00f, 1.9f, DustID.RainbowMk2);
            SpawnShockwaveRing(center, 48, 6.2f, 0.33f, 1.6f, DustID.RainbowMk2);
            SpawnShockwaveRing(center, 36, 4.0f, 0.66f, 1.3f, DustID.FireworksRGB);

            // Layer 3: rising golden vortex (2 spiral arms).
            const int SpiralArms = 2;
            const int SpiralSteps = 22;
            for (int arm = 0; arm < SpiralArms; arm++)
            {
                for (int i = 0; i < SpiralSteps; i++)
                {
                    float t = i / (SpiralSteps - 1f);
                    float angle = MathHelper.Pi * arm + t * MathHelper.TwoPi * 1.5f;
                    float radius = MathHelper.Lerp(6f, 26f, t);
                    Vector2 pos = center + angle.ToRotationVector2() * radius;
                    Vector2 vel = (angle + MathHelper.PiOver2).ToRotationVector2() * 2.5f
                        + new Vector2(0f, -4.5f - 3.5f * t);
                    Dust d = Dust.NewDustPerfect(pos, DustID.GoldFlame, vel, 80, default, 1.45f);
                    d.noGravity = true;
                    d.fadeIn = 1.1f;
                }
            }

            // Layer 4: twinkling star sparkles.
            for (int i = 0; i < 18; i++)
            {
                Vector2 pos = center + Main.rand.NextVector2Circular(30f, 24f);
                Dust d = Dust.NewDustPerfect(pos, DustID.TreasureSparkle,
                    Main.rand.NextVector2Circular(0.8f, 0.8f) + new Vector2(0f, -0.6f),
                    60, default, 1.35f);
                d.noGravity = true;
                d.fadeIn = 0.9f;
            }

            // Layer 5: falling gem glitter (gravity on).
            for (int i = 0; i < 24; i++)
            {
                Vector2 pos = center + new Vector2(Main.rand.NextFloat(-44f, 44f),
                    Main.rand.NextFloat(-72f, -28f));
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f),
                    Main.rand.NextFloat(0.6f, 1.7f));
                int type = Main.rand.NextBool() ? DustID.GemDiamond : DustID.GemSapphire;
                Dust d = Dust.NewDustPerfect(pos, type, vel, 60, default,
                    Main.rand.NextFloat(0.9f, 1.3f));
                d.fadeIn = 0.7f;
            }

            Lighting.AddLight(center, 1.5f, 1.3f, 0.8f);

            // Only shake the camera of the player who used the item.
            if (player.whoAmI == Main.myPlayer)
            {
                Main.instance.CameraModifiers.Add(
                    new PunchCameraModifier(center, Main.rand.NextVector2Unit(), 7f, 9f, 22, 1000f, FullName));
            }

            // Layered chord.
            SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen with { Volume = 0.8f, Pitch = -0.25f }, center);
            SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = 0.9f }, center);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.85f, Pitch = 0.15f }, center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.55f, PitchVariance = 0.1f }, center);

            CombatText.NewText(player.getRect(), new Color(255, 220, 80),
                SpawnSetText.Value, dramatic: true);
        }

        // One ring of the shockwave: evenly spaced dust with hue rotating a full lap
        // around the circumference, offset per ring.
        private static void SpawnShockwaveRing(Vector2 center, int count, float speed,
            float hueOffset, float scale, int dustType)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Color color = Main.hslToRgb((hueOffset + (float)i / count) % 1f, 1f, 0.62f);
                Dust d = Dust.NewDustPerfect(center, dustType,
                    angle.ToRotationVector2() * speed, 0, color, scale);
                d.noGravity = true;
                d.fadeIn = 1.25f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.StoneBlock, 10)
                .Register();
        }
    }
}
