using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UdonTrigger
{
    public static class TriggerParameters
    {
        public enum TriggerType
        {
            OnInteract, OnPickup
        }

        public enum EventType
        {
            SetGameObjectActive, SetComponentActive
        }

        public enum BoolOp
        {
            UnUsed, False, True, Toggle
        }

        [System.Serializable]
        public struct Triggers
        {
            public TriggerType triggerType;
            public List<Events> events;
            public string name;

            public Triggers(TriggerType _triggerType)
            {
                triggerType = _triggerType;
                switch(triggerType)
                {
                    case TriggerType.OnInteract:
                        events = new List<Events>();
                        name = "Test Trigger";
                        break;
                    default:
                        events = new List<Events>();
                        name = "";
                        break;
                }
            }

            public Triggers(TriggerType _triggerType, List<Events> _events, string _name)
            {
                triggerType = _triggerType;
                events = _events;
                name = _name;
            }

            public static int ConpareType(Triggers a, Triggers b)
            {
                return a.triggerType.CompareTo(b.triggerType);
            }
        }

        [System.Serializable]
        public struct Events
        {
            public EventType eventType;
            public string parameterString;
            public BoolOp parameterBoolOp;
            public float parameterFloat;
            public int parameterInt;
            public Object[] parameterObjects;

            public Events(EventType _eventType)
            {
                eventType = _eventType;
                switch(eventType)
                {
                    case EventType.SetGameObjectActive:
                        parameterString = "";
                        parameterBoolOp = BoolOp.True;
                        parameterFloat = 0f;
                        parameterInt = 0;
                        parameterObjects = new Object[0];
                        break;

                    default:
                        parameterString = "";
                        parameterBoolOp = BoolOp.UnUsed;
                        parameterFloat = 0f;
                        parameterInt = 0;
                        parameterObjects = new Object[0];
                        break;
                }
            }

            public Events(EventType _eventType, string _parameterString, BoolOp _parameterBoolOp, float _parameterFloat, int _parameterInt, Object[] _parameterObjects)
            {
                eventType = _eventType;
                parameterString = _parameterString;
                parameterBoolOp = _parameterBoolOp;
                parameterFloat = _parameterFloat;
                parameterInt = _parameterInt;
                parameterObjects = _parameterObjects;
            }
        }
    }
}