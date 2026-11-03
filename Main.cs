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
        private static Rigidbody? trackedThrownObject;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized.");

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

            category.CreateBool("Lock-In Target", Color.green, isLockedIn, (value) => 
            {
                isLockedIn = value;
                if (!isLockedIn)
                {
                    currentTargetTransform = null;
                }
            });

            category.CreateFunction("🔒 Quick Lock Target", Color.cyan, () => 
            {
                Camera cam = Camera.main;
                if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 200f))
                {
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg($"Locked onto target: {hit.transform.name}");
                }
            });

            category.CreateFunction("❌ Reset Target", Color.red, () => 
            {
                isLockedIn = false;
                currentTargetTransform = null;
                trackedThrownObject = null;
                MelonLogger.Msg("Target reset and unlocked.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null)
            {
                CreateBullseyeReticle();
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                Transform pointer = cam.transform;

                // Update target position based on locked object or active raycast
                if (isLockedIn && currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                }
                else
                {
                    if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, 200f))
                    {
                        targetPosition = hit.point;
                    }
                    else
                    {
                        targetPosition = pointer.position + (pointer.forward * 5.0f);
                    }
                }

                // Render and scale the bright holographic bullseye reticle so it's visible anywhere on map
                if (targetReticle != null)
                {
                    targetReticle.transform.position = targetPosition;
                    targetReticle.transform.LookAt(cam.transform);

                    float distanceToCam = Vector3.Distance(cam.transform.position, targetPosition);
                    float dynamicScale = Mathf.Clamp(distanceToCam * 0.08f, 0.25f, 2.5f);
                    targetReticle.transform.localScale = new Vector3(dynamicScale, dynamicScale, dynamicScale);

                    targetReticle.SetActive(true);
                }
            }

            // Universal Homing Curve Logic: Any thrown object curves aggressively to hit the target
            if (isLockedIn && targetPosition != Vector3.zero)
            {
                // Auto-detect newly thrown objects if we aren't tracking one yet
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = GameObject.FindObjectsOfType<Rigidbody>();
                    foreach (Rigidbody rb in allBodies)
                    {
                        // Check if object is moving fast enough to be a thrown item and isn't the player/reticle
                        if (rb != null && rb.velocity.magnitude > 3f && rb.gameObject != targetReticle)
                        {
                            trackedThrownObject = rb;
                            break;
                        }
                    }
                }

                // Apply aggressive steering force so the object curves directly into the target
                if (trackedThrownObject != null)
                {
                    if (currentTargetTransform != null)
                    {
                        targetPosition = currentTargetTransform.position;
                    }

                    Vector3 directionToTarget = (targetPosition - trackedThrownObject.position).normalized;
                    float speed = Mathf.Max(trackedThrownObject.velocity.magnitude, 22f);
                    
                    // Smoothly curve velocity vector directly toward the target destination
                    trackedThrownObject.velocity = Vector3.Slerp(trackedThrownObject.velocity, directionToTarget * speed, Time.deltaTime * 15f);
                }
            }
            else
            {
                trackedThrownObject = null;
            }
        }

        private static void CreateBullseyeReticle()
        {
            // Create a Quad to display a clean glowing cyan bullseye crosshair overlay
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HolographicBullseyeCrosshair";
            targetReticle.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            // Strip collider so it has zero physics impact
            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.Destroy(col);
            }

            // Apply glowing unlit cyan material visible across all map perspectives
            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader sh = Shader.Find("Unlit/Color");
                if (sh != null)
                {
                    renderer.material.shader = sh;
                    renderer.material.color = new Color(0f, 1f, 1f, 1f); // Vibrant visible cyan
                }
            }

            targetReticle.SetActive(false);
            MelonLogger.Msg("Bullseye reticle object successfully built.");
        }
    }
}
