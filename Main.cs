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
            mainPage.CreateBool("Enable 'Locked In' Mode", Color.cyan, isLockedInMode, (value) => 
            {
                isLockedInMode = value;
                if (!isLockedInMode) ResetLock();
            });
        }

        public override void OnUpdate()
        {
            if (Player.rigManager == null) return;

            Transform offhand = Player.leftHand != null ? Player.leftHand.transform : null;
            if (offhand != null)
            {
                UpdateCrosshairVisual(offhand);

                if (isLockedInMode)
                {
                    bool isTriggerPressed = Player.leftController != null && Player.leftController.GetIndexTouchpad();
                    
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

            TrackHandReleaseSpeed();
        }

        public override void
