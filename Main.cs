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

        public override void OnInitializeMelon()
        {
            try
            {
                MelonLogger.Msg("Initializing Homing Throw Mod safely...");

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

                category.CreateBool("Lock-In Mode", Color.green, isLockedIn, (value) => 
                {
                    isLockedIn = value;
                    if (!isLockedIn)
                    {
                        currentTargetTransform = null;
                    }
                });

                category.CreateFunction("🔒 Lock Target Now", Color.cyan, () => 
                {
                    Camera cam = Camera.main;
                    if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 100f))
                    {
                        isLockedIn = true;
                        currentTargetTransform = hit.transform;
                    }
                });

                category.CreateFunction("❌ Reset Target", Color.red, () => 
                {
                    isLockedIn = false;
                    currentTargetTransform = null;
                });
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Error during initialization: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            try
            {
                if (!isHomingEnabled) return;

                if (targetReticle == null)
                {
                    CreateHolographicReticle();
                }

                Camera cam = Camera.main;
                if (cam != null && targetReticle != null)
                {
                    Transform pointerTransform = cam.transform;

                    if (isLockedIn && currentTargetTransform != null)
                    {
                        targetPosition = currentTargetTransform.position;
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(cam.transform);
                        targetReticle.SetActive(true);
                    }
                    else
                    {
                        if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                        {
                            targetPosition = hit.point;
                            targetReticle.transform.position = targetPosition;
                            targetReticle.transform.LookAt(cam.transform);
                            targetReticle.SetActive(true);
                        }
                        else
                        {
                            targetReticle.SetActive(false);
                        }
                    }
                }
            }
            catch
            {
                // Suppress update errors silently so game execution never freezes or black screens
            }
        }

        private static void CreateHolographicReticle()
        {
            try
            {
                targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
                if (targetReticle != null)
                {
                    targetReticle.name = "HolographicCrosshair";
                    targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                    Collider col = targetReticle.GetComponent<Collider>();
                    if (col != null)
                    {
                        UnityEngine.Object.Destroy(col);
                    }

                    Renderer renderer = targetReticle.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Shader sh = Shader.Find("Unlit/Color");
                        if (sh != null)
                        {
                            renderer.material.shader = sh;
                            renderer.material.color = Color.cyan;
                        }
                    }
                    targetReticle.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to create reticle: {ex.Message}");
            }
        }
    }
}
