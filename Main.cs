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

            category.CreateFunction("🔒 Lock Target / Toggle Lock-In", Color.green, () => 
            {
                Camera cam = Camera.main;
                if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 100f))
                {
                    isLockedIn = !isLockedIn;
                    if (isLockedIn)
                    {
                        currentTargetTransform = hit.transform;
                        MelonLogger.Msg(string.Format("Target Locked onto: {0}", hit.transform.name));
                    }
                    else
                    {
                        currentTargetTransform = null;
                        MelonLogger.Msg("Target Unlocked.");
                    }
                }
            });

            category.CreateFunction("❌ Reset Lock-In", Color.red, () => 
            {
                isLockedIn = false;
                currentTargetTransform = null;
                MelonLogger.Msg("Lock-In Reset.");
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
                            targetReticle.SetActive(true);
                        }
                    }
                    else if (!isLockedIn && targetReticle != null)
                    {
                        targetReticle.SetActive(false);
                    }
                }
            }

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
                GameObject.Destroy(col);
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
