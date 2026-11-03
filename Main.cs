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

        // Double-tap timer logic for Left Joystick Click (L3)
        private static float lastClickTime = 0f;
        private static bool wasStickPressedLastFrame = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized.");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled) ResetHomingState();
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

            // Handle controller thumbstick double-tap via BoneLib Player API safely
            CheckStickDoubleTap(cam, hitSomething, hit);

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

            // Homing execution block
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
                            Vector3 toPlayer = (cam.transform.position - rb.position).normalized;
                            
                            // Check 1: Must originate within 2m of player (in hands)
                            bool originatesNearPlayer = Vector3.Distance(rb.position, cam.transform.position) < 2.0f;
                            // Check 2: Must be moving fast (>5m/s)
                            bool isMovingFast = rb.velocity.magnitude > 5.0f;
                            // Check 3: MUST NOT be traveling TOWARDS the player (prevents force-grabbing interference)
                            bool isMovingAwayFromPlayer = Vector3.Dot(rb.velocity.normalized, toPlayer) < 0.2f;

                            if (originatesNearPlayer && isMovingFast && isMovingAwayFromPlayer)
                            {
                                string objName = rb.gameObject.name.ToLower();
                                bool isBluntOrBladed = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || objName.Contains("hammer") || objName.Contains("blade") || objName.Contains("melee") || objName.Contains("prop") || rb.mass < 15f;

                                if (isBluntOrBladed)
                                {
                                    trackedThrownObject = rb;
                                    throwStartTime = Time.time;
                                    MelonLogger.Msg("Tracking thrown weapon: " + rb.gameObject.name);
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

                    // Release homing control close to target for direct physics impact
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

        private static void CheckStickDoubleTap(Camera cam, bool hitSomething, RaycastHit hit)
        {
            try
            {
                if (Player.leftController != null)
                {
                    bool isPressedThisFrame = Player.leftController.GetThumbstickTouch() && Player.leftController.GetThumbstickAxis().magnitude > 0.8f;

                    if (isPressedThisFrame && !wasStickPressedLastFrame)
                    {
                        if (Time.time - lastClickTime < 0.4f)
                        {
                            // Double tap detected!
                            if (isLockedIn)
                            {
                                ResetHomingState();
                                MelonLogger.Msg("Lock
