using System.Collections.Generic;
using System;
using System.Text;

namespace UdonTrigger {
    public static class UdonTriggerAssemblyBuilder
    {
        public static string GetAssemblyStr(List<TriggerParameters.Triggers>triggerList, out Dictionary<string, (object value, Type type)> defaultValues)
        {
            StringBuilder assemblyTextBuilder = new StringBuilder();
            Dictionary<string, (object value, Type type)> values = new Dictionary<string, (object value, Type type)>();
            List<EventBuilder> eventList = new List<EventBuilder>();

            int eventCount = 0;
            foreach(TriggerParameters.Triggers t in triggerList)
            {
                foreach(TriggerParameters.Events e in t.events)
                {
                    switch(e.eventType)
                    {
                        case TriggerParameters.EventType.SetGameObjectActive:
                            eventList.Add(new SetGameObjectActive(t.triggerType, eventCount, e.parameterObjects, e.parameterBoolOp));
                            break;
                    }
                    eventCount++;
                }
            }

            //Construct Data Block and Get Default Values
            assemblyTextBuilder.Append(".data_start\n\n");

            foreach(var eventBuilder in eventList)
            {
                var valueDic = eventBuilder.GetValues();
                foreach(KeyValuePair<string, (object value, Type type)> item in valueDic)
                {
                    string typeStr = "";
                    if (item.Value.type == typeof(bool))
                    {
                        typeStr = "SystemBoolean, null";
                    }else if(item.Value.type == typeof(UnityEngine.GameObject))
                    {
                        typeStr = "UnityEngineGameObject, this";
                    }
                    assemblyTextBuilder.Append($"{new string(' ', 4)}{item.Key}: %{typeStr}\n");
                }

                values.Marge(valueDic);
            }
            defaultValues = values;

            assemblyTextBuilder.Append(".data_end\n\n");


            //Construct Code Block
            assemblyTextBuilder.Append(".code_start\n\n");

            int currentTriggerType = -1;
            int currentLine = 0;
            for(int i=0; i < eventList.Count; i++)
            {
                TriggerParameters.TriggerType triggerType = eventList[i].triggerType;
                if (currentTriggerType != (int)triggerType)
                {
                    currentTriggerType = (int)triggerType;
                    string str = "";
                    switch(triggerType)
                    {
                        case TriggerParameters.TriggerType.OnInteract:
                            str = "_interact";
                            break;
                        case TriggerParameters.TriggerType.OnPickup:
                            str = "_onPickup";
                            break;
                    }
                    assemblyTextBuilder.Append($"{new string(' ', 4)}.export {str}\n\n");
                    assemblyTextBuilder.Append($"{new string(' ', 4)}{str}:\n\n");
                }

                // Build Code
                currentLine = eventList[i].BuildCode(assemblyTextBuilder, currentLine);

                if (i < eventList.Count - 1)
                {
                    if (currentTriggerType != (int)eventList[i + 1].triggerType)
                    {
                        assemblyTextBuilder.Append($"{new string(' ', 8)}JUMP, 0xFFFFFFFC\n\n");
                        currentLine++;
                    }
                }else
                {
                    assemblyTextBuilder.Append($"{new string(' ', 8)}JUMP, 0xFFFFFFFC\n\n");
                    currentLine++;
                }
            }

            assemblyTextBuilder.Append("\n\n.code_end");


            return assemblyTextBuilder.ToString();
        }
    }

    public abstract class EventBuilder
    {
        public TriggerParameters.TriggerType triggerType;

        protected int _eventCount;
        protected StringBuilder _stringBuilder;
        protected object[] _objects;
        protected int _lineSum;

        public abstract int BuildCode(StringBuilder sb, int lineCount);

        public abstract Dictionary<string, (object value, Type type)> GetValues();

        #region Create Code Line Method
        protected void AppendLine(string line)
        {
            _stringBuilder.Append($"{new string(' ', 8)}{line}\n");
            _lineSum++;
        }
        protected void AddNop()
        {
            AppendLine("NOP");
        }
        protected void AddPush(string value)
        {
            AppendLine($"PUSH, {value}");
        }
        protected void AddPop()
        {
            AppendLine("POP");
        }
        protected void AddJumpIfFalse(int jumpLine)
        {
            AppendLine($"JUMP_IF_FALSE, {convertBase(_lineSum + jumpLine)}");
        }
        protected void AddJump(int jumpLine)
        {
            AppendLine($"JUMP, {convertBase(_lineSum + jumpLine)}");
        }
        protected void AddExtern(string methodStr)
        {
            AppendLine($"EXTERN, \"{methodStr}\"");
        }
        protected void AddAnnotation()
        {
            AppendLine("ANNOTATION");
        }
        protected void AddJumpIndirect(string value)
        {
            AppendLine($"JUMP_INDIRECT, {value}");
        }
        protected void AddCopy()
        {
            AppendLine("COPY");
        }
        private string convertBase(int dec)
        {
            return $"0x{String.Format("{0:X8}", dec)}";
        }
        #endregion
    }

    public class SetGameObjectActive : EventBuilder
    {
        private TriggerParameters.BoolOp _boolOp;

        public SetGameObjectActive(TriggerParameters.TriggerType tt, int eventCount, object[] objects, TriggerParameters.BoolOp boolOp)
        {
            triggerType = tt;
            _eventCount = eventCount;
            _objects = objects;
            _boolOp = boolOp;
        }

        public override int BuildCode(StringBuilder sb, int lineCount)
        {
            _stringBuilder = sb;
            _lineSum = lineCount;

            for (int i=0; i < _objects.Length; i++)
            {
                if (_boolOp == TriggerParameters.BoolOp.False || _boolOp == TriggerParameters.BoolOp.True)
                {
                    AddPush($"object{i}_{_eventCount}");
                    AddPush($"value0_{_eventCount}");
                    AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
                }else if (_boolOp == TriggerParameters.BoolOp.Toggle)
                {
                    AddPush($"object{i}_{_eventCount}");
                    AddPush($"value0_{_eventCount}");
                    AddExtern("UnityEngineGameObject.__get_activeSelf__SystemBoolean");
                    AddPush($"value0_{_eventCount}");
                    AddJumpIfFalse(5);
                    AddPush($"object{i}_{_eventCount}");
                    AddPush($"value2_{_eventCount}");
                    AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
                    AddJump(4);
                    AddPush($"object{i}_{_eventCount}");
                    AddPush($"value1_{_eventCount}");
                    AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
                }
            }

            return _lineSum;
        }

        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            switch (_boolOp)
            {
                case TriggerParameters.BoolOp.False:
                    valuesDic.Add($"value0_{_eventCount}", (false, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.True:
                    valuesDic.Add($"value0_{_eventCount}", (true, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.Toggle:
                    valuesDic.Add($"value0_{_eventCount}", (null, null));
                    valuesDic.Add($"value1_{_eventCount}", (false, typeof(bool)));
                    valuesDic.Add($"value2_{_eventCount}", (true, typeof(bool)));
                    break;
            }

            for (int i=0; i < _objects.Length; i++)
            {
                if (_objects[i] != null)
                    valuesDic.Add($"object{i}_{_eventCount}", (_objects[i], typeof(UnityEngine.GameObject)));
            }

            return valuesDic;
        }
    }
}