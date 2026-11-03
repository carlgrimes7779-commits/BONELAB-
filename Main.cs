using System;
using UnityEngine;
using MelonLoader;
using BoneLib;
using BoneLib.BoneMenu;

[assembly: MelonInfo(typeof(HomingThrowQuest.HomingThrowMod), "Homing Throw Quest", "1.2.0", "YourName")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace HomingThrowQuest
{
    public class HomingThrowMod : MelonMod
    {
        private GameObject crosshairObject;
        private Material crosshairMaterial;

        private Transform targetHead;
        private Rigidbody lastThrownRigidbody;

        private float originalThrowSpeed = 15f;

        private bool isLockedInMode = false;
        private bool isTargetLocked = false;

        private float lockDistance = 40f;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Homing Throw Mod Loaded!");

            Page mainPage = Page.Create("Homing Throw Mod");

            mainPage.CreateBool(
                "Enable 'Locked In' Mode",
                Color.cyan,
                isLockedInMode,
                (value) =>
                {
                    isLockedInMode = value;

                    if (!isLockedInMode)
                    {
                        ResetLock();
                    }
                }
            );

            crosshairMaterial = new Material(
                Shader.Find("Sprites/Default")
            );

            crosshairMaterial.color = Color.red;
        }

        public override void OnUpdate()
        {
            if (Player.rigManager == null)
                return;

            Transform offhand = Player.leftHand != null
                ? Player.leftHand.transform
                : null;

            if (offhand == null)
                return;

            UpdateCrosshairVisual(offhand);

            if (isLockedInMode)
            {
                bool isTriggerPressed =
                    Player.leftController != null &&
                    Player.leftController.GetIndexTouchpad();

                if (isTriggerPressed && !isTargetLocked)
                {
                    TryLockOntoHead(offhand);
                }
                else if (!isTriggerPressed && isTargetLocked)
                {
                    ResetLock();
                }
            }
        }

        private void UpdateCrosshairVisual(Transform hand)
        {
            if (crosshairObject == null)
            {
                crosshairObject = GameObject.CreatePrimitive(
                    PrimitiveType.Sphere
                );

                crosshairObject.name = "HomingThrowCrosshair";

                Collider col = crosshairObject.GetComponent<Collider>();

                if (col != null)
                    UnityEngine.Object.Destroy(col);

                Renderer renderer =
                    crosshairObject.GetComponent<Renderer>();

                if (renderer != null && crosshairMaterial != null)
                {
                    renderer.material = crosshairMaterial;
                }

                crosshairObject.transform.localScale =
                    Vector3.one * 0.08f;
            }

            crosshairObject.transform.position =
                hand.position + hand.forward * 5f;

            crosshairObject.transform.rotation =
                hand.rotation;
        }

        private void TryLockOntoHead(Transform hand)
        {
            // Target detection still needs to be connected
            // to BONELAB's NPC/head references.

            targetHead = null;

            isTargetLocked = false;
        }

        private void ResetLock()
        {
            isTargetLocked = false;
            targetHead = null;
        }

        public override void OnDeinitializeMelon()
        {
            if (crosshairObject != null)
            {
                UnityEngine.Object.Destroy(crosshairObject);
                crosshairObject = null;
            }

            if (crosshairMaterial != null)
            {
                UnityEngine.Object.Destroy(crosshairMaterial);
                crosshairMaterial = null;
            }

            LoggerInstance.Msg("Homing Throw Mod Unloaded!");
        }
    }
}