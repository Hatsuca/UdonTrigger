using System;
using System.Collections.Generic;
using UnityEditor;
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

            //DrawPublicVariables(udonBehaviour, ref dirty);
            DrawAssemblyErrorTextArea();
            DrawAssemblyTextArea(false, ref dirty);

        }

        protected override void RefreshProgramImpl()
        {

            CompileTrigger();
            base.RefreshProgramImpl();
            ApplyDefaultValuesToHeap();
        }

        protected void CompileTrigger()
        {
            //udonAssembly = ;
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

            if(variableValue == null || !variableValue.Equals(defaultValue))
            {
                if(defaultValue != null)
                {
                    if(!dirty && GUILayout.Button("Reset to Default Value"))
                    {
                        variableValue = defaultValue;
                        dirty = true;
                    }
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
    }
}
