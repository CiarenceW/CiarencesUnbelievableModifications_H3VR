using UnityEngine;

namespace CiarencesUnbelievableModifications
{
    internal static class ExtensionsToMakeMyLifeLessShit
    {
        public static bool TryGetComponent<T>(this Component component, out T result) where T : Component
        {
            return (result = component.GetComponent<T>()) != null; //wow!!!! all that in one line!!! cool right?
        }

        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        {
            return (result = component.GetComponentInParent<T>()) != null;
        }

        public static bool TryGetComponentInChildren<T>(this Component component, out T result) where T: Component
        {
            return (result = component.GetComponentInChildren<T>()) != null;
        }
    }
}
