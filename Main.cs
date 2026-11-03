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
        private static GameObject? laserLineObj;
        private static LineRenderer? laserLine;
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
                if (!isHomingEnabled)
                {
                    ResetHomingState();
                }
            });

            category.CreateFunction("❌ Reset Target (Offhand Trigger Alternative)", Color.red, () => 
            {
                ResetHomingState();
                MelonLogger.Msg("Lock-In Deleted / Reset.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            // Initialize visual elements if missing
            if (targetReticle == null) CreateCrosshairReticle();
            if (laserLineObj == null) CreateLaserLine();

            Camera cam = Camera.main;
            if (cam == null) return;

            // Use camera/head or simulated hand pointers for standalone stability
            Transform pointer = cam.transform;

            // 1. Offhand Trigger Action: Delete / Unlock Lock-In Mode
            bool offhandDeleteTrigger = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton1);
            if (offhandDeleteTrigger && isLockedIn)
            {
                ResetHomingState();
                MelonLogger.Msg("Lock-in deleted via offhand trigger action.");
            }

            // 2. Right Hand / Primary Trigger Action: Lock In Target
            bool rightHandLockTrigger = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0);

            if (isLockedIn && currentTargetTransform != null)
            {
                targetPosition = currentTargetTransform.position;
                if (targetReticle != null)
                {
                    targetReticle.transform.position = targetPosition;
                    targetReticle.transform.LookAt(cam.transform);
                    targetReticle.SetActive(true);
                }
                if (laserLine != null) laserLine.enabled = false;
            }
            else
            {
                if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, 200f))
                {
                    targetPosition = hit.point;

                    // Update laser line from pointer to hit point
                    if (laserLine != null)
                    {
                        laserLine.enabled = true;
                        laserLine.SetPosition(0, pointer.position);
                        laserLine.SetPosition(1, targetPosition);
                    }

                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(cam.transform);

                        float dist = Vector3.Distance(pointer.position, targetPosition);
                        float scale = Mathf.Clamp(dist * 0.05f, 0.2f, 1.5f);
                        targetReticle.transform.localScale = new Vector3(scale, scale, scale);
                        targetReticle.SetActive(true);
                    }

                    // Lock-in triggered by right hand
                    if (rightHandLockTrigger)
                    {
                        isLockedIn = true;
                        currentTargetTransform = hit.transform;
                        MelonLogger.Msg($"Locked onto target: {hit.transform.name}");
                    }
                }
                else
                {
                    if (laserLine != null) laserLine.enabled = false;
                    if (targetReticle != null) targetReticle.SetActive(false);
                }
            }

            // 3. Homing Physics for Blunt and Bladed Objects Only
            if (isLockedIn && targetPosition != Vector3.zero)
            {
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = GameObject.FindObjectsOfType<Rigidbody>();
                    foreach (Rigidbody rb in allBodies)
                    {
                        if (rb != null && rb.velocity.magnitude > 3f && rb.gameObject != targetReticle && rb.gameObject != laserLineObj)
                        {
                            // Filter specifically for blunt/bladed handheld objects (knives, melee items, physics props)
                            string objName = rb.gameObject.name.ToLower();
                            bool isBluntOrBladed = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || 
                                                 objName.Contains("hammer") || objName.Contains("melee") || objName.Contains("prop") || 
                                                 objName.Contains("blunt") || rb.mass < 15f;

                            if (isBluntOrBladed)
                            {
                                trackedThrownObject = rb;
                                MelonLogger.Msg($"Tracking thrown blunt/bladed object: {rb.gameObject.name}");
                                break;
                            }
                        }
                    }
                }

                // Apply high-speed curve toward the target
                if (trackedThrownObject != null)
                {
                    if (currentTargetTransform != null)
                    {
                        targetPosition = currentTargetTransform.position;
                    }

                    Vector3 directionToTarget = (targetPosition - trackedThrownObject.position).normalized;
                    float boostedSpeed = Mathf.Max(trackedThrownObject.velocity.magnitude * 1.5f, 25f);
                    
                    trackedThrownObject.velocity = Vector3.Slerp(trackedThrownObject.velocity, directionToTarget * boostedSpeed, Time.deltaTime * 20f);
                }
            }
        }

        private static void ResetHomingState()
        {
            isLockedIn = false;
            currentTargetTransform = null;
            trackedThrownObject = null;
            if (targetReticle != null) targetReticle.SetActive(false);
            if (laserLine != null) laserLine.enabled = false;
        }

        private static void CreateCrosshairReticle()
        {
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HomingCrosshairBullseye";
            targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader sh = Shader.Find("Unlit/Color");
                if (sh != null)
                {
                    renderer.material.shader = sh;
                    renderer.material.color = new Color(0f, 1f, 1f, 1f); // Bright Cyan
                }
            }
            targetReticle.SetActive(false);
        }

        private static void CreateLaserLine()
        {
            laserLineObj = new GameObject("HomingLaserPointer");
            laserLine = laserLineObj.AddComponent<LineRenderer>();
            laserLine.startWidth = 0.02f;
            laserLine.endWidth = 0.02f;
            laserLine.positionCount = 2;

            Shader sh = Shader.Find("Unlit/Color");
            if (sh != null)
            {
                laserLine.material = new Material(sh);
                laserLine.material.color = Color.cyan;
            }
            laserLine.enabled = false;
        }
    }
}
