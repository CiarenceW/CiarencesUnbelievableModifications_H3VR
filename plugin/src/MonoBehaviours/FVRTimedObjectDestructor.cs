using FistVR;
using System.Collections;
using UnityEngine;

namespace CiarencesUnbelievableModifications.MonoBehaviours
{
    public class FVRTimedObjectDestructor : MonoBehaviour
    {
        private Coroutine destroyCoroutine;

        private void Awake()
        {
            if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled) return;

            destroyCoroutine = StartCoroutine(DestroyCountdown());
        }

        public void OnPickup()
        {
			if (destroyCoroutine != null)
			{
				StopCoroutine(destroyCoroutine);
			}
        }

        public void OnDrop()
		{
			if (destroyCoroutine != null)
			{
				StopCoroutine(destroyCoroutine);
			}

			destroyCoroutine = StartCoroutine(DestroyCountdown());
        }

        public void OnDestroy()
		{
			if (destroyCoroutine != null)
			{
				StopCoroutine(destroyCoroutine);
			}
		}

        private IEnumerator DestroyCountdown()
        {
            yield return new WaitForSeconds(SettingsManager.configTODTimeToDestroy.Value);

            if (!GM.CurrentSceneSettings.IsSpawnLockingEnabled)
            {
                yield break;
            }

            Destroy(this.gameObject);

            yield break;
        }
    }
}
