using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace UdonTrigger {
    public class UdonTriggerAssemblyBuilder
    {
        private StringBuilder _assemblyTextBuilder = new StringBuilder();
        private int _indentLevel = 1;
        private int _assemblyLineCount = 0;
        private Dictionary<string, (object value, Type type)> _defaultValues = new Dictionary<string, (object value, Type type)>();
        private List<string> _valueNameList = new List<string>();
        private List<TriggerParameters.Triggers> _triggerList;

        public UdonTriggerAssemblyBuilder(List<TriggerParameters.Triggers> triggers)
        {
            _triggerList = new List<TriggerParameters.Triggers>(triggers);
        }

        private string GetAssemblyStr(out Dictionary<string, (object value, Type type)> defaultValue)
        {
            string codeBlockStr = ConstructCodeBlock();
            string dataBlockStr = ConstructDataBlock();

            //Build Data Block
            _assemblyTextBuilder.Append(".data_start\n\n");
            _assemblyTextBuilder.Append(dataBlockStr);
            _assemblyTextBuilder.Append(".data_end\n\n");

            //Build Code Block
            _assemblyTextBuilder.Append(".code_start\n\n");
            _assemblyTextBuilder.Append(codeBlockStr);
            _assemblyTextBuilder.Append(".code_end");

            defaultValue = _defaultValues;
            return _assemblyTextBuilder.ToString();
        }

        private string ConstructCodeBlock()
        {
            StringBuilder codeBlockStr = new StringBuilder();

            _triggerList.Sort(TriggerParameters.Triggers.ConpareType);

            int currentType = -1;
            for (int i=0; i < _triggerList.Count; i++)
            {
                //Add TriggerType
                if ((int)_triggerList[i].triggerType != currentType)
                {
                    switch(_triggerList[i].triggerType)
                    {
                        case TriggerParameters.TriggerType.OnInteract:
                            break;
                    }
                }
            }

            return codeBlockStr.ToString();
        }

        private string ConstructDataBlock()
        {
            StringBuilder dataBlockStr = new StringBuilder();

            return dataBlockStr.ToString();
        }

        #region Create Code Line Method
        private void AppendLine(string line)
        {
            _assemblyTextBuilder.Append($"{new string(' ', _indentLevel * 8)}{line}\n");
        }
        private void AddNop() {
            AppendLine("NOP");
        }
        private void AddPush(string value)
        {
            AppendLine($"PUSH, {value}");
        }
        private void AddPop()
        {
            AppendLine("POP");
        }
        private void AddJumpIfFalse(int jumpLine)
        {
            int targetLine = _assemblyLineCount + jumpLine;
            AppendLine($"JUMP_IF_FALSE, {convertBase(targetLine)}");
        }
        private void AddJump(int jumpLine)
        {
            int targetLine = _assemblyLineCount + jumpLine;
            AppendLine($"JUMP, {convertBase(targetLine)}");
        }
        private void AddJumpToEnd()
        {
            AppendLine("JUMP, 0xFFFFFFFC");
        }
        private void AddExtern(string methodStr)
        {
            AppendLine($"EXTERN, \"{methodStr}\"");
        }
        private void AddAnnotation()
        {
            AppendLine("ANNOTATION");
        }
        private void AddJumpIndirect(string value)
        {
            AppendLine($"JUMP_INDIRECT, {value}");
        }
        private void AddCopy()
        {
            AppendLine("COPY");
        }
        private string convertBase(int dec)
        {
            return $"0x{String.Format("{0:X8}", dec)}";
        }
        #endregion
    }
}