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

            category.CreateBool("Lock-In Mode Enabled", Color.green, isLockedIn, (value) =>
            {
                isLockedIn = value;
                if (!isLockedIn)
                {
                    currentTargetTransform = null;
                    MelonLogger.Msg("Lock-In Mode Disabled.");
                }
                else
                {
                    MelonLogger.Msg("Lock-In Mode Enabled.");
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

                // If locked in on a specific target (object or person), stick permanently to them
                if (isLockedIn && currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition + Vector3.up * 0.1f;
                        targetReticle.SetActive(true);
                    }
                }
                else
                {
                    // Raycast forward to find objects or people
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.SetActive(true);

                            // If lock-in mode is toggled on via menu, locking onto the raycast hit object/person
                            if (isLockedIn && currentTargetTransform == null)
                            {
                                currentTargetTransform = hit.transform;
                                MelonLogger.Msg(string.Format("Permanently Locked onto: {0}", hit.transform.name));
                            }
                        }
                    }
                    else if (!isLockedIn && targetReticle != null)
                    {
                        targetReticle.SetActive(false);
                    }
                }
            }

            // Apply homing trajectory force if an object is tracked
            if (currentThrownBody != null)
            {
                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 15f);
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 12f);
            }
        }

        private static void CreateTargetReticle()
        {
            // Create a 2D-style transparent crosshair plane/quad
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HomingCrosshairReticle";
            targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Remove collider so it never interacts physically with the world
            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                GameObject.Destroy(col);
            }

            // Apply transparent 2D crosshair shader styling
            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.shader = Shader.Find("Transparent/Diffuse");
                renderer.material.color = new Color(0f, 1f, 1f, 0.8f); // Cyan transparent look
            }

            targetReticle.SetActive(false);
        }
    }
}
