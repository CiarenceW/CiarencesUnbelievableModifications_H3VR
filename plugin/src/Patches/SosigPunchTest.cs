//not worth it, would have to do some fuckery by adding a new FVRPhysicalObject that is always attached to the controller, can't be fucked really, no other way to do damage to sosigs, sorry :)
using CiarencesUnbelievableModifications.Libraries;
using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class SosigPunchTest
	{
		private static AccessTools.FieldRef<FVRInteractiveObject, Collider[]> m_collidersFieldRef = AccessTools.FieldRefAccess<FVRInteractiveObject, Collider[]>("m_colliders");

		private static Collider[] puncherColliders = new Collider[2];

		[HarmonyPatch(typeof(FVRViveHand), nameof(FVRViveHand.Awake))]
		[HarmonyPostfix]
		private static void AddPunchCollidersToHands()
		{
			for (int handIndex = 0; handIndex < GM.CurrentMovementManager.Hands.Length; handIndex++)
			{
				var hand = GM.CurrentMovementManager.Hands[handIndex];
				GameObject puncherGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				puncherGO.GetComponent<MeshRenderer>().enabled = false;
				puncherGO.name = "Puncher";
				puncherGO.transform.parent = hand.transform;
				puncherGO.transform.localPosition = Vector3.zero;
				puncherGO.transform.localScale = Vector3.one * 0.2f;
				puncherGO.layer = 20; //AgentBody layer
				var sphereCollider = puncherGO.GetComponent<SphereCollider>();
				puncherColliders[handIndex] = sphereCollider;

				foreach (var interactable in FVRInteractiveObject.All)
				{
					foreach (var collider in m_collidersFieldRef.Invoke(interactable))
					{
						Physics.IgnoreCollision(sphereCollider, collider, true);
					}
				}
			}
		}

		[HarmonyPatch(typeof(FVRInteractiveObject), nameof(FVRInteractiveObject.Awake))]
		[HarmonyPostfix]
		private static void IgnorePunchers(FVRInteractiveObject __instance)
		{
			foreach (var collider in m_collidersFieldRef.Invoke(__instance))
			{
				Physics.IgnoreCollision(puncherColliders[0], collider, true);
				Physics.IgnoreCollision(puncherColliders[1], collider, true);
			}
		}

		public class SosigPuncher : MonoBehaviour
		{
			private void OnCollisionEnter(Collision collision)
			{
				IFVRDamageable damageable = null;
				if (collision.gameObject.TryGetComponent<SosigLink>(out var sosigLink))
				{
					damageable = sosigLink;
				}
				else if (collision.gameObject.TryGetComponent<SosigWearable>(out var wearable))
				{
					damageable = wearable;
				}
				else if (collision.gameObject.TryGetComponent<SosigWearblePasser>(out var wearblePasser))
				{
					damageable = wearblePasser;
				}

				if (damageable != null)
				{
					Damage damage = new Damage()
					{
						Class = Damage.DamageClass.Melee,
						Dam_Blunt = 2,
						Source_IFF = GM.CurrentPlayerBody.GetPlayerIFF(),
						edgeNormal = transform.forward,
						damageSize = 0.02f,
						point = collision.contacts[0].point,
						hitNormal = collision.contacts[0].normal,
						strikeDir = transform.forward
					};

					damageable.Damage(damage);
				}
			}
		}
	}
}