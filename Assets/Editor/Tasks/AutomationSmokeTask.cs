using UnityEngine;

public static class AutomationSmokeTask
{
    [AutomationTask(order: 1000)]
    public static void Run()
    {
        Debug.Log("[Automation] AutomationSmokeTask PASS.");
    }
}
