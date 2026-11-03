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

            category.CreateFunction("🎯 Lock Target (Offhand Action)", Color.green, () => 
            {
                Camera cam = Camera.main;
                if (cam == null) return;

                int layerMask = ~((1 << 2) | (1 << 4) | (1 << 20)); // Ignores triggers/spawn volumes
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 200f, layerMask))
                {
                    if (hit.transform != null)
                    {
                        isLockedIn = true;
                        currentTargetTransform = hit.transform;
                        MelonLogger.Msg("Locked onto target: " + hit.transform.name);
                    }
                }
            });

            category.CreateFunction("❌ Reset / Delete Lock", Color.red, () => 
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

            Vector3 offhandOrigin = cam.transform.position + (cam.transform.right * -0.25f) + (cam.transform.forward * 0.2f) - (cam.transform.up * 0.15f);
            Vector3 aimDirection = cam.transform.forward;

            int layerMask = ~((1 << 2) | (1 << 4) | (1 << 20));
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(offhandOrigin, aimDirection, out hit, 200f, layerMask);

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

            if (isLockedIn && targetPosition != Vector3.zero)
            {
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
                    for (int i = 0; i < allBodies.Length; i++)
                    {
                        Rigidbody rb = allBodies[i];
                        if (rb != null && rb.velocity.magnitude > 4f && rb.gameObject != targetReticle && rb.gameObject != laserLineObj && rb.position.sqrMagnitude > 1f)
                        {
                            string objName = rb.gameObject.name.ToLower();
                            bool isBluntOrBladed = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || objName.Contains("hammer") || objName.Contains("blade") || objName.Contains("melee") || objName.Contains("prop") || rb.mass < 15f;

                            if (isBluntOrBladed)
                            {
                                trackedThrownObject = rb;
                                throwStartTime = Time.time;
                                MelonLogger.Msg("Tracking weapon: " + rb.gameObject.name);
                                break;
                            }
                        }
                    }
                }

                if (trackedThrownObject != null)
                {
                    if (Time.time - throwStartTime > 4f)
                    {
                        trackedThrownObject = null;
                        return;
                    }

                    if (currentTargetTransform != null)
                    {
                        targetPosition = currentTargetTransform.position;
                    }

                    Vector3 directionToTarget = (targetPosition - trackedThrownObject.position).normalized;
                    float currentSpeed = Mathf.Max(trackedThrownObject.velocity.magnitude, 20f);
                    trackedThrownObject.velocity = Vector3.Slerp(trackedThrownObject.velocity, directionToTarget * currentSpeed, Time.deltaTime * 25f);
                }
            }
        }

        private static void ResetHomingState()
        {
            isLockedIn = false;
            currentTargetTransform
