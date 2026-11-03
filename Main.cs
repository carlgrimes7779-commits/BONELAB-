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

        private static GameObject? targetReticle;
        private static Transform? currentTargetTransform;
        private static Vector3 targetPosition;
        private static Rigidbody? currentThrownBody;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized!");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);

            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled && targetReticle != null)
                {
                    targetReticle.SetActive(false);
                    isLockedIn = false;
                }
            });

            category.CreateFunction("❌ Reset Lock-In Target", Color.red, () => 
            {
                isLockedIn = false;
                currentTargetTransform = null;
                MelonLogger.Msg("Target Unlocked.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null)
            {
                CreateTargetReticle();
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                Transform pointerTransform = cam.transform;

                if (isLockedIn && currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(cam.transform);
                        targetReticle.SetActive(true);
                    }
                }
                else
                {
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.transform.LookAt(cam.transform);
                            targetReticle.SetActive(true);
                        }

                        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton3))
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg(string.Format("Locked onto moving target: {0}", hit.transform.name));
                        }
                    }
                    else if (!isLockedIn && targetRet
