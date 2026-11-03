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

                // Check if user is pointing at something via raycast
                if (Physics.Raycast(pointerTransform.position, pointerTransform.forward, out RaycastHit hit, 100f))
                {
                    // If locked in, track moving targets smoothly without freezing the game
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
                        targetPosition = hit.point;
                        if (targetReticle != null)
                        {
                            targetReticle.transform.position = targetPosition;
                            targetReticle.transform.LookAt(cam.transform);
                            targetReticle.SetActive(true);
                        }

                        // Offhand trigger press activation simulation (using Menu button or alternate input if controller mapping is custom)
                        // This allows you to press your offhand trigger/button to lock onto moving objects or people permanently
                        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton3))
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg(string.Format("Locked onto moving target: {0}", hit.transform.name));
                        }
                    }
                }
                else if (!isLockedIn && targetReticle != null)
                {
                    targetReticle.SetActive(false);
                }
            }

            // Smooth homing physics toward locked moving target
            if (currentThrownBody != null && isLockedIn)
            {
                if (currentTargetTransform != null)
                {
                    targetPosition = currentTargetTransform.position;
                }

                Vector3 directionToTarget = (targetPosition - currentThrownBody.position).normalized;
                float currentSpeed = Mathf.Max(currentThrownBody.velocity.magnitude, 15f);
                currentThrownBody.velocity = Vector3.Lerp(currentThrownBody.velocity, directionToTarget * currentSpeed, Time.deltaTime * 15f);
            }
        }

        private static void CreateTargetReticle()
        {
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HomingCrosshairReticle";
            targetReticle.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                GameObject.Destroy(col);
            }

            Renderer renderer = targetReticle.GetComponent<Renderer>();
            
