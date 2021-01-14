using System.Collections.Generic;
using UnityEngine;
using VRC.Udon;

namespace UdonTrigger
{
    [RequireComponent(typeof(UdonBehaviour))]
    public class Udon_Trigger : MonoBehaviour
    {
        

        public UdonBehaviour udonBehaviour;
        public UdonTriggerProgramAsset triggerProgramAsset;
        public List<TriggerParameters.Triggers> triggers = new List<TriggerParameters.Triggers>();

        public string path = "Assets/UdonTriggerProgramAsset.asset";

        public void CompileProgram()
        {
            //UdonBehaviour.programSourceにProgramAssetを登録
            if (udonBehaviour == null) udonBehaviour = GetComponent<UdonBehaviour>();
            if (udonBehaviour.programSource == null)
            {
                //ScriptableAsset生成
                var obj = ScriptableObject.CreateInstance<UdonTriggerProgramAsset>();
                UnityEditor.AssetDatabase.CreateAsset(obj, path);

                triggerProgramAsset = (UdonTriggerProgramAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(UdonTriggerProgramAsset));

                //ProgramAsset登録
                var udonBehaviourSO = new UnityEditor.SerializedObject(udonBehaviour);
                udonBehaviourSO.Update();
                udonBehaviourSO.FindProperty("programSource").objectReferenceValue = triggerProgramAsset;
                udonBehaviourSO.ApplyModifiedProperties();
                Debug.Log("Setup Completed.");
            }

            triggerProgramAsset.RefreshProgram();
        }
    }
}