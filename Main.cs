using MelonLoader;
using BoneLib;
using UnityEngine;

[assembly: MelonInfo(typeof(HomingThrowMod.HomingThrowMod), "Homing throw quest", "1.0.0", "Author")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace HomingThrowMod
{
    public class HomingThrowMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing throw mod initialized!");
        }
    }
}
