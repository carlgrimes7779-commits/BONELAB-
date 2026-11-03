using MelonLoader;
using BoneLib;
using UnityEngine;

[assembly: MelonInfo(typeof(HomingThrowMod.HomingThrowMod), "Homing throw quest", "1.0.0", "Author")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace HomingThrowMod
{
    public class HomingThrowMod : MelonMod
    {
        public static bool isLockedIn = false;

        private static GameObject? targetReticle;
        private static Transform? currentTargetTransform;
        private static Vector3 targetPosition;
        private static Rigidbody? currentThrownBody;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Homing Throw Mod Initialized Successfully.");
        }

        public override void OnUpdate()
        {
            // Check if player is holding an item in either hand
            bool isHoldingItem = false;
            Transform? offhandTransform = null;

            if (Player.PhysicsRig != null)
            {
                // Determine active holding state and offhand pointer
                bool rightHolding = Player.RightHand != null && Player.RightHand.HasJointGrab();
                bool leftHolding = Player.LeftHand != null && Player.LeftHand.HasJointGrab();

                if (rightHolding)
                {
                    isHoldingItem = true;
                    offhandTransform = Player.LeftHand != null ? Player.LeftHand.transform : null;
                }
                else if (leftHolding)
                {
                    isHoldingItem = true;
                    offhandTransform = Player.RightHand != null ? Player.RightHand.transform : null;
                }
            }

            // If not holding anything, hide the reticle and unlock target
            if (!isHoldingItem)
            {
                if (targetReticle != null) targetReticle.SetActive(false);
                isLockedIn = false;
                currentTargetTransform = null;
                return;
            }

            // Ensure our visible bullseye/crosshair reticle exists
            if (targetReticle == null)
            {
                CreateBullseyeReticle();
            }

            // Use offhand controller position/rotation as the aiming pointer
            Transform pointer = offhandTransform != null ? offhandTransform : (Camera.main != null ? Camera.main.transform : null);
            if (pointer == null) return;

            // Handle Lock-In Trigger Logic from offhand controller
            // (Using Button/Trigger input check supported safely in MelonLoader)
            bool triggerClicked = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton14);

            if (isLockedIn && currentTargetTransform != null)
            {
                // Stick permanently to the locked moving target
                targetPosition = currentTargetTransform.position;
                if (targetReticle != null)
                {
                    targetReticle.transform.position = targetPosition;
                    targetReticle.transform.LookAt(Camera.main != null ? Camera.main.transform : pointer);
                    
                    // Scale dynamically based on distance from player camera for consistent visual sizing
                    if (Camera.main != null)
                    {
                        float dist = Vector3.Distance(Camera.main.transform.position, targetPosition);
                        float scale = Mathf.Clamp(dist * 0.08f, 0.15f, 1.5f);
                        targetReticle.transform.localScale = new Vector3(scale, scale, scale);
                    }
                    targetReticle.SetActive(true);
                }
            }
            else
            {
                // Raycast from offhand pointer to find targets/surfaces
                if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, 150f))
                {
                    targetPosition = hit.point;
                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(Camera.main != null ? Camera.main.transform : pointer);

                        if (Camera.main != null)
                        {
                            float dist = Vector3.Distance(Camera.main.transform.position, targetPosition);
                            float scale = Mathf.Clamp(dist * 0.08f, 0.15f, 1.5f);
                            targetReticle.transform.localScale = new Vector3(scale, scale, scale);
                        }
                        targetReticle.SetActive(true);

                        // Click offhand trigger to lock in permanently on object or person
                        if (triggerClicked)
                        {
                            isLockedIn = true;
                            currentTargetTransform = hit.transform;
                            MelonLogger.Msg($"Locked onto target: {hit.transform.name}");
                        }
                    }
                }
                else
                {
                    // Default projection if ray hits nothing
                    targetPosition = pointer.position + (pointer.forward * 5.0f);
                    if (targetReticle != null)
                    {
                        targetReticle.transform.position = targetPosition;
                        targetReticle.transform.LookAt(pointer);
                        targetReticle.SetActive(true);
                    }
                }
            }

            // Apply homing trajectory force to any active thrown object toward the locked target
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
            // Create a high-visibility procedural bullseye crosshair object
            targetReticle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetReticle.name = "HomingBullseyeReticle";
            targetReticle.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Remove collider so it doesn't interfere with physics
            Collider col = targetReticle.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.Destroy(col);
            }

            // Apply bright glowing cyan shader so it stands out clearly across any map perspective
            Renderer renderer = targetReticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader sh = Shader.Find("Unlit/Color");
                if (sh != null)
                {
                    renderer.material.shader = sh;
                    renderer.material.color = new Color(0f, 1f, 1f, 1f); // Vibrant cyan
                }
            }

            targetReticle.SetActive(false);
            MelonLogger.Msg("Bullseye crosshair reticle generated successfully.");
        }
    }
}
