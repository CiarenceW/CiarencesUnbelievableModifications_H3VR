using FistVR;
using UnityEngine;

namespace CiarencesUnbelievableModifications.MonoBehaviours
{
    public class FVRMagazinePoseExtender : MonoBehaviour
    {
        public enum CurrentMagazinePose
        {
            Base,
            Reversed
        }

        public Vector3 relativeUp = Vector3.zero;
        public Vector3 relativeForward = Vector3.zero;

        public static float distance_down = SettingsManager.configReverseMagHoldPositionDistance.Value;

        public float distance_override = 0f;

        public Transform basePoseOverride;
        public Transform reversePoseOverride;

        public Transform baseQBPoseOverride;
        public Transform reverseQBPoseOverride;

        public CurrentMagazinePose currentMagazinePose = CurrentMagazinePose.Base;

        private float lerpProgress = 1f;

        public FVRFireArmMagazine magazine;

        private void Awake()
        {
            magazine = GetComponent<FVRFireArmMagazine>();

            if (magazine.ObjectWrapper == null || magazine.IsIntegrated) Destroy(this); //I think it's for shotguns

            distance_override = SettingsManager.BindMagazineOffset(magazine).Value; //assign (if it exists) persistent data

            CreateTransforms();
        }

        public void CreateTransforms()
        {
            if (magazine == null) magazine = GetComponent<FVRFireArmMagazine>();

            if (GM.CurrentMovementManager.Hands[0] == null) return; //what why would this happen, huh? what? what the fuck dude, this is fucked!!!!!!!!! I mean it hasn't but I'm getting mad thinking about the prospect of it happening

            var CMode = GM.CurrentMovementManager.Hands[0].CMode;

            //too lazy to make it better
            if ((CMode == ControlMode.Oculus || CMode == ControlMode.Index) && magazine.PoseOverride_Touch != null)
            {
                if (!transform.Find("basePoseOverride"))
                {
                    basePoseOverride = new GameObject("basePoseOverride").transform;
                    basePoseOverride.SetParent(magazine.PoseOverride_Touch.parent);

                    basePoseOverride.localPosition = magazine.PoseOverride_Touch.localPosition;
                    basePoseOverride.localRotation = magazine.PoseOverride_Touch.localRotation;
                }

                if (!transform.Find("reversePoseOverride"))
                {
                    reversePoseOverride = new GameObject("reversePoseOverride").transform;
                    reversePoseOverride.SetParent(magazine.PoseOverride_Touch.parent);

                    OffsetReverseHoldingPose();

                    reversePoseOverride.localRotation = Quaternion.Inverse(magazine.PoseOverride_Touch.localRotation);
                    var reversePoseLocalRot = reversePoseOverride.localEulerAngles;
                    reversePoseLocalRot.y -= 180f;
                    SettingsManager.LogVerboseInfo($"{reversePoseOverride.localEulerAngles} -> {reversePoseLocalRot}");
                    reversePoseOverride.localEulerAngles = reversePoseLocalRot;
                }

                if (!transform.Find("baseQBPoseOverride"))
                {
                    baseQBPoseOverride = new GameObject("baseQBPoseOverride").transform;

                    baseQBPoseOverride.SetParent(magazine.QBPoseOverride.parent);

                    baseQBPoseOverride.localPosition = magazine.QBPoseOverride.localPosition;
                    baseQBPoseOverride.localRotation = magazine.QBPoseOverride.localRotation;
                }

                if (!transform.Find("reverseQBPoseOverride"))
                {
                    reverseQBPoseOverride = new GameObject("reverseQBPoseOverride").transform;

                    reverseQBPoseOverride.SetParent(magazine.QBPoseOverride.parent);

                    reverseQBPoseOverride.localPosition = magazine.QBPoseOverride.localPosition;

                    reverseQBPoseOverride.localRotation = Quaternion.Inverse(magazine.QBPoseOverride.localRotation);
                    var reverseQBPoseLocalRot = reverseQBPoseOverride.localEulerAngles;
                    reverseQBPoseLocalRot.z -= 180f;
                    SettingsManager.LogVerboseInfo($"{reverseQBPoseOverride.localEulerAngles} -> {reverseQBPoseLocalRot}");
                    reverseQBPoseOverride.localEulerAngles = reverseQBPoseLocalRot;
                }
            }
            else
            {
                if (!transform.Find("basePoseOverride"))
                {
                    basePoseOverride = new GameObject("basePoseOverride").transform;

                    basePoseOverride.SetParent(magazine.PoseOverride.parent);

                    basePoseOverride.localPosition = magazine.PoseOverride.localPosition;
                    basePoseOverride.localRotation = magazine.PoseOverride.localRotation;
                }

                if (!transform.Find("reversePoseOverride"))
                {
                    reversePoseOverride = new GameObject("reversePoseOverride").transform;

                    reversePoseOverride.SetParent(magazine.PoseOverride.parent);

                    OffsetReverseHoldingPose();

                    reversePoseOverride.localRotation = Quaternion.Inverse(magazine.PoseOverride.localRotation);
                    var reversePoseLocalRot = reversePoseOverride.localEulerAngles;
                    reversePoseLocalRot.y -= 180f;
                    SettingsManager.LogVerboseInfo($"{reversePoseOverride.localEulerAngles} -> {reversePoseLocalRot}");
                    reversePoseOverride.localEulerAngles = reversePoseLocalRot;
                }

                if (!transform.Find("baseQBPoseOverride"))
                {
                    baseQBPoseOverride = new GameObject("baseQBPoseOverride").transform;

                    baseQBPoseOverride.SetParent(magazine.QBPoseOverride.parent);

                    baseQBPoseOverride.localPosition = magazine.QBPoseOverride.localPosition;
                    baseQBPoseOverride.localRotation = magazine.QBPoseOverride.localRotation;
                }

                if (!transform.Find("reverseQBPoseOverride"))
                {
                    reverseQBPoseOverride = new GameObject("reverseQBPoseOverride").transform;

                    reverseQBPoseOverride.SetParent(magazine.QBPoseOverride.parent);

                    reverseQBPoseOverride.localPosition = magazine.QBPoseOverride.localPosition;

                    reverseQBPoseOverride.localRotation = Quaternion.Inverse(magazine.QBPoseOverride.localRotation);
                    var reverseQBPoseLocalRot = reverseQBPoseOverride.localEulerAngles;
                    reverseQBPoseLocalRot.z -= 180f;
                    SettingsManager.LogVerboseInfo($"{reverseQBPoseOverride.localEulerAngles} -> {reverseQBPoseLocalRot}");
                    reverseQBPoseOverride.localEulerAngles = reverseQBPoseLocalRot;
                }
            }
        }

        public void OffsetReverseHoldingPose()
        {
            reversePoseOverride.localPosition = basePoseOverride.localPosition;
            var reversePoseLocalPos = reversePoseOverride.localPosition;
            reversePoseLocalPos.y -= distance_down + distance_override;
            reversePoseOverride.localPosition = reversePoseLocalPos;
        }

        public void SwitchMagazinePose()
        {
            lerpProgress = 0f;

            if (currentMagazinePose == CurrentMagazinePose.Base)
            {
                currentMagazinePose = CurrentMagazinePose.Reversed;
            }
            else
            {
                currentMagazinePose = CurrentMagazinePose.Base;
            }
        }

        public void FU()
        {
            lerpProgress += Time.deltaTime * 6f;
            if (basePoseOverride == null || reversePoseOverride == null || baseQBPoseOverride == null || reverseQBPoseOverride == null) return;
            if (currentMagazinePose == CurrentMagazinePose.Base)
            {
                magazine.PoseOverride.localPosition = Vector3.Lerp(reversePoseOverride.localPosition, basePoseOverride.localPosition, lerpProgress);
                magazine.PoseOverride.localRotation = Quaternion.Lerp(reversePoseOverride.localRotation, basePoseOverride.localRotation, lerpProgress);

                magazine.QBPoseOverride.localPosition = Vector3.Lerp(reverseQBPoseOverride.localPosition, baseQBPoseOverride.localPosition, lerpProgress);
                magazine.QBPoseOverride.localRotation = Quaternion.Lerp(reverseQBPoseOverride.localRotation, baseQBPoseOverride.localRotation, lerpProgress);
            }
            else
            {
                magazine.PoseOverride.localPosition = Vector3.Lerp(basePoseOverride.localPosition, reversePoseOverride.localPosition, lerpProgress);
                magazine.PoseOverride.localRotation = Quaternion.Lerp(basePoseOverride.localRotation, reversePoseOverride.localRotation, lerpProgress);

                magazine.QBPoseOverride.localPosition = Vector3.Lerp(baseQBPoseOverride.localPosition, reverseQBPoseOverride.localPosition, lerpProgress);
                magazine.QBPoseOverride.localRotation = Quaternion.Lerp(baseQBPoseOverride.localRotation, reverseQBPoseOverride.localRotation, lerpProgress);
            }
        }

        //this is for the KeepPalmedMagRot transpiler, while we were experimenting with stuff, but now it's easier for the config
        public Quaternion GetRotation()
        {
            if (SettingsManager.configEnableMagPalmKeepOffset.Value)
            {
                return Quaternion.LookRotation(magazine.m_magParent.transform.TransformDirection(relativeForward), magazine.m_magParent.transform.TransformDirection(relativeUp)); //Szikaka I love you
            }
            else
            {
                return magazine.m_magParent.transform.rotation;
            }
        }   
    }
}
