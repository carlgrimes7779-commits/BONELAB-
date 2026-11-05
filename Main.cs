using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;

[assembly: MelonInfo(typeof(HomingThrowMod.HomingThrowMod), "Homing throw quest", "1.2.0", "Author")]
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
        private static float throwStartTime = 0f;

        private static Transform? hoveredTransform;
        private static float hoverStartTime = 0f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod v1.2.0 Initialized.");

            Page category = Page.Root.CreatePage("Homing Throw Quest", Color.yellow);
            category.CreateBool("Enable Homing", Color.cyan, isHomingEnabled, (value) => 
            {
                isHomingEnabled = value;
                if (!isHomingEnabled) ResetHomingState();
            });

            category.CreateFunction("🎯 Force Lock Target", Color.green, () => 
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

            int layerMask = ~18;
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(offhandOrigin, aimDirection, out hit, 200f, layerMask);

            // Hover lock-on calculation
            if (!isLockedIn && hitSomething && hit.transform != null)
            {
                if (IsValidTarget(hit.transform, cam))
                {
                    if (hoveredTransform == hit.transform)
                    {
                        if (Time.time - hoverStartTime > 0.75f)
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg("Hover locked onto target: " + hit.transform.name);
                        }
                    }
                    else
                    {
                        hoveredTransform = hit.transform;
                        hoverStartTime = Time.time;
                    }
                }
            }
            else if (!hitSomething && !isLockedIn)
            {
                hoveredTransform = null;
            }

            // Crosshair update
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
                if (hitSomething && hit.transform != null && IsValidTarget(hit.transform, cam))
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

            // Instantaneous Release & Curve Homing Loop
            if (isLockedIn && targetPosition != Vector3.zero)
            {
                if (trackedThrownObject == null)
                {
                    Rigidbody[] allBodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
                    for (int i = 0; i < allBodies.Length; i++)
                    {
                        Rigidbody rb = allBodies[i];
                        if (rb == null || rb.gameObject == targetReticle || rb.gameObject == laserLineObj) continue;

                        float distToPlayer = Vector3.Distance(rb.position, cam.transform.position);

                        // Triggers instantly the moment an object near the player moves with any speed (> 0.2 m/s)
                        if (distToPlayer < 2.0f && rb.velocity.magnitude > 0.2f)
                        {
                            if (IsValidTarget(rb.transform, cam))
                            {
                                trackedThrownObject = rb;
                                throwStartTime = Time.time;
                                MelonLogger.Msg("Instant homing engaged for released item: " + rb.gameObject.name);
                                break;
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

                    Vector3 currentPos = trackedThrownObject.position;
                    Vector3 toTarget = targetPosition - currentPos;
                    float distanceToTarget = toTarget.magnitude;

                    // Release control right before impact for clean damage registration
                    if (distanceToTarget < 0.7f)
                    {
                        trackedThrownObject = null;
                        return;
                    }

                    // Obstacle avoidance & curve calculation via Raycast anticipation
                    Vector3 desiredDir = toTarget.normalized;
                    RaycastHit obstacleHit;
                    if (Physics.Raycast(currentPos, desiredDir, out obstacleHit, Mathf.Min(distanceToTarget, 3.0f), ~18))
                    {
                        // If an obstacle is directly in the path, bend the steering vector upward and around it
                        if (obstacleHit.transform != currentTargetTransform && !obstacleHit.transform.IsChildOf(currentTargetTransform))
                        {
                            desiredDir = (desiredDir + Vector3.up * 1.5f).normalized;
                        }
                    }

                    float flightSpeed = Mathf.Max(trackedThrownObject.velocity.magnitude, 28f);
                    Vector3 targetVelocity = desiredDir * flightSpeed;

                    // High steering agility forces immediate curve response
                    Vector3 steeringForce = Vector3.ClampMagnitude(targetVelocity - trackedThrownObject.velocity, 35f);
                    trackedThrownObject.velocity += steeringForce * Time.deltaTime;

                    if (trackedThrownObject.velocity.sqrMagnitude > 0.1f)
                    {
                        trackedThrownObject.rotation = Quaternion.Slerp(
                            trackedThrownObject.rotation, 
                            Quaternion.LookRotation(trackedThrownObject.velocity), 
                            Time.deltaTime * 20f
                        );
                    }
                }
            }
        }

        private static bool IsValidTarget(Transform target, Camera cam)
        {
            if (target == null) return false;

            if (Vector3.Distance(target.position, cam.transform.position) < 1.2f)
            {
                return false;
            }

            string lowerName = target.name.ToLower();
            if (lowerName.Contains("player") || lowerName.Contains("head") || lowerName.Contains("avatar") || 
                lowerName.Contains("fusion") || lowerName.Contains("network") || lowerName.Contains("synced") ||
                lowerName.Contains("homing"))
            {
                return false;
            }

            return true;
        }

        private static void LockTargetFromPointer()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            int layerMask = ~18;
            RaycastHit hit;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 200f, layerMask))
            {
                if (hit.transform != null && IsValidTarget(hit.transform, cam))
                {
                    isLockedIn = true;
                    currentTargetTransform = hit.transform;
                    MelonLogger.Msg("Forced lock onto target: " + hit.transform.name);
                }
            }
        }

        private static void ResetHomingState()
        {
            isLockedIn = false;
            currentTargetTransform = null;
            hoveredTransform = null;
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
