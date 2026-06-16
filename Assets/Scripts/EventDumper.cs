#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ImageCampus.ToolBox.Events;
using UnityEditor;
using UnityEngine;

public static class EventDumper
{
    private const string OutputPath = "EventDump.txt";

    [MenuItem("Tools/Dump Events To TXT")]
    public static void DumpEvents()
    {
        Dictionary<string, Type> eventsByName = new Dictionary<string, Type>();
        Dictionary<Type, List<FieldInfo>> fieldsByEvent = new Dictionary<Type, List<FieldInfo>>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsValueType
                    && !type.IsEnum
                    && !type.IsPrimitive
                    && typeof(IEvent).IsAssignableFrom(type))
                {
                    eventsByName[type.Name] = type;

                    if (!fieldsByEvent.ContainsKey(type))
                        fieldsByEvent[type] = new List<FieldInfo>();

                    foreach (FieldInfo field in type.GetFields(
                        BindingFlags.DeclaredOnly |
                        BindingFlags.Instance |
                        BindingFlags.Public))
                    {
                        fieldsByEvent[type].Add(field);
                    }
                }
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("====================================================");
        sb.AppendLine("EVENT");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  Total events: {eventsByName.Count}");
        sb.AppendLine("====================================================");
        sb.AppendLine();

        List<string> sortedNames = new List<string>(eventsByName.Keys);
        sortedNames.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (string name in sortedNames)
        {
            Type eventType = eventsByName[name];
            List<FieldInfo> fields = fieldsByEvent[eventType];

            sb.AppendLine($"EVENT: {name}");
            if (fields.Count == 0)
            {
                sb.AppendLine("  Parameters: (none)");
            }
            else
            {
                sb.AppendLine($"  Parameters ({fields.Count}):");
                foreach (FieldInfo field in fields)
                    sb.AppendLine($"    - {field.Name} : {field.FieldType.Name}");

                sb.Append("  Usage hint : ");
                sb.Append(name);
                if (fields.Count > 0)
                {
                    sb.Append(" : ");
                    List<string> paramHints = new List<string>();
                    foreach (FieldInfo field in fields)
                        paramHints.Add($"<{field.Name}>");
                    sb.Append(string.Join(", ", paramHints));
                }
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        string fullPath = Path.Combine(Application.dataPath, "..", OutputPath);
        fullPath = Path.GetFullPath(fullPath);
        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

        Debug.Log($"[{nameof(EventDumper)}] Wrote {eventsByName.Count} events to: {fullPath}");
        EditorUtility.RevealInFinder(fullPath);
    }
}
#endif