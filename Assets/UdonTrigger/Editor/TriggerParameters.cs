using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UdonTrigger
{
    public class TriggerParameters
    {
        public enum TriggerType
        {
            OnInteract
        }

        public enum EventType
        {
            SetGameObjectActive, SetComponentActive
        }

        public enum BoolOp
        {
            False, True, Toggle
        }

        public struct Triggers
        {
            public TriggerType triggerType;
            public List<Events> events;
            public string name;

            public Triggers(TriggerType _triggerType, List<Events> _events, string _name)
            {
                triggerType = _triggerType;
                events = _events;
                name = _name;
            }
        }

        public struct Events
        {
            public EventType eventType;
            public string parameterString;
            public BoolOp parameterBoolOp;
            public float parameterFloat;
            public int parameterInt;
            public List<Object> parameterObjects;

            public Events(EventType _eventType, string _parameterString, BoolOp _parameterBoolOp, float _parameterFloat, int _parameterInt, List<Object> _parameterObjects)
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