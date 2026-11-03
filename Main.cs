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
        private static Transform currentTargetTransform;
        private static Vector3 targetPosition;
        private static Rigidbody currentThrownBody;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized!");

            // Create BoneMenu UI
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

            category.CreateFunction("Reset Lock-In", Color.red, () => 
            {
                isLockedIn = false;
                currentTargetTransform = null;
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            // Initialize visual target reticle if missing
            if (targetReticle == null)
            {
                CreateTargetReticle();
            }

            // Raycast targeting logic from right hand or head
            if (Player.PhysicsRig != null)
            {
                Transform pointerTransform = Player.RightHand != null ? Player.RightHand.transform : Player.Head.transform;

                // Handle Lock-In tracking mode
                if (isLockedIn && currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.SetActive(true);
                    }
                }
                else
                {
                    // Raycast out to find surfaces or objects
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.SetActive(true);
                        }

                        // Primary controller trigger click locks target
                        if (Player.RightController != null && Player.RightController.GetIndexDown())
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg($"Target Locked onto: {hit.transform.name}");
                        }
                    }
                    else if (!isLockedIn && targetReticle != null)
                    {
                        targetReticle.SetActive(false);
                    }
                }

                // Press menu button to unlock target
                if (isLockedIn && Player.RightController != null && Player.RightController.GetMenuButtonDown())
                {
                    isLockedIn = false;
                    currentTargetTransform = null;
                    MelonLogger.Msg("Target Unlocked.");
                }
            }

            // Apply homing trajectory force to active thrown objects
            if (currentThrownBody != null)
            {
                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 15f);
                
                // Curve object towards target
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 12f);
            }
        }

        private static void CreateTargetReticle()
        {
            // Create target indicator sphere
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetRet
