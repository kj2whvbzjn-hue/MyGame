using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AutomationTaskAttribute : Attribute
{
    public int Order { get; }

    public AutomationTaskAttribute(int order = 0)
    {
        Order = order;
    }
}

public static class AutomationTaskRunner
{
    public static void RunAll()
    {
        var tasks = FindTasks()
            .OrderBy(task => task.attribute.Order)
            .ThenBy(task => task.method.DeclaringType?.FullName)
            .ThenBy(task => task.method.Name)
            .ToList();

        if (tasks.Count == 0)
        {
            Debug.Log("[Automation] No C# automation tasks registered.");
            return;
        }

        foreach (var task in tasks)
        {
            string name = $"{task.method.DeclaringType?.FullName}.{task.method.Name}";
            Debug.Log($"[Automation] Running C# task: {name}");

            try
            {
                task.method.Invoke(null, null);
            }
            catch (TargetInvocationException ex)
            {
                throw new Exception(
                    $"Automation task failed: {name}",
                    ex.InnerException ?? ex);
            }
        }
    }

    private static IEnumerable<(MethodInfo method, AutomationTaskAttribute attribute)> FindTasks()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).ToArray();
            }

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attribute = method.GetCustomAttribute<AutomationTaskAttribute>();
                    if (attribute == null)
                        continue;

                    if (method.ReturnType != typeof(void) ||
                        method.GetParameters().Length != 0)
                    {
                        throw new InvalidOperationException(
                            $"Automation task must be a parameterless static void method: " +
                            $"{type.FullName}.{method.Name}");
                    }

                    yield return (method, attribute);
                }
            }
        }
    }
}
