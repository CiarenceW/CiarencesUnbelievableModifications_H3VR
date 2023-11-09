using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace CiarencesUnbelievableModifications
{
    public class TranspilerHelper
    {
        public static bool TryMatchForward(bool useEnd, IEnumerable<CodeInstruction> instructions, ILGenerator generator, out CodeMatcher codeMatcher, MethodBase __originalMethod, Action<string> logger = null, params CodeMatch[] codeMatches)
        {
            codeMatcher = new CodeMatcher(instructions, generator).MatchForward(useEnd, codeMatches);

            if (logger == null) logger = Debug.LogError;
            return (!codeMatcher.ReportFailure(__originalMethod, logger));
        }
    }
}
