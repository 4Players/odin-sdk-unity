using UnityEngine;
using System.Linq;
using System;

namespace OdinNative.Unity.CIEditor
{
    public class OdinEditorExec
    {
        public static void CmpHeader()
        {
            if (!Application.isBatchMode) return;

            string dotH = "e55589e5cceab4bd150c66c4ac0ae1d6";
            /*
            * EXIT_CODE -eq 0 "Run succeeded, no failures occurred";
            * EXIT_CODE -eq 1 "Run failure, script compile error (compilationhadfailure)";
            * EXIT_CODE -eq 2 "Run succeeded, some tests failed";
            * EXIT_CODE -eq 3 "Run failure (other failure)";
            */

#if UNITY_EDITOR
            Action<int> ret = UnityEditor.EditorApplication.Exit;
#else
            Action<int> ret = Application.Quit;
#endif

            string[] args = System.Environment.GetCommandLineArgs();
            string arg = args.LastOrDefault();
            if (arg.Equals(dotH))
                ret(0);
            else if (args.Contains(dotH))
            {
                Debug.LogWarning($"{nameof(OdinEditorExec)} only in args.");
                ret(0);
            }

            Debug.LogError($"{nameof(OdinEditorExec)} failed, {dotH} with {arg}");
            ret(2);
        }
    }
}
