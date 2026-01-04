using BepInEx;
using BepInEx.Configuration;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;
using WTTArmoryClient.Patches;
using WTTArmoryClient.Properties;

namespace WTTArmoryClient
{
    [BepInPlugin("com.wtt.armory", "WTT-Armory", "2.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static GameWorld GameWorld;
        public static string[] GunIDs = ["678fe4a4906c7bd23722c71f", "679a6a534f3d279c99b135b9"];
        
        // Hyperburst configuration
        public static ConfigEntry<float> BurstROFMulti { get; private set; }
        public static ConfigEntry<float> BurstRecoilMulti { get; private set; }
        public static ConfigEntry<float> ShotResetDelay { get; private set; }
        public static ConfigEntry<float> ShotThreshold { get; private set; }

        // Hyperburst state
        public static bool IsAN94 { get; set; }
        public static bool IsFiring { get; set; }
        public static int RecoilShotCount { get; set; }
        public static int ROFShotCount { get; set; }
        public static float ShotTimer { get; set; }
        public Player You { get; set; }

        private void Awake()
        {
            // Hyperburst configuration with ordering
            BurstROFMulti = Config.Bind(
                "Hyperburst",
                "Burst ROF Multi",
                3f,
                new ConfigDescription("Rate of fire multiplier during hyperburst",
                    new AcceptableValueRange<float>(1f, 4f),
                    new ConfigurationManagerAttributes { Order = 1 }
                )
            );

            BurstRecoilMulti = Config.Bind(
                "Hyperburst",
                "Burst Recoil Multi",
                0.5f,
                new ConfigDescription("Recoil multiplier during hyperburst",
                    new AcceptableValueRange<float>(0.1f, 1f),
                    new ConfigurationManagerAttributes { Order = 2 }
                )
            );

            ShotResetDelay = Config.Bind(
                "Hyperburst",
                "Shot Reset Delay",
                0.05f,
                new ConfigDescription("Time delay after firing to determine if firing has stopped",
                    new AcceptableValueRange<float>(0.01f, 2f),
                    new ConfigurationManagerAttributes { Order = 3 }
                )
            );

            ShotThreshold = Config.Bind(
                "Hyperburst",
                "Hyperburst Shot Threshold",
                1f,
                new ConfigDescription("Shot count when hyperburst ends (adjust for timing issues)",
                    new AcceptableValueRange<float>(0f, 5f),
                    new ConfigurationManagerAttributes { Order = 4 }
                )
            );

            // Enable patches
            new UpdateWeaponVariablesPatch().Enable();
            new ShootPatch().Enable();
        }

        private void Update()
        {
            // Update Hyperburst logic
            if (You == null)
            {
                if (GameWorld?.MainPlayer is { IsYourPlayer: true } mainPlayer)
                {
                    You = mainPlayer;
                }
            }
            else
            {
                var firearmController = You.HandsController as Player.FirearmController;
                UpdateHyperburst(firearmController);
            }
        }

        private void UpdateHyperburst(Player.FirearmController fc)
        {
            if (fc == null || !IsAN94) return;

            fc.Item.MalfState.OverheatFirerateMultInited = true;
            fc.Item.MalfState.OverheatFirerateMult = 
                (ROFShotCount <= ShotThreshold.Value && fc.Item.SelectedFireMode != Weapon.EFireMode.single) 
                ? BurstROFMulti.Value 
                : 1f;

            if (IsFiring)
            {
                ShotTimer += Time.deltaTime;
                if (!fc.autoFireOn && ShotTimer >= ShotResetDelay.Value)
                {
                    ShotTimer = 0f;
                    IsFiring = false;
                    RecoilShotCount = 0;
                    ROFShotCount = 0;
                }
            }
        }
    }
}