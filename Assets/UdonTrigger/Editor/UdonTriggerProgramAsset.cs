using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Editor.ProgramSources;
using VRC.Udon.Serialization.OdinSerializer;

namespace UdonTrigger
{
    [CreateAssetMenu(menuName = "VRChat/Udon/Udon Trigger Program Asset", fileName = "New Udon Trigger Program Asset")]
    public class UdonTriggerProgramAsset : UdonAssemblyProgramAsset
    {
        [SerializeField]
        private bool showAssembly = false;

        [NonSerialized, OdinSerialize]
        private Dictionary<string, (object value, Type type)> heapDefaultValues = new Dictionary<string, (object value, Type type)>();

        public List<TriggerParameters.Triggers> triggers = new List<TriggerParameters.Triggers>();

        protected override void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            DrawUdonTriggerInspector();
            DrawPublicVariables(udonBehaviour, ref dirty);
            DrawAssemblyErrorTextArea();
            DrawAssemblyTextArea(false, ref dirty);

        }

        protected override void RefreshProgramImpl()
        {

            CompileTriggers();
            base.RefreshProgramImpl();
            ApplyDefaultValuesToHeap();
        }

        protected void CompileTriggers()
        {
            udonAssembly = UdonTriggerAssemblyBuilder.GetAssemblyStr(triggers, out heapDefaultValues);
        }

        protected override void DrawAssemblyTextArea(bool allowEditing, ref bool dirty)
        {
            EditorGUI.BeginChangeCheck();
            bool newShowAssembly = EditorGUILayout.Foldout(showAssembly, "Compiled Trigger Assembly");
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Toggle Assembly Foldout");
                showAssembly = newShowAssembly;
            }

            if (!showAssembly)
            {
                return;
            }

            EditorGUI.indentLevel++;
            base.DrawAssemblyTextArea(allowEditing, ref dirty);
            EditorGUI.indentLevel--;
        }

        protected void ApplyDefaultValuesToHeap()
        {
            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if (symbolTable == null || heap == null)
            {
                return;
            }

            foreach (KeyValuePair<string, (object value, Type type)> defaultValue in heapDefaultValues)
            {
                if (!symbolTable.HasAddressForSymbol(defaultValue.Key))
                {
                    continue;
                }

                uint symbolAddress = symbolTable.GetAddressFromSymbol(defaultValue.Key);
                (object value, Type declaredType) = defaultValue.Value;
                if (typeof(UnityEngine.Object).IsAssignableFrom(declaredType))
                {
                    if (value != null && !declaredType.IsInstanceOfType(value))
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }

                    if ((UnityEngine.Object)value == null)
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }
                }

                if (value != null)
                {
                    if (!declaredType.IsInstanceOfType(value))
                    {
                        value = declaredType.IsValueType ? Activator.CreateInstance(declaredType) : null;
                    }
                }

                if (declaredType == null)
                {
                    declaredType = typeof(object);
                }
                heap.SetHeapVariable(symbolAddress, value, declaredType);
            }
        }

        protected override object GetPublicVariableDefaultValue(string symbol, Type type)
        {

            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if (symbolTable == null || heap == null)
            {
                return null;
            }

            if (!heapDefaultValues.ContainsKey(symbol))
            {
                return null;
            }

            (object value, Type declaredType) = heapDefaultValues[symbol];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(declaredType))
            {
                return value;
            }

            return (UnityEngine.Object)value == null ? null : value;
        }

        protected override object DrawPublicVariableField(string symbol, object variableValue, Type variableType, ref bool dirty,
            bool enabled)
        {

            EditorGUILayout.BeginHorizontal();
            variableValue = base.DrawPublicVariableField(symbol, variableValue, variableType, ref dirty, enabled);
            object defaultValue = null;
            if(heapDefaultValues.ContainsKey(symbol))
            {
                defaultValue = heapDefaultValues[symbol].value;
            }

            //強制defaultValue固定
            if(variableValue == null || !variableValue.Equals(defaultValue))
            {
                if(defaultValue != null)
                {
                    variableValue = defaultValue;
                    dirty = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            return variableValue;
        }

        #region Serialization Methods

        protected override void OnAfterDeserialize()
        {

        }

        #endregion

        
        //Udon Trigger Inspector GUI
        public SerializedObject serializedObject;
        private ReorderableList[] eventLists = new ReorderableList[0];
        private ReorderableList[] relayLists = new ReorderableList[0];
        private ReorderableList[] objectLists = new ReorderableList[0];
        private bool[] visible = new bool[0];

        private SerializedProperty triggersProperty;
        private SerializedProperty proximityProperty;
        private SerializedProperty interactTextProperty;
        private SerializedProperty ownershipProperty;
        private SerializedProperty drawLinesProperty;

        private TriggerParameters.TriggerType addTriggerSelectedType = TriggerParameters.TriggerType.OnInteract;

        //未実装変数
        [SerializeField]
        private bool takesOwnershipIfNecessary = false;
        [SerializeField]
        private float proximity = 0f;
        [SerializeField]
        private string interactText = "";

        private void DrawUdonTriggerInspector()
        {
            //シリアライズオブジェクト取得
            if (serializedObject == null)
                serializedObject = new SerializedObject(this);

            triggersProperty = serializedObject.FindProperty("triggers");
            proximityProperty = serializedObject.FindProperty("proximity");
            interactTextProperty = serializedObject.FindProperty("interactText");
            ownershipProperty = serializedObject.FindProperty("takesOwnershipIfNecessary");
            //drawLinesProperty = serializedObject.FindProperty("trawLines");

            serializedObject.Update();

            SerializedProperty triggers = triggersProperty.Copy();
            int triggersLength = triggers.arraySize;

            //トリガー数に変動あったらReorderableList初期化
            if (eventLists.Length != triggersLength)
                eventLists = new ReorderableList[triggersLength];

            if (relayLists.Length != triggersLength)
                relayLists = new ReorderableList[triggersLength];

            if (objectLists.Length != triggersLength)
                objectLists = new ReorderableList[triggersLength];

            //折り畳みフラグ移植
            if (visible.Length != triggersLength)
            {
                bool[] newVisible = new bool[triggersLength];
                for (int i = 0; i < visible.Length && i < newVisible.Length; ++i)
                    newVisible[i] = visible[i];
                for (int i = visible.Length; i < newVisible.Length; ++i)
                    newVisible[i] = true;
                visible = newVisible;
            }


            //GUI開始
            EditorGUILayout.Separator();

            GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 30));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(ownershipProperty, new GUIContent("Take Ownership of Action Target"));

            EditorGUILayout.Space();

            //トリガー描画
            RenderTriggers();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));

            serializedObject.ApplyModifiedProperties();
        }

        //トリガー描画
        private void RenderTriggers()
        {
            GUIStyle triggerStyle = new GUIStyle(EditorStyles.helpBox);

            SerializedProperty triggers = triggersProperty.Copy();
            int triggersLength = triggers.arraySize;

            //トリガーの表示ループ
            List<int> to_remove = new List<int>();
            for (int i=0; i < triggersLength; ++i)
            {
                SerializedProperty triggerProperty = triggers.GetArrayElementAtIndex(i);
                SerializedProperty broadcastProperty = triggerProperty.FindPropertyRelative("broadcastType");

                EditorGUILayout.BeginVertical(triggerStyle);

                //折り畳み部分表示
                if (RenderTriggerHeader(triggerProperty, ref visible[i]))
                {
                    to_remove.Add(i);
                    EditorGUILayout.EndVertical();

                    continue;
                }

                //折り畳み
                if (!visible[i])
                {
                    EditorGUILayout.EndVertical();
                    continue;
                }

                //RPC関連警告

                EditorGUILayout.Separator();
                
                //トリガーオプション描画
                RenderTriggerEditor(triggerProperty, i);
                
                //イベント描画
                if (eventLists.Length == triggersLength)
                {
                    EditorGUILayout.Separator();

                    if (triggerProperty.FindPropertyRelative("triggerType").intValue != (int)TriggerParameters.TriggerType.Relay)
                    {
                        //イベント描画
                        RenderTriggerEventsEditor(triggerProperty, i);

                        EditorGUILayout.Separator();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }

            //Removeリストのトリガーを削除

            //Trigger追加リストとボタン描画
            RenderAddTrigger();
        }

        //トリガー追加ボタン描画
        private void RenderAddTrigger()
        {
            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(15f));
            EditorGUILayout.Space();

            Rect selectedRect = new Rect(rect.x, rect.y, rect.width / 4 * 3 - 5, rect.height);
            Rect addRect = new Rect(selectedRect.x + selectedRect.width + 5, rect.y, rect.width / 4, rect.height);

            addTriggerSelectedType =
                (TriggerParameters.TriggerType)EditorGUI.Popup(selectedRect, (int)addTriggerSelectedType, System.Enum.GetNames(typeof(TriggerParameters.TriggerType)));

            if (GUI.Button(addRect, "Add"))
            {
                SerializedProperty triggersAry = triggersProperty;

                // hacks : なにやってるのこれ…
                triggersAry.Next(true);
                triggersAry.Next(true);

                int triggersLength = triggersAry.intValue;
                triggersAry.intValue = triggersLength + 1;
                triggersAry.Next(true);

                for (int idx = 0; idx < triggersLength; ++idx)
                    triggersAry.Next(false);

                triggersAry.FindPropertyRelative("triggerType").intValue = (int)addTriggerSelectedType;
                triggersAry.FindPropertyRelative("broadcastType").intValue = (int)TriggerParameters.BroadcastType.AlwaysBufferOne;
            }

            EditorGUILayout.EndHorizontal();
        }

        //トリガーヘッダー描画
        private bool RenderTriggerHeader(SerializedProperty triggerProperty, ref bool expand)
        {
            bool delete = false;

            if (!delete)
            {
                Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(15f));
                EditorGUILayout.Space();

                int baseWidth = (int)((rect.width - 40) / 4);

                Rect foldoutRect = new Rect(rect.x + 10, rect.y, 20, rect.height);
                Rect typeRect = new Rect(rect.x + 20, rect.y, baseWidth, rect.height);
                Rect broadcastRect = new Rect(rect.x + 25 + baseWidth, rect.y, baseWidth, rect.height);
                Rect randomRect = new Rect(rect.x + 30 + baseWidth * 2, rect.y, baseWidth, rect.height);
                Rect removeRect = new Rect(rect.x + 35 + baseWidth * 3, rect.y, baseWidth, rect.height);

                expand = EditorGUI.Foldout(foldoutRect, expand, GUIContent.none);

                SerializedProperty triggerTypeProperty = triggerProperty.FindPropertyRelative("triggerType");
                TriggerParameters.TriggerType currentType = (TriggerParameters.TriggerType)triggerTypeProperty.intValue;

                SerializedProperty nameProperty = triggerProperty.FindPropertyRelative("name");
                if (string.IsNullOrEmpty(nameProperty.stringValue))
                    nameProperty.stringValue = "Unnamed";

                triggerTypeProperty.intValue = EditorGUI.Popup(typeRect, triggerTypeProperty.intValue, System.Enum.GetNames(typeof(TriggerParameters.TriggerType)));

                SerializedProperty broadcastTypeProperty = triggerProperty.FindPropertyRelative("broadcastType");
                broadcastTypeProperty.intValue = EditorGUI.Popup(broadcastRect, broadcastTypeProperty.intValue, System.Enum.GetNames(typeof(TriggerParameters.BroadcastType)));

                //ランダマイズ用パラメータ
                SerializedProperty probabilitiesProperty = triggerProperty.FindPropertyRelative("probabilities");
                SerializedProperty probabilitityLockProperty = triggerProperty.FindPropertyRelative("probabilityLock");
                SerializedProperty eventsProperty = triggerProperty.FindPropertyRelative("events");

                //ランダマイズ描画
                if (eventsProperty.arraySize < 1)
                    GUI.enabled = false;
                if (GUI.Toggle(randomRect, probabilitiesProperty.arraySize > 0, new GUIContent("Randomize")))
                    probabilitityLockProperty.arraySize = probabilitiesProperty.arraySize = eventsProperty.arraySize;
                else
                    probabilitityLockProperty.arraySize = probabilitiesProperty.arraySize = 0;
                GUI.enabled = true;

                if (GUI.Button(removeRect, "Remove"))
                    delete = true;

                EditorGUILayout.EndHorizontal();

                //broadcastヘルプボックス
                if (expand)
                {
                    EditorGUILayout.HelpBox("Test.", MessageType.Info);
                }

            }

            return delete;
        }

        //トリガーオプション描画
        private void RenderTriggerEditor(SerializedProperty triggerProperty, int idx)
        {
            //ディレイ描画
            EditorGUILayout.PropertyField(triggerProperty.FindPropertyRelative("afterSeconds"), new GUIContent("Delay in Seconds"));

            //TriggerTypeによって必要なプロパティ描画
            TriggerParameters.TriggerType triggerType = (TriggerParameters.TriggerType)triggerProperty.FindPropertyRelative("triggerType").intValue;
            switch(triggerType)
            {
                case TriggerParameters.TriggerType.Custom:
                    break;
                default:
                    if (triggerType == TriggerParameters.TriggerType.OnInteract)
                        RenderInteractableEditor();
                    else
                        RenderEmpty(triggerProperty);
                    break;
            }
        }

        #region トリガーオプション描画メソッド
        //インタラクト
        private void RenderInteractableEditor()
        {
            EditorGUILayout.PropertyField(interactTextProperty, new GUIContent("Interaction Text"));
            proximityProperty.floatValue = EditorGUILayout.Slider("Proximity", proximityProperty.floatValue, 0f, 100f);
        }

        private void RenderEmpty(SerializedProperty triggerProperty)
        {
        }
        #endregion

        //イベントReorderableList描画
        private void RenderTriggerEventsEditor(SerializedProperty triggerProperty, int idx)
        {
            //ReorderableList作成
            if (eventLists[idx] == null)
            {
                ReorderableList newList = new ReorderableList(serializedObject, triggerProperty.FindPropertyRelative("events"), true, true, true, true);
                newList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Actions");
                newList.drawElementCallback = (Rect rect, int index, bool isActive, bool isForcused) =>
                {
                    SerializedProperty eventsListProperty = triggerProperty.FindPropertyRelative("events");
                    SerializedProperty probabilitiesProperty = triggerProperty.FindPropertyRelative("probabilities");
                    SerializedProperty probabilityLockProperty = triggerProperty.FindPropertyRelative("probabilityLock");
                    //SerializedProperty shadowListProperty = triggerProperty.FindPropertyRelative("DataStorageShadowValues");

                    //if (shadowListProperty != null && shadowListProperty.arraySize != eventsListProperty.arraySize)
                    //    shadowListProperty.arraySize = eventsListProperty.arraySize;

                    //SerializedProperty shadowProperty = shadowListProperty == null ? null : shadowListProperty.GetArrayElementAtIndex(index);
                    SerializedProperty eventProperty = eventsListProperty.GetArrayElementAtIndex(index);
                    SerializedProperty eventTypeProperty = eventProperty.FindPropertyRelative("eventType");
                    SerializedProperty parameterStringProperty = eventProperty.FindPropertyRelative("parameterString");

                    string label = ((TriggerParameters.EventType)eventTypeProperty.intValue).ToString();
                    if (!string.IsNullOrEmpty(parameterStringProperty.stringValue))
                        label += " (" + parameterStringProperty.stringValue + ")";

                    //Randomizeされてたら表示切替
                    EditorGUI.LabelField(rect, label);

                    if (isForcused)
                        objectLists[idx] = null;
                    if (isActive)
                    {
                        EditorGUILayout.Space();

                        //イベントオプション描画
                        RenderEventEditor(null, triggerProperty, eventProperty, idx);
                    }
                };
                newList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
                {
                    GenericMenu menu = new GenericMenu();
                    SerializedProperty eventsList = triggerProperty.FindPropertyRelative("events");

                    var eventNames = System.Enum.GetNames(typeof(TriggerParameters.EventType));
                    foreach(string name in eventNames)
                    {
                        TriggerParameters.EventType et;
                        Enum.TryParse(name, out et);
                        menu.AddItem(new GUIContent("BasicEvents/" + name), false, (t) =>
                        {
                            //イベント追加
                            eventsList.arraySize++;

                            SerializedProperty newEventProperty = eventsList.GetArrayElementAtIndex(eventsList.arraySize - 1);
                            newEventProperty.FindPropertyRelative("eventType").intValue = (int)(TriggerParameters.EventType)t;
                            newEventProperty.FindPropertyRelative("parameterObjects").arraySize = 0;
                            newEventProperty.FindPropertyRelative("parameterInt").intValue = 0;
                            newEventProperty.FindPropertyRelative("parameterFloat").floatValue = 0f;
                            newEventProperty.FindPropertyRelative("parameterString").stringValue = null;

                            serializedObject.ApplyModifiedProperties();
                        }, et);
                    }
                    //EventsFromSceneのイベントの取得処理
                    menu.ShowAsContext();

                    //イベント追加したらReorderableList再読み込み
                    eventLists = new ReorderableList[0];
                    objectLists = new ReorderableList[0];
                    relayLists = new ReorderableList[0];
                };

                eventLists[idx] = newList;
            }

            ReorderableList eventList = eventLists[idx];
            eventList.DoLayoutList();
        }

        //イベントオプション描画
        public void RenderEventEditor(SerializedProperty shadowProperty, SerializedProperty triggerProperty, SerializedProperty eventProperty, int triggerIdx)
        {
            /*
            SerializedProperty eventTypeProperty = eventProperty.FindPropertyRelative("eventType");
            //SerializedProperty parameterObjectProperty = eventProperty.FindPropertyRelative("parameterObject");
            SerializedProperty parameterObjectsProperty = eventProperty.FindPropertyRelative("parameterObjects");
            SerializedProperty parameterStringProperty = eventProperty.FindPropertyRelative("parameterString");
            SerializedProperty parameterBoolOpProperty = eventProperty.FindPropertyRelative("parameterBoolOp");
            SerializedProperty parameterFloatProperty = eventProperty.FindPropertyRelative("parameterFloat");
            SerializedProperty parameterIntProperty = eventProperty.FindPropertyRelative("parameterInt");
            //SerializedProperty parameterBytesProperty = eventProperty.FindPropertyRelative("parameterBytes");
            */
        }

        #region イベントオプション描画メソッド

        #endregion
    }
}
