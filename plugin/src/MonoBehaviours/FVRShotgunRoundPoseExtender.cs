using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.MonoBehaviours
{
    public class FVRShotgunRoundPoseExtender : MonoBehaviour
    {
        public FVRFireArmRound shotgunShell;

        private Transform basePoseOverride;
        private Transform competitivePoseOverride;

        private Transform baseQBPoseOverride;
        private Transform competitiveQBPoseOverride;

        private void Awake()
        {
            shotgunShell = GetComponent<FVRFireArmRound>();

            SetQBOverrideTransforms();
        }

        public void SwitchTransform(bool forceOff = false)
        {
            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value)
            {
                shotgunShell.PoseOverride = competitivePoseOverride;
                shotgunShell.QBPoseOverride = competitiveQBPoseOverride;
            }
            else
            {
                shotgunShell.PoseOverride = basePoseOverride;
                shotgunShell.QBPoseOverride = baseQBPoseOverride;
            }

            //fuck
            if (forceOff)
            {
                shotgunShell.PoseOverride = basePoseOverride;
                shotgunShell.QBPoseOverride = baseQBPoseOverride;
            }
        }

        public void SetQBOverrideTransforms()
        {
            if (baseQBPoseOverride == null)
            {
                baseQBPoseOverride = new GameObject("baseQBPoseOverride").transform;
                baseQBPoseOverride.localPosition = shotgunShell.QBPoseOverride.localPosition;
                baseQBPoseOverride.localRotation = shotgunShell.QBPoseOverride.localRotation;
            }
            if (competitiveQBPoseOverride == null)
            {
                competitiveQBPoseOverride = new GameObject("competitiveQBPoseOverride").transform;
                competitiveQBPoseOverride.parent = shotgunShell.QBPoseOverride.parent;
                competitiveQBPoseOverride.localPosition = baseQBPoseOverride.localPosition + new Vector3(0, 0, GetComponent<CapsuleCollider>().radius * 1.75f);
                competitiveQBPoseOverride.localEulerAngles = new Vector3(90, 0, 0);
            }

            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value)
            {
                shotgunShell.QBPoseOverride = competitiveQBPoseOverride;
            }
        }

        public void SetOverrideTransforms()
        {
            if (shotgunShell == null) return;

            if (basePoseOverride == null)
            {
                var obj = new GameObject("basePoseOverride");
                if (obj != null)
                {
                    basePoseOverride = obj.transform;
                    basePoseOverride.parent = shotgunShell.transform;
                }
                if (shotgunShell.PoseOverride != null)
                {
                    basePoseOverride.localPosition = shotgunShell.PoseOverride.localPosition;
                    basePoseOverride.localRotation = shotgunShell.PoseOverride.localRotation;
                }
            }

            if (competitivePoseOverride == null)
            {
                var obj = new GameObject("competitivePoseOverride");
                if (obj != null) competitivePoseOverride = obj.transform;
                if (competitivePoseOverride != null) competitivePoseOverride.parent = basePoseOverride.parent; //don't ask
            }
            
            if (competitivePoseOverride != null) //end my life
            {
                competitivePoseOverride.localPosition = new Vector3(0, -0.025f, -0.1f);
                competitivePoseOverride.localEulerAngles = new Vector3(0, 180, 90);
                if (shotgunShell.m_hand != null)
                {
                    if (shotgunShell.m_hand.IsThisTheRightHand) //southpawoids
                    {
                        competitivePoseOverride.localEulerAngles = new Vector3(0, 180, -90);
                    }
                }
            }


            var hasRightAmount = (shotgunShell.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value;

            var noOneCaresAboutAmount = (shotgunShell.ProxyRounds.Count + 1 > SettingsManager.configMaxShellsInHand.Value && !SettingsManager.configRevertToNormalGrabbingWhenAboveX.Value);

            var hasLeverAction = (shotgunShell.m_hand != null && shotgunShell.m_hand.OtherHand.CurrentInteractable != null && shotgunShell.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm);

            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value && (hasRightAmount || noOneCaresAboutAmount) && shotgunShell.m_hand != null && shotgunShell.m_hand.OtherHand.CurrentInteractable != null && shotgunShell.m_hand.OtherHand.CurrentInteractable is FVRFireArm gun && gun.Magazine != null && gun.Magazine.IsIntegrated && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value)) && competitivePoseOverride != null)
            {
                shotgunShell.PoseOverride = competitivePoseOverride;
            }
            else
            {
                shotgunShell.PoseOverride = basePoseOverride;
            }
        }
    }
}
