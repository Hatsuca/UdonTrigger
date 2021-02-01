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

        public enum BroadcastType
        {
            Always, Master, Local, Owner, AlwaysUnbuffered, MasterUnbuffered, OwnerUnbuffered, AlwaysBufferOne, MasterBufferOne, OwnerBufferOne
        }

        public enum BoolOp
        {
            UnUsed, False, True, Toggle
        }

        [System.Serializable]
        public struct Triggers
        {
            public int triggerType;
            public List<Events> events;
            public string name;
            public BroadcastType broadcastType;

            public Triggers(int _triggerType)
            {
                triggerType = _triggerType;
                broadcastType = BroadcastType.AlwaysBufferOne;

                TriggerType type = (TriggerType)triggerType;
                switch(type)
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

            public Triggers(int _triggerType,BroadcastType _broadcastType, List<Events> _events, string _name)
            {
                triggerType = _triggerType;
                broadcastType = _broadcastType;
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
            public int eventType;
            public string parameterString;
            public int parameterBoolOp;
            public float parameterFloat;
            public int parameterInt;
            public Object[] parameterObjects;


            public Events(int _eventType)
            {
                eventType = _eventType;

                EventType type = (EventType)eventType;
                switch(type)
                {
                    case EventType.SetGameObjectActive:
                        parameterString = "";
                        parameterBoolOp = (int)BoolOp.True;
                        parameterFloat = 0f;
                        parameterInt = 0;
                        parameterObjects = new Object[0];
                        break;

                    default:
                        parameterString = "";
                        parameterBoolOp = (int)BoolOp.UnUsed;
                        parameterFloat = 0f;
                        parameterInt = 0;
                        parameterObjects = new Object[0];
                        break;
                }
            }

            public Events(int _eventType, string _parameterString, int _parameterBoolOp, float _parameterFloat, int _parameterInt, Object[] _parameterObjects)
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