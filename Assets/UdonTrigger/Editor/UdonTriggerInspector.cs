using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using VRC.Udon;
using VRC.Udon.Editor.ProgramSources;

namespace UdonTrigger
{
    public class UdonTriggerInspector : EditorWindow
    {
        public GameObject gameObjects;

        private static UdonTriggerInspector _window;

        private UdonBehaviour _udonBehaviour;
        private UdonTriggerProgramAsset _triggerProgramAsset;
        private GameObject _selectGameObject;

        [MenuItem("Window/UdonTrigger/UdonTriggerInspector")]
        static void Open()
        {
            _window = GetWindow<UdonTriggerInspector>("UdonTriggerInspector");
        }

        private void OnEnable()
        {
            _window = GetWindow<UdonTriggerInspector>("UdonTriggerInspector");
            Selection.selectionChanged += () => _window.OnSelectionChanged();
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= () => _window.OnSelectionChanged();
        }

        private void OnGUI()
        {
            if (_selectGameObject == null)
                return;

            bool isDisabled = false;

            EditorGUILayout.LabelField(_selectGameObject?.name);
            if (_udonBehaviour != null)
                EditorGUILayout.LabelField(_udonBehaviour?.gameObject.name);
            
            if (_udonBehaviour?.programSource == null)
            {
                CreateUdonTriggerGUI();
                isDisabled = true;
            }
            else
            {
                AbstractUdonProgramSource programSource = _udonBehaviour.programSource;
                if (programSource != null)
                {
                    if (typeof(UdonTriggerProgramAsset) == programSource.GetType())
                    {
                        _triggerProgramAsset = (UdonTriggerProgramAsset)programSource;

                        UdonTriggerInspectorGUI();
                    }
                    else
                    {
                        isDisabled = true;
                    }
                }
                else
                {
                    isDisabled = true;
                }
            }

            
            EditorGUI.BeginDisabledGroup(isDisabled);
                if (GUILayout.Button("Compile"))
                {
                    _triggerProgramAsset.RefreshProgram();
                }
            EditorGUI.EndDisabledGroup();
        }

        private void UdonTriggerInspectorGUI()
        {
            bool isChanged = false;

            EditorGUILayout.LabelField("Triggers");
            EditorGUI.indentLevel++;
            for (int i=0; i < _triggerProgramAsset.triggers.Count; i++)
            {
                EditorGUILayout.LabelField($"Name: {_triggerProgramAsset.triggers[i].name}");
                EditorGUILayout.LabelField($"Type: {_triggerProgramAsset.triggers[i].triggerType.ToString()}");
                EditorGUILayout.LabelField("Events");
                EditorGUI.indentLevel++;
                for(int l=0; l < _triggerProgramAsset.triggers[i].events.Count; l++)
                {
                    EditorGUILayout.LabelField($"Type: {_triggerProgramAsset.triggers[i].events[l].eventType.ToString()}");
                    EditorGUILayout.LabelField($"String: {_triggerProgramAsset.triggers[i].events[l].parameterString}");
                    EditorGUILayout.LabelField($"BoolOp: {_triggerProgramAsset.triggers[i].events[l].parameterBoolOp.ToString()}");
                    EditorGUILayout.LabelField($"Float: {_triggerProgramAsset.triggers[i].events[l].parameterFloat.ToString()}");
                    EditorGUILayout.LabelField($"Int: {_triggerProgramAsset.triggers[i].events[l].parameterInt.ToString()}");
                    EditorGUILayout.LabelField("Objects");
                    EditorGUI.indentLevel++;

                    _triggerProgramAsset.triggers[i].events[l].parameterObjects[0] = 
                        EditorGUILayout.ObjectField(_triggerProgramAsset.triggers[i].events[l].parameterObjects[0], typeof(GameObject), true);
                    /*
                    foreach (UnityEngine.Object _object in _triggerProgramAsset.triggers[i].events[l].parameterObjects)
                    {
                        EditorGUILayout.LabelField($"Object: {_object.ToString()}");
                    }
                    */
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;

                //イベント追加
                if (GUILayout.Button("AddEvent"))
                {
                    _triggerProgramAsset.triggers[i].events.Add(new TriggerParameters.Events(TriggerParameters.EventType.SetGameObjectActive, "", TriggerParameters.BoolOp.True, 0, 0, new GameObject[1]));
                    isChanged = true;
                }
            }
            EditorGUI.indentLevel--;

            //トリガー追加
            if (GUILayout.Button("AddTrigger"))
            {
                _triggerProgramAsset.triggers.Add(new TriggerParameters.Triggers(TriggerParameters.TriggerType.OnInteract));
                isChanged = true;
            }
            
            if (isChanged)
            {
                EditorUtility.SetDirty(_triggerProgramAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Repaint();
            }
        }

        private void CreateUdonTriggerGUI()
        {
            EditorGUILayout.LabelField("Udon Trigger Not Found.");
            if (GUILayout.Button("Create Udon Trigger Program Asset"))
            {
                CreateUdonTriggerProgramAsset();
            }
        }

        //ProgramAssetを生成してUdonBehaviourにアタッチ
        private void CreateUdonTriggerProgramAsset()
        {
            //ファイル保存先指定
            var scene = SceneManager.GetActiveScene();
            string[] sceneNameSplit = { scene.name };
            var pathArray = scene.path.Split(sceneNameSplit, StringSplitOptions.None);
            var filePath = EditorUtility.SaveFilePanelInProject(
                                            "Save Udon Trigger Program Asset",
                                            $"{_selectGameObject.name} Udon Trigger Program Asset",
                                            "asset",
                                            "",
                                            pathArray[0]);
            if (string.IsNullOrEmpty(filePath))
                return;

            Debug.Log(filePath);

            //Add Component
            if (_udonBehaviour == null)
                _udonBehaviour = Undo.AddComponent<UdonBehaviour>(_selectGameObject);

            //ScriptableAsset生成
            var obj = CreateInstance<UdonTriggerProgramAsset>();
            AssetDatabase.CreateAsset(obj, filePath);
            AssetDatabase.Refresh();

            _triggerProgramAsset = (UdonTriggerProgramAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(filePath, typeof(UdonTriggerProgramAsset));

            //ProgramAsset登録
            var udonBehaviourSO = new UnityEditor.SerializedObject(_udonBehaviour);
            udonBehaviourSO.Update();
            udonBehaviourSO.FindProperty("programSource").objectReferenceValue = _triggerProgramAsset;
            udonBehaviourSO.ApplyModifiedProperties();

            Debug.Log($"Create Udon Trigger Program Asset. : {filePath}");

            Repaint();
        }

        private void OnSelectionChanged()
        {
            _selectGameObject = Selection.activeGameObject;
            _udonBehaviour = _selectGameObject?.GetComponent<UdonBehaviour>();

            Repaint();
        }

        private void OnHierarchyChange()
        {

            Repaint();
        }
    }
}