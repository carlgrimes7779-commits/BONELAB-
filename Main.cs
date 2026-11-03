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
                if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 150f))
                {
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg($"[HomingMod] Permanently locked target: {hit.transform.name}");
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
                CreateBullseyeReticle();
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

                        // Dynamic scale by camera distance so the reticle stays visible everywhere on the map
                        float dist = Vector3.Distance(cam.transform.position, targetPosition);
                        float scale = Mathf.Clamp(dist * 0.08f, 0.2f, 2.0f);
                        targetReticle.transform.localScale = new Vector3(scale, scale, scale);

                        targetReticle.SetActive(true);
                    }
                }
                else
                {
                    if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 150f))
                    {
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.transform.LookAt(cam.transform);

                            float dist = Vector3.Distance(cam.transform.position, targetPosition);
                            float scale = Mathf.Clamp(dist * 0.08f, 0.2f, 2.0f);
                            targetReticle.transform.localScale = new Vector3(scale, scale, scale);

                            targetReticle.SetActive(true);
                        }
                    }
                    else
                    {
                        if (targetReticle != null && !isLockedIn)
                        {
                            targetReticle.SetActive(false);
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
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 20f);
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 20f);
            }
        }

        private static void CreateBullseyeReticle()
        {
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HomingBullseyeReticle";
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
                    renderer.material.color = new Color(0f, 1f, 1f, 1f); // Glowing bright cyan
                }
            }

            targetReticle.SetActive(false);
            MelonLogger.Msg("Bullseye reticle created successfully.");
        }
    }
}
