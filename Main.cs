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
        public static bool isLockedIn = false;

        private static GameObject targetReticle;
        private static GameObject laserLineObj;
        private static LineRenderer laserLine;
        private static Transform currentTargetTransform;
        private static Vector3 targetPosition;
        private static Rigidbody trackedThrownObject;
        private static float throwStartTime = 0f;

        private static float lastClickTime = 0f;
        private static bool wasStickPressedLastFrame = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized.");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled) ResetHomingState();
            });

            category.CreateFunction("🎯 Force Lock Target", Color.green, () => 
            {
                LockTargetFromPointer();
            });

            category.CreateFunction("❌ Reset / Clear Lock", Color.red, () => 
            {
                ResetHomingState();
                MelonLogger.Msg("Lock-In state cleared.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null) CreateCrosshairReticle();
            if (laserLineObj == null) CreateLaserLine();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 offhandOrigin = cam.transform.position + (cam.transform.right * -0.25f) + (cam.transform.forward * 0.2f) - (cam
