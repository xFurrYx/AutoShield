using System;
using HarmonyLib;

namespace AutoShield;

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
internal static class EquipItemPatch
{
    /// <summary>
    /// The eligible one-handed weapon currently being equipped, if any. While it is set,
    /// the vanilla "unequip the old right-hand item" step must not pull the shield away,
    /// so a sword-to-axe swap keeps the shield up instead of lowering and re-raising it.
    /// </summary>
    internal static ItemDrop.ItemData SwappingTo;

    private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
    {
        if (SwappingTo == null && __instance == Player.m_localPlayer && ShieldLogic.IsEligibleWeapon(item))
            SwappingTo = item;
    }

    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result, object[] __args)
    {
        if (!__result || __instance != Player.m_localPlayer)
            return;

        // EquipItem(item, triggerEquipEffects: false) is how the game restores saved
        // equipment on load. Don't add a shield the player didn't have.
        if (__args.Length > 1 && __args[1] is bool triggerEffects && !triggerEffects)
            return;

        ShieldLogic.OnWeaponEquipped((Player)__instance, item);
    }

    private static Exception Finalizer(ItemDrop.ItemData item, Exception __exception)
    {
        if (SwappingTo == item)
            SwappingTo = null;
        return __exception;
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
internal static class UnequipItemPatch
{
    // The game calls UnequipItem on items that aren't equipped (e.g. when moving them
    // into a chest), so only react if the item really was in the right hand.
    private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out bool __state)
    {
        __state = item != null && __instance.m_rightItem == item;
    }

    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __state)
    {
        if (!__state || __instance != Player.m_localPlayer)
            return;

        // Swapping to another eligible one-handed weapon: keep the shield in hand.
        if (EquipItemPatch.SwappingTo != null)
            return;

        ShieldLogic.OnWeaponUnequipped((Player)__instance, item);
    }
}

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
internal static class InventoryGridUpdateGuiPatch
{
    private static void Postfix(InventoryGrid __instance)
    {
        try
        {
            InventoryUi.OnGridUpdated(__instance);
        }
        catch (Exception e)
        {
            // Never let a cosmetic badge problem break the inventory screen.
            InventoryUi.ReportOnce(e);
        }
    }
}
