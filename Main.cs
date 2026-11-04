using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;
using UnityEngine;

[assembly: MelonInfo(typeof(HomingThrowMod.HomingThrowMod), "Homing throw quest", "1.1.1", "Author")]
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

        // Steering configuration
        private const float STEERING_STRENGTH = 18f;
        private const float MAX_HOMING_FLIGHT_TIME = 5f;
        private const float MIN_THROW_SPEED = 1.2f;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod v1.1.1 Initialized.");

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

            // Target Validation Check on Raycast Hover
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
                            MelonLogger.Msg("Hover locked onto valid target: " + hit.transform.name);
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

            // Homing Execution Loop
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
                        float currentSpeed = rb.velocity.magnitude;
                        Vector3 throwDir = rb.velocity.normalized;
                        Vector3 awayFromPlayerDir = (rb.position - cam.transform.position).normalized;

                        bool isNearPlayer = distToPlayer < 2.5f;
                        bool hasReliableSpeed = currentSpeed >= MIN_THROW_SPEED;
                        bool isMovingOutward = Vector3.Dot(throwDir, awayFromPlayerDir) > 0.25f;

                        if (isNearPlayer && hasReliableSpeed && isMovingOutward)
                        {
                            string objName = rb.gameObject.name.ToLower();
                            bool isThrowWeapon = objName.Contains("knife") || objName.Contains("sword") || objName.Contains("axe") || 
                                                 objName.Contains("hammer") || objName.Contains("blade") || objName.Contains("melee") || 
                                                 objName.Contains("prop") || rb.mass < 18f;

                            if (isThrowWeapon)
                            {
                                trackedThrownObject = rb;
                                throwStartTime = Time.time;
                                MelonLogger.Msg("Homing tracking engaged for: " + rb.gameObject.name);
                                break;
                            }
                        }
                    }
                }

                if (trackedThrownObject != null)
                {
                    if (Time.time - throwStartTime > MAX_HOMING_FLIGHT_TIME)
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

                    // Relinquish control right before impact for hit physics registration
                    if (distanceToTarget < 0.8f)
                    {
                        trackedThrownObject = null;
                        return;
                    }

                    // Continuous Steering Force Calculation
                    float currentSpeed = Mathf.Max(trackedThrownObject.velocity.magnitude, 22f);
                    Vector3 desiredVelocity = toTarget.normalized * currentSpeed;
                    
                    Vector3 steeringForce = Vector3.ClampMagnitude(desiredVelocity - trackedThrownObject.velocity, STEERING_STRENGTH);
                    trackedThrownObject.velocity += steeringForce * Time.deltaTime;

                    if (trackedThrownObject.velocity.sqrMagnitude > 0.1f)
                    {
                        trackedThrownObject.rotation = Quaternion.Slerp(
                            trackedThrownObject.rotation, 
                            Quaternion.LookRotation(trackedThrownObject.velocity), 
                            Time.deltaTime * 14f
                        );
                    }
                }
            }
        }

        private static bool IsValidTarget(Transform target, Camera cam)
        {
            if (target == null) return false;

            // Prevent self-targeting by camera/head proximity or player root naming
            if (Vector3.Distance(target.position, cam.transform.position) < 1.2f)
            {
                return false;
            }

            string lowerName = target.name.ToLower();
            if (lowerName.Contains("player") || lowerName.Contains("head") || lowerName.Contains("avatar") || 
                lowerName.Contains("fusion") || lowerName.Contains("network") || lowerName.Contains("synced"))
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
