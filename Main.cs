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
                    Rigidbody[] allBodies = GameObject.FindObjectsOfType<Rigidbody>();
                    foreach (Rigidbody rb in allBodies)
                    {
                        if (rb != null && rb.velocity.magnitude > 3f && rb.gameObject != targetReticle && rb.gameObject != laserLineObj)
                        {
                            string
