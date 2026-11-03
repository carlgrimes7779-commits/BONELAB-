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

            category.CreateFunction("🎯 Lock Target (Right Hand Aim)", Color.green, () => 
            {
                Camera cam = Camera.main;
                if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 200f))
                {
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg("Locked target: " + hit.transform.name);
                }
            });

            category.CreateFunction("❌ Delete / Reset Lock (Offhand Action)", Color.red, () => 
            {
                ResetHomingState();
                MelonLogger.Msg("Lock-In state reset.");
            });
        }

        public override void OnUpdate()
        {
            if (!isHomingEnabled) return;

            if (targetReticle == null) CreateCrosshairReticle();
            if (laserLineObj == null) CreateLaserLine();

            Camera cam = Camera.main;
            if (cam == null) return;

            Transform pointer = cam.transform;

            if (isLockedIn && currentTargetTransform != null)
            {
                targetPosition = currentTargetTransform.position;
                if (targetReticle != null)
                {
                    targetReticle.transform.position = targetPosition;
                    targetReticle.transform.LookAt(cam.transform);
                    targetReticle.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                    targetReticle.SetActive(true);
                }
                if (laserLine != null) laserLine.enabled = false;
            }
            else
            {
                if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, 200f))
                {
                    targetPosition = hit.point;

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
                }
                else
                {
                    if (laserLine != null) laserLine.enabled = false;
                    if (targetReticle != null) targetReticle.SetActive(false);
                }
            }

            if (isLockedIn && targetPosition != Vector3.zero)
            {
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = Object.FindObjectsOfType<Rigidbody>();
                    for (int i = 0; i < allBodies.Length; i++)
                    {
                        Rigidbody rb = allBodies[i];
                        if (rb != null && rb.velocity.magnitude > 3f && rb.gameObject != targetReticle && rb.gameObject != laserLineObj)
                        {
                            string objName = rb.gameObject.name.ToLower();
                            bool isBluntOrBladed = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || 
                                                 objName.Contains("hammer") || objName.Contains("blade") || objName.Contains("melee") || 
                                                 objName.Contains("prop") || rb.mass < 15f;

                            if (isBluntOrBladed)
                            {
                                trackedThrownObject = rb;
                                MelonLogger.Msg("Tracking blunt/bladed weapon: " + rb.gameObject.name);
                                break;
                            }
                        }
                    }
                }

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
            targetReticle.name = "HomingCrosshair";
            targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

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
