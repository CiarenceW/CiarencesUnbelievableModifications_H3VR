using FistVR;
using System.Collections;
using UnityEngine;

namespace CiarencesUnbelievableModifications.MonoBehaviours
{
    public class FVRTimedObjectDestructor : MonoBehaviour
    {
        Coroutine destroyCoroutine;

        private float timeBeforeDestroyed = 50f;

        private FVRFireArmMagazine magazine;

        private void Awake()
        {
            //unfinished
            return;

            if (magazine.m_hand != null) return;

            if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled) return;

            destroyCoroutine = StartCoroutine(DestroyCountdown());
        }

        private void OnPickup()
        {
            StopCoroutine(destroyCoroutine);
        }

        private void OnDrop()
        {
            StopCoroutine(destroyCoroutine);

            destroyCoroutine = StartCoroutine(DestroyCountdown());
        }

        private void OnDestroy()
        {
            StopCoroutine(destroyCoroutine);
        }

        private IEnumerator DestroyCountdown()
        {
            yield return new WaitForSeconds(timeBeforeDestroyed);

            if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled)
            {
                yield break;
            }

            Destroy(this);

            yield break;
        }
    }
}
