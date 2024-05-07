using CiarencesUnbelievableModifications.Patches;
using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.MonoBehaviours
{
    //TODO: try using the IsPivotLocked property instead of making new transforms (more stable?) (less shit?) (fuck?)
    public class FVRShotgunRoundPoseExtender : MonoBehaviour
    {
        public FVRFireArmRound shotgunShell;

        public bool shouldPez;
        public bool hasAncestor;

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
            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value && competitivePoseOverride != null && competitiveQBPoseOverride != null)
            {
                shotgunShell.PoseOverride = competitivePoseOverride;
                shotgunShell.QBPoseOverride = competitiveQBPoseOverride;
            }
            else
            {
                if (basePoseOverride == null) return;
                shotgunShell.PoseOverride = basePoseOverride;
            }

            //fuck
            if (forceOff && !SettingsManager.configForceUnconditionalCompetitiveShellGrabbing.Value)
            {
                shotgunShell.PoseOverride = basePoseOverride;
            }
        }

        public void SetQBOverrideTransforms()
        {
            if (baseQBPoseOverride == null)
            {
                baseQBPoseOverride = new GameObject("baseQBPoseOverride").transform;
                baseQBPoseOverride.parent = shotgunShell.QBPoseOverride.parent;
                baseQBPoseOverride.localPosition = shotgunShell.QBPoseOverride.localPosition;
                baseQBPoseOverride.localRotation = shotgunShell.QBPoseOverride.localRotation;
            }
            if (competitiveQBPoseOverride == null)
            {
                competitiveQBPoseOverride = new GameObject("competitiveQBPoseOverride").transform;
                competitiveQBPoseOverride.parent = shotgunShell.QBPoseOverride.parent;
				competitiveQBPoseOverride.localPosition = baseQBPoseOverride.localPosition + new Vector3(0, (GetComponent<CapsuleCollider>().radius * 1.75f) * ((shotgunShell.ProxyRounds.Count + 1) / 2), 0);
                competitiveQBPoseOverride.localEulerAngles = new Vector3(-90, 0, 0);
            }
			else
			{
				competitiveQBPoseOverride.localPosition = baseQBPoseOverride.localPosition + new Vector3(0, (GetComponent<CapsuleCollider>().radius * 1.75f) * ((shotgunShell.ProxyRounds.Count + 1) / 2), 0);
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
                if (shotgunShell.PoseOverride != null)
                {
                    var obj = new GameObject("basePoseOverride");
                    basePoseOverride = obj.transform;
                    obj.transform.parent = shotgunShell.transform;
                    basePoseOverride.localPosition = shotgunShell.PoseOverride.localPosition;
                    SettingsManager.LogVerboseInfo("BPOLP: " + basePoseOverride.localPosition);
                    basePoseOverride.localRotation = shotgunShell.PoseOverride.localRotation;
                    SettingsManager.LogVerboseInfo("BPOLR: " + basePoseOverride.localRotation);
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
                competitivePoseOverride.localPosition = SettingsManager.configCompetitiveShellPoseOverridePosition.Value;
                SettingsManager.LogVerboseInfo("CPOLP: " + competitivePoseOverride.localPosition);
                competitivePoseOverride.localEulerAngles = SettingsManager.configCompetitiveShellPoseOverrideRotation.Value;
                if (shotgunShell.m_hand != null)
                {
                    if (shotgunShell.m_hand.IsThisTheRightHand) //southpawoids
                    {
                        var southpaidsw = SettingsManager.configCompetitiveShellPoseOverrideRotation.Value;
                        southpaidsw.z = southpaidsw.z * -1;
                        competitivePoseOverride.localEulerAngles = southpaidsw;
                    }
                }
                SettingsManager.LogVerboseInfo("CPOLR: " + competitivePoseOverride.localRotation);
            }


            var hasRightAmount = ((shotgunShell.ProxyRounds.Count + 1) <= SettingsManager.configMaxShellsInHand.Value);

            var noOneCaresAboutAmount = (shotgunShell.ProxyRounds.Count + 1 > SettingsManager.configMaxShellsInHand.Value && !SettingsManager.configRevertToNormalGrabbingWhenAboveX.Value);

            if (!hasRightAmount && !noOneCaresAboutAmount || (!hasAncestor && shotgunShell.ProxyRounds.Count == 0 && SettingsManager.configPezOnGrabOneShell.Value && shotgunShell.m_hand != null && CompetitiveShellGrabbing.IsSingleShellGrabAction(shotgunShell.m_hand)))
            {
                shouldPez = true;
            }

            SettingsManager.LogVerboseInfo(shouldPez);

            var hasLeverAction = (shotgunShell.m_hand != null && shotgunShell.m_hand.OtherHand.CurrentInteractable != null && shotgunShell.m_hand.OtherHand.CurrentInteractable is LeverActionFirearm) || (shotgunShell.m_hand != null && shotgunShell.m_hand.OtherHand.CurrentInteractable != null && shotgunShell.m_hand.OtherHand.CurrentInteractable is Revolver);

            if (SettingsManager.configEnableCompetitiveShellGrabbing.Value && (hasRightAmount || noOneCaresAboutAmount) && shotgunShell.m_hand != null && shotgunShell.m_hand.OtherHand.CurrentInteractable != null && shotgunShell.m_hand.OtherHand.CurrentInteractable is FVRFireArm gun && gun.Magazine != null && gun.Magazine.IsIntegrated && (!hasLeverAction || (hasLeverAction && !SettingsManager.configNoLeverAction.Value)) && competitivePoseOverride != null)
            {
                SwitchTransform(shouldPez);
            }
            else
            {
                SwitchTransform(true);
            }
        }
    }
}
