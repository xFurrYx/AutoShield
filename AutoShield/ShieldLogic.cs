using System;
using System.Collections.Generic;
using System.Globalization;

namespace AutoShield;

/// <summary>
/// All gameplay rules live here. Patches only forward events to these methods.
/// </summary>
internal static class ShieldLogic
{
    // Per-item keys (ItemData.m_customData). The value of ShieldKey is a UTC tick
    // stamp: if several marked shields ever end up in one inventory (e.g. an old one
    // comes back out of a chest), the most recently marked one wins.
    internal const string ShieldKey = "bladeoffurry.autoshield.shield";
    internal const string ExcludedKey = "bladeoffurry.autoshield.excluded";

    // ---------------------------------------------------------------- queries

    internal static bool IsShield(ItemDrop.ItemData item) =>
        item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;

    internal static bool IsOneHanded(ItemDrop.ItemData item) =>
        item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon;

    internal static bool IsExcluded(ItemDrop.ItemData item)
    {
        if (!Plugin.WeaponExclusion.Value || !IsOneHanded(item) || item.m_customData == null)
            return false;
        return item.m_customData.ContainsKey(ExcludedKey);
    }

    /// <summary>A one-handed weapon that should bring the shield along.</summary>
    internal static bool IsEligibleWeapon(ItemDrop.ItemData item) => IsOneHanded(item) && !IsExcluded(item);

    internal static ItemDrop.ItemData GetMarkedShield(Inventory inventory)
    {
        if (inventory == null)
            return null;

        ItemDrop.ItemData best = null;
        long bestStamp = long.MinValue;

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (!IsShield(item) || item.m_customData == null ||
                !item.m_customData.TryGetValue(ShieldKey, out string raw))
                continue;

            long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long stamp);
            if (best == null || stamp > bestStamp)
            {
                best = item;
                bestStamp = stamp;
            }
        }

        return best;
    }

    // ---------------------------------------------------------------- equip events

    internal static void OnWeaponEquipped(Player player, ItemDrop.ItemData weapon)
    {
        if (!Plugin.AutoEquip.Value || !IsEligibleWeapon(weapon))
            return;

        // Only act if the weapon really ended up in hand and the off hand is free.
        if (player.m_rightItem != weapon || player.m_leftItem != null)
            return;

        ItemDrop.ItemData shield = GetMarkedShield(player.GetInventory());
        if (shield != null)
            player.EquipItem(shield);
    }

    internal static void OnWeaponUnequipped(Player player, ItemDrop.ItemData weapon)
    {
        if (!Plugin.AutoEquip.Value || !Plugin.AutoUnequip.Value || !IsEligibleWeapon(weapon))
            return;

        ItemDrop.ItemData left = player.m_leftItem;
        if (left == null || left != GetMarkedShield(player.GetInventory()))
            return;

        player.UnequipItem(left);
    }

    // ---------------------------------------------------------------- inventory clicks

    internal static void OnMiddleClick(Player player, ItemDrop.ItemData item)
    {
        if (player == null || item == null)
            return;

        if (IsOneHanded(item))
        {
            if (Plugin.WeaponExclusion.Value)
                ToggleExcluded(item);
            return;
        }

        if (!IsShield(item) || !Plugin.AutoEquip.Value)
            return;

        Inventory inventory = player.GetInventory();

        if (GetMarkedShield(inventory) == item)
        {
            // Unmark only. Whatever is in your hands stays there.
            ClearShieldMarks(inventory);
            return;
        }

        Mark(inventory, item);

        ItemDrop.ItemData left = player.m_leftItem;
        bool holdingOtherShield = IsShield(left) && left != item;
        bool readyForShield = left == null && IsEligibleWeapon(player.m_rightItem);

        if (holdingOtherShield || readyForShield)
            player.EquipItem(item);
    }

    private static void Mark(Inventory inventory, ItemDrop.ItemData shield)
    {
        ClearShieldMarks(inventory);
        Data(shield)[ShieldKey] = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
    }

    private static void ClearShieldMarks(Inventory inventory)
    {
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            item.m_customData?.Remove(ShieldKey);
    }

    private static void ToggleExcluded(ItemDrop.ItemData weapon)
    {
        Dictionary<string, string> data = Data(weapon);
        if (!data.Remove(ExcludedKey))
            data[ExcludedKey] = "1";
    }

    private static Dictionary<string, string> Data(ItemDrop.ItemData item) =>
        item.m_customData ??= new Dictionary<string, string>();
}
