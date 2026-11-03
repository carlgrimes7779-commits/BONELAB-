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

            int layerMask = -18;
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(offhandOrigin, aimDirection, out hit, 200f, layerMask, QueryTriggerInteraction.Ignore);

            CheckControllerInput(cam, hitSomething, hit);

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
