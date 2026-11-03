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
        private static bool previousTriggerState = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized.");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled) ResetHomingState();
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null) CreateCrosshairReticle();
            if (laserLineObj == null) CreateLaserLine();

            Camera cam = Camera.main;
            if (cam == null) return;

            // Offhand pointer origin and direction
            Vector3 offhandOrigin = cam.transform.position + (cam.transform.right * -0.25f) + (cam.transform.forward * 0.2f) - (cam.transform.up * 0.15f);
            Vector3 aimDirection = cam.transform.forward;

            // Detect left/offhand trigger input safely across Quest runtime
            bool currentTriggerState = Input.GetAxis("TriggerLeft") > 0.7f || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton1);
            bool triggerPressedDown = currentTriggerState && !previousTriggerState;
            previousTriggerState = currentTriggerState;

            // Perform raycast ignoring spawn volumes and triggers so it hits traffic cones, props, and enemies instead of void gates
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(offhandOrigin, aimDirection, out hit, 200f, ~((1 << 2) | (1 << 4) | (1 << 20)));

            if (triggerPressedDown)
            {
                if (isLockedIn)
                {
                    // If already locked in, clicking the offhand trigger deletes/resets the lock
                    ResetHomingState();
                    MelonLogger.Msg("Lock-in deleted via offhand trigger.");
                }
                else if (hitSomething && hit.transform != null)
                {
                    // Lock onto the exact object being aimed at
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg("Locked onto target: " + hit.transform.name);
                }
            }

            if (isLockedIn && currentTargetTransform != null
