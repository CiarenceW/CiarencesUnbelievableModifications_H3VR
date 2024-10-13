using CiarencesUnbelievableModifications.MonoBehaviours;
using CiarencesUnbelievableModifications.Libraries;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    public static class ReverseMagHoldPos
    {
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Awake))]
        [HarmonyPostfix]
        private static void AddMagazinePostExtenderComponent(FVRFireArmMagazine __instance)
        {
            if (__instance.ObjectWrapper != null && __instance.ObjectWrapper.ItemID != null && !IsMelonAmmoBoxTeehee(__instance))
            {
                __instance.GetOrAddComponent<FVRMagazinePoseExtender>();
            }
        }

        public static bool IsMelonAmmoBoxTeehee(FVRFireArmMagazine magazine)
        {
            return magazine.ObjectWrapper.ItemID.Contains(".MELON");
        }

        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.UpdateInteraction))]
        [HarmonyPostfix]
        private static void PatchUpdateInteracton(FVRFireArmMagazine __instance, ref FVRViveHand hand)
        {
            if (!SettingsManager.configEnableReverseMagHold.Value) return;

            if(!__instance.TryGetComponent<FVRMagazinePoseExtender>(out var magPoseExtender)) return;

            if (!hand.IsInStreamlinedMode && hand.Input.TouchpadDown)
            {
                if (Vector2.Angle(hand.Input.TouchpadAxes, Vector2.left) < 45f || Vector2.Angle(hand.Input.TouchpadAxes, Vector2.right) < 45f)
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }

            if (hand.IsInStreamlinedMode && hand.Input.TriggerDown)
            {
                magPoseExtender.SwitchMagazinePose();
            }

            //reverse pose offset adjustment
            if (!hand.IsInStreamlinedMode && hand.Input.TriggerPressed && magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed)
            {
                magPoseExtender.distance_override += Vector2.Dot(hand.Input.TouchpadAxes, Vector2.down) * 0.01f;
                SettingsManager.BindMagazineOffset(__instance).Value = magPoseExtender.distance_override; //persistent data stuff
                magPoseExtender.OffsetReverseHoldingPose();
            }

            /*if (magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed && hand.CurrentHoveredQuickbeltSlot != null && hand.CurrentHoveredQuickbeltSlot.CurObject == null)
            {
                magPoseExtender.magazine.PoseOverride.localPosition = magPoseExtender.basePoseOverride.localPosition;
            }*/
        }

        //[HarmonyPatch(typeof(FVRPhysicalObject), nameof(FVRPhysicalObject.SetQuickBeltSlot))] not sure about what this does
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.EndInteraction))]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Load), new[] { typeof(AttachableFirearm) } )]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.Load), new[] { typeof(FVRFireArm) } )]
        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.LoadIntoSecondary))]
        [HarmonyPostfix]
        private static void PatchLoadMagazine(FVRFireArmMagazine __instance)
        {
            if (__instance.TryGetComponent<FVRMagazinePoseExtender>(out var magPoseExtender))
            {
                if (magPoseExtender.currentMagazinePose == FVRMagazinePoseExtender.CurrentMagazinePose.Reversed)
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }
        }

        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.BeginInteraction))]
        [HarmonyPostfix]
        private static void PatchMagazineBeginInteraction(FVRFireArmMagazine __instance, FVRViveHand hand)
        {
            if (__instance.TryGetComponent<FVRMagazinePoseExtender>(out var magPoseExtender))
            {
                if (!SettingsManager.configEnableReverseMagHold.Value) return;

                if (Vector3.Dot(hand.GetMagPose().up, __instance.QBPoseOverride.transform.up) > SettingsManager.configReverseMagGrabMinDotProduct.Value
                    && !__instance.m_isSpawnLock 
                    && (!SettingsManager.configReverseMagHoldHandgunOnly.Value || //if other hand's current interactible is a handgun and HandgunOnly mode is on and the current magazine is compatible with the gun
                        (__instance.m_hand.OtherHand.CurrentInteractable == null || __instance.m_hand.OtherHand.CurrentInteractable is Handgun gun &&
                        SettingsManager.configReverseMagHoldHandgunOnly.Value && 
                        __instance.MagazineType == gun.MagazineType)))
                {
                    magPoseExtender.SwitchMagazinePose();
                }
            }
        }

        [HarmonyPatch(typeof(FVRFireArmMagazine), nameof(FVRFireArmMagazine.FVRFixedUpdate))]
        [HarmonyPostfix]
        private static void UpdateFVRPoseExtender(FVRFireArmMagazine __instance)
        {
            if(__instance.TryGetComponent<FVRMagazinePoseExtender>(out var magPoseExtender) && SettingsManager.configEnableReverseMagHold.Value)
            {
                magPoseExtender.FU(); //dis shit is weird
            }
        }
    }
}
