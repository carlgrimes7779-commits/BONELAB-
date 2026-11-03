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

            // Create BoneMenu UI entry
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

            // Initialize visual reticle sphere if missing
            if (targetReticle == null)
            {
                CreateTargetReticle();
            }

            // Raycast targeting logic from hand or head
            if (Player.PhysicsRig != null)
            {
                Transform pointerTransform = Player.RightHand != null ? Player.RightHand.transform : Player.Head.transform;

                // Lock-In tracking mode
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
                    // Raycast forward to locate surface or enemy NPC
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.SetActive(true);
                        }

                        // Right index trigger click locks target
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

                // Menu button click unlocks target
                if (isLockedIn && Player.RightController != null && Player.RightController.GetMenuButtonDown())
                {
                    isLockedIn = false;
                    currentTargetTransform = null;
                    MelonLogger.Msg("Target Unlocked.");
                }
            }

            // Apply trajectory force to thrown object towards target position
            if (currentThrownBody != null)
            {
                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 15f);
                
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 12f);
            }
        }

        private static void CreateTargetReticle()
        {
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetReticle.name = "HomingTargetReticle";
            targetReticle.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }

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
