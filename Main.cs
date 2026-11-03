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
        private static GameObject laserLineObj;
        private static LineRenderer laserLine;
        private static Transform currentTargetTransform;
        private static Vector3 targetPosition;
        private static Rigidbody trackedThrownObject;
        private static float throwStartTime = 0f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized.");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled) ResetHomingState();
            });

            category.CreateFunction("🎯 Lock Target (Aim & Click)", Color.green, () => 
            {
                LockTargetFromPointer();
            });

            category.CreateFunction("❌ Reset / Clear Lock", Color.red, () => 
            {
                ResetHomingState();
                MelonLogger.Msg("Lock-In state cleared.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null) CreateCrosshairReticle();
            if (laserLineObj == null) CreateLaserLine();

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 offhandOrigin = cam.transform.position + (cam.transform.right * -0.25f) + (cam.transform.forward * 0.2f) - (cam.transform.up * 0.15f);
            Vector3 aimDirection = cam.transform.forward;

            int layerMask = -18;
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(offhandOrigin, aimDirection, out hit, 200f, layerMask, QueryTriggerInteraction.Ignore);

            if (isLockedIn && currentTargetTransform != null)
            {
                targetPosition = currentTargetTransform.position;
                if (targetReticle != null)
                {
                    targetReticle.transform.position = targetPosition;
                    targetReticle.transform.LookAt(cam.transform);
                    targetReticle.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    targetReticle.SetActive(true);
                }
                if (laserLine != null) laserLine.enabled = false;
            }
            else
            {
                if (hitSomething)
                {
                    targetPosition = hit.point;

                    if (laserLine != null)
                    {
                        laserLine.enabled = true;
                        laserLine.SetPosition(0, offhandOrigin);
                        laserLine.SetPosition(1, targetPosition);
                    }

                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(cam.transform);

                        float dist = Vector3.Distance(offhandOrigin, targetPosition);
                        float scale = Mathf.Clamp(dist * 0.05f, 0.2f, 1.5f);
                        targetReticle.transform.localScale = new Vector3(scale, scale, scale);
                        targetReticle.SetActive(true);
                    }
                }
                else
                {
                    if (laserLine != null) laserLine.enabled = false;
                    if (targetReticle != null) targetReticle.SetActive(false);
                }
            }

            // Homing execution block with strict hand-throw verification (<2m origin & >6 speed)
            if (isLockedIn && targetPosition != Vector3.zero)
            {
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
                    for (int i = 0; i < allBodies.Length; i++)
                    {
                        Rigidbody rb = allBodies[i];
                        if (rb != null && rb.gameObject != targetReticle && rb.gameObject != laserLineObj)
                        {
                            // STRICT CHECK: Must be moving fast (>6f) AND originate near the player's hands (<2m) to confirm it was actively thrown
                            bool isThrownFromHand = rb.velocity.magnitude > 6.0f && Vector3.Distance(rb.position, cam.transform.position) < 2.0f;

                            if (isThrownFromHand)
                            {
                                string objName = rb.gameObject.name.ToLower();
                                bool isBluntOrBladed = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || objName.Contains("hammer") || objName.Contains("blade") || objName.Contains("melee") || objName.Contains("prop") || rb.mass < 15f;

                                if (isBluntOrBladed)
                                {
                                    trackedThrownObject = rb;
                                    throwStartTime = Time.time;
                                    MelonLogger.Msg("Tracking hand-thrown weapon: " + rb.gameObject.name);
                                    break;
                                }
                            }
                        }
                    }
                }

                if (trackedThrownObject != null)
                {
                    if (Time.time - throwStartTime > 6f)
                    {
                        trackedThrownObject = null;
                        return;
                    }

                    if (currentTargetTransform != null)
                    {
                        targetPosition = currentTargetTransform.position;
                    }

                    Vector3 toTarget = targetPosition - trackedThrownObject.position;
                    float distanceToTarget = toTarget.magnitude;

                    // Release homing control close to the target for clean, hard impacts
                    if (distanceToTarget < 0.75f)
                    {
                        trackedThrownObject = null;
                        return;
                    }

                    float currentSpeed = Mathf.Max(trackedThrownObject.velocity.magnitude, 25f);
                    Vector3 desiredVelocity = toTarget.normalized * currentSpeed;

                    trackedThrownObject.velocity = Vector3.Lerp(trackedThrownObject.velocity, desiredVelocity, Time.deltaTime * 18f);

                    if (trackedThrownObject.velocity.sqrMagnitude > 0.1f)
                    {
                        trackedThrownObject.rotation = Quaternion.Slerp(trackedThrownObject.rotation, Quaternion.LookRotation(trackedThrownObject.velocity), Time.deltaTime * 12f);
                    }
                }
            }
        }

        private static void LockTargetFromPointer()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            int layerMask = -18;
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 200f, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != null)
                {
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg("Locked onto target: " + hit.transform.name);
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
            targetReticle.name = "HomingCrosshair";
            targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader sh = Shader.Find("GUI/Text Shader");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                
                if (sh != null)
                {
                    renderer.material = new Material(sh);
                    renderer.material.color = new Color(0f, 1f, 1f, 1f);
                }
            }
            targetReticle.SetActive(false);
        }

        private static void CreateLaserLine()
        {
            laserLineObj = new GameObject("HomingLaserPointer");
            laserLine = laserLineObj.AddComponent<LineRenderer>();
            laserLine.startWidth = 0.015f;
            laserLine.endWidth = 0.015f;
            laserLine.positionCount = 2;

            Shader sh = Shader.Find("GUI/Text Shader");
            if (sh == null) sh = Shader.Find("Unlit/Color");

            if (sh != null)
            {
                laserLine.material = new Material(sh);
                laserLine.material.color = new Color(0f, 1f, 1f, 1f);
            }
            laserLine.enabled = false;
        }
    }
}
