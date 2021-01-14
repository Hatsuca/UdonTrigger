using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UdonTrigger
{
    [CustomEditor(typeof(Udon_Trigger))]
    public class Udon_TriggerEditor : UnityEditor.Editor
    {
        Udon_Trigger udon_Trigger;

        private void OnEnable()
        {
            udon_Trigger = (Udon_Trigger)target;
            Debug.Log("OnEnable");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Compile"))
            {
                udon_Trigger.CompileProgram();
            }
        }
    }
}