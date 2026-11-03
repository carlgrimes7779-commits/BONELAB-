using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;
using Il2CppSLZ.Marrow;

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
            MelonLogger.Msg("Homing Throw Mod with Lock-In Initialized!");

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

            category.CreateFunction("Reset Lock-In Target", Color.red, () => 
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

            // Lock-in targeting logic via raycast from local player's right hand/head
            if (Player.RigManager != null)
            {
                Transform pointerTransform = Player.RightHand != null ? Player.RightHand.transform : Player.Head.transform;

                // Handle Lock-In tracking
                if (isLockedIn && currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                    targetReticle.transform.position = targetPosition;
                    targetReticle.SetActive(true);
                }
                else
                {
                    // Raycast ahead to find surfaces or NPCs
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point;
                        targetReticle.transform.position = targetPosition;
                        targetReticle.SetActive(true);

                        // Trigger click lock-in detection (Check trigger pressure or primary button)
                        if (Player.RightController != null && Player.RightController.GetIndexDown())
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg($"Target Locked onto: {hit.transform.name}");
                        }
                    }
                    else if (!isLockedIn)
                    {
                        targetReticle.SetActive(false);
                    }
                }

                // If lock-in key pressed again, unlock
                if (isLockedIn && Player.RightController != null && Player.RightController.GetMenuButtonDown())
                {
                    isLockedIn = false;
                    currentTargetTransform = null;
                    MelonLogger.Msg("Target Unlocked.");
                }
            }

            // Homing force applied to active thrown body
            if (currentThrownBody != null)
            {
                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 15f);
                
                // Adjust velocity towards target position
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 12f);
            }
        }

        private static void CreateTargetReticle()
        {
            // Create a simple primitive ring/sphere indicator
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetReticle.name = "HomingTargetReticle";
            targetReticle.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            // Remove collider so it doesn't physically bump objects
            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Set color to bright cyan
            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.shader = Shader.Find("Unlit/Color");
                renderer.material.color = Color.cyan;
            }

            targetReticle.SetActive(false);
        }
    }
}
