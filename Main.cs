using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;

[assembly: MelonInfo(typeof(HomingThrowMod.HomingThrowMod), "Homing throw quest", "1.0.0", "Author")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace HomingThrowMod
{
    public class HomingThrowMod : MelonMod
    {
        public static bool isHomingEnabled = true;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing throw mod initializing menu...");

            // Create main page under BoneMenu Root
            Page category = Page.Category("Homing Throw Quest", Color.yellow);

            // Add enable/disable toggle
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                MelonLogger.Msg($"Homing enabled: {isHomingEnabled}");
            });
        }
    }
}

