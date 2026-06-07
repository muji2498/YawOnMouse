using System;
using System.Collections;
using HarmonyLib;
using Rewired;

namespace YawOnMouse.Patches;

// derived from https://github.com/9138noms/TargetCamControl/blob/main/Plugin.cs#L140
[HarmonyPatch(typeof(InputManager_Base), "Awake")]
static class RewiredAwakePatches
{
    private const string TargetCategory = "flight";
    private const int ActionID = 850;

    [HarmonyPrefix]
    static void Prefix(InputManager_Base __instance)
    {
        try
        {
            var userData = __instance.userData;
            if (userData == null) return;

            // im not using a publicised assembly, so im just using reflection instead
            var categories = GetField<IList>(userData, "actionCategories");
            var actions = GetField<IList>(userData, "actions");
            if (categories == null || actions == null) return;

            object targetCat = null;
            foreach (var category in categories)
            {
                var name = GetProp<string>(category, "name");
                if (string.Equals(name, TargetCategory, StringComparison.OrdinalIgnoreCase))
                {
                    targetCat = category;
                    break;
                }
            }

            if (targetCat == null)
            {
                Plugin.Logger.LogWarning("[YawOnMouse] Flight category not found — toggle action will not be registered");
                return;
            }

            bool exists = false;
            int nextId = ActionID;

            foreach (var a in actions)
            {
                var name = GetProp<string>(a, "name");
                if (name == Plugin.ACTION_TOGGLE) exists = true;

                var id = GetProp<int>(a, "id");
                if (id >= nextId) nextId = id + 1;
            }

            if (exists) return;

            var actionType = typeof(InputAction);
            var action = (InputAction) Activator.CreateInstance(actionType, true);

            SetProp(actionType, action, "id", nextId);
            SetProp(actionType, action, "name", Plugin.ACTION_TOGGLE);
            SetProp(actionType, action, "type", InputActionType.Button);
            SetProp(actionType, action, "descriptiveName", "Yaw On Mouse Toggle");

            var targetCatId = GetProp<int>(targetCat, "id");
            SetProp(actionType, action, "categoryId", targetCatId);
            SetField(actionType, action, "_userAssignable", true);

            actions.Add(action);
            
            var catMap = GetField<object>(userData, "actionCategoryMap");
            if (catMap != null)
            {
                var addActionMethod = AccessTools.Method(catMap.GetType(), "AddAction", new[] { typeof(int), typeof(int) });
                addActionMethod?.Invoke(catMap, new object[] { targetCatId, nextId });
            }

            Plugin.RewiredReady = true;
            Plugin.Logger.LogInfo("[YawOnMouse] Registered toggle action in Flight category");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"[YawOnMouse] Action registration: {e}");
        }
    }

    private static T GetProp<T>(object instance, string name) =>
        (T)(AccessTools.Property(instance.GetType(), name)?.GetValue(instance) ?? default(T));

    private static void SetProp<T>(Type type, object instance, string name, T value) =>
        AccessTools.Property(type, name)?.SetValue(instance, value, null);

    private static T GetField<T>(object instance, string name) =>
        (T)(AccessTools.Field(instance.GetType(), name)?.GetValue(instance) ?? default(T));

    private static void SetField<T>(Type type, object instance, string name, T value) =>
        AccessTools.Field(type, name)?.SetValue(instance, value);
}
