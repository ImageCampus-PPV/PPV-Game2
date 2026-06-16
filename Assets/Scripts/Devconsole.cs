using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Devconsole : MonoBehaviour
{
    private EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();

    private MethodInfo _raiseEvent;

    private Dictionary<string, Type> _eventsTypeByName;
    private Dictionary<Type, List<Type>> _variablesTypeByEventType;

    private TMP_InputField inputField;

    public Devconsole()
    {
    }

    private void Start()
    {
        inputField = GetComponent<TMP_InputField>();

        _eventsTypeByName = new Dictionary<string, Type>();
        _variablesTypeByEventType = new Dictionary<Type, List<Type>>();

        _raiseEvent = EventBus.GetType().GetMethod(nameof(EventBus.Raise));

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsValueType && !type.IsEnum && !type.IsPrimitive && typeof(IEvent).IsAssignableFrom(type))
                {
                    _eventsTypeByName.Add(type.Name, type);

                    if (!_variablesTypeByEventType.ContainsKey(type))
                        _variablesTypeByEventType.Add(type, new List<Type>());

                    foreach (FieldInfo variable in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                        _variablesTypeByEventType[type].Add(variable.FieldType);
                }
            }
        }

        inputField.onSubmit.AddListener(OnSubmitCommand);
    }

    private void OnSubmitCommand(string text)
    {
        string[] subStrings = text.Split(':', 2);
        string eventName = subStrings[0].Trim();

        bool error = false;
        string[] rawParams = null;
        Type eventType = null;
        object[] parameters = new object[0];

        if (!_eventsTypeByName.ContainsKey(eventName))
        {
            Debug.LogWarning($"Event: {eventName} doesn't exist.");
            return;
        }

        eventType = _eventsTypeByName[eventName];

        if (subStrings.Length > 1)
        {
            rawParams = subStrings[1].Split(new[] { ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

            TrimStringParameters(ref rawParams);

            parameters = GetStringsAsParameters(eventType, rawParams, ref error);
        }

        if (!error)
            if (_variablesTypeByEventType[eventType].Count == parameters.Length)
                RaiseEvent(eventType, parameters);
            else
                Debug.Log($"You are short on the amount of parameters {parameters.Length} / {_variablesTypeByEventType[eventType].Count}");
    }

    private void TrimStringParameters(ref string[] rawParams)
    {
        for (int i = 0; i < rawParams.Length; ++i)
            rawParams[i] = rawParams[i].Trim();
    }

    private object[] GetStringsAsParameters(Type eventType, string[] parametersAsString, ref bool error)
    {
        object[] parameters = new object[parametersAsString.Length];
        int index = 0;

        foreach (Type variableType in _variablesTypeByEventType[eventType])
        {
            if (index >= parametersAsString.Length)
                break;

            try
            {
                parameters[index] = Convert.ChangeType(parametersAsString[index], variableType);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                error = true;
                break;
            }

            ++index;
        }

        return parameters;
    }

    private void RaiseEvent(Type eventType, object[] parameters)
    {
        _raiseEvent.MakeGenericMethod(eventType).Invoke(EventBus, new object[] { parameters });
    }
}

