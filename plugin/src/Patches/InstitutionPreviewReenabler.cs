﻿using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications.Patches
{
    internal static class InstitutionPreviewReenabler
    {
        [HarmonyPatch(typeof(MainMenuScreen), nameof(MainMenuScreen.Awake))]
        [HarmonyPostfix]
        private static void ReenableInstitutionPreviewScreen()
        {
            var panel = GameObject.Find("MainMenuSceneProtoBase/SceneScreens/SceneScreen_Screen14 (7)");
            panel.SetActive(true);
            if (panel != null)
            {
                panel.transform.position = new Vector3(11.66f, -0.79f, 4.4f);
                panel.transform.eulerAngles = new Vector3(0, 59.8033f, 0);
            }
        }
    }
}
