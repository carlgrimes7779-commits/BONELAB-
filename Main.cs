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
            MelonLogger.Msg("Homing Throw Mod Initialized Successfully.");

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

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null)
            {
                CreateHolographicReticle();
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
                    }
                    else
                    {
                        targetPosition = pointerTransform.position + (pointerTransform.forward * 3.0f);
                        if (targetReticle != null && !isLockedIn)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.transform.LookAt(cam.transform);
                            targetReticle.SetActive(true);
                        }
                    }
                }
            }

            if (currentThrownBody != null && isLockedIn)
            {
                if (currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                }

                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 18f);
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 18f);
            }
        }

        private static void CreateHolographicReticle()
        {
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HolographicCrosshair";
            targetReticle.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.Destroy(col);
            }

            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.shader = Shader.Find("Unlit/Color");
                renderer.material.color = new Color(0f, 1f, 1f, 1f);
            }

            targetReticle.SetActive(false);
        }
    }
}
