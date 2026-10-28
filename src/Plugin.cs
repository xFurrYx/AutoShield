// AutoShield — mark a shield, draw a one-handed weapon, the shield comes with it.
// Fork of ShieldMeBruh by Vapok (MIT). See LICENSE.md.
//
// Design notes:
//  * Client-only. On a dedicated server the plugin loads, logs one line and does nothing.
//  * No network calls, no telemetry, no UI besides two tiny inventory badges.
//  * Depends on BepInEx alone (no Jötunn, no YamlDotNet, no shared libraries).
//  * Marks are stored on the items themselves (ItemData.m_customData), so they follow
//    the item through inventory moves, chests, death/tombstones and save files without
//    any slot-tracking code.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutoShield;

[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "bladeoffurry.autoshield";
    public const string Name = "AutoShield";
    public const string Version = "3.0.0";

    internal static ManualLogSource Log { get; private set; }

    internal static ConfigEntry<bool> AutoEquip { get; private set; }
    internal static ConfigEntry<bool> AutoUnequip { get; private set; }
    internal static ConfigEntry<bool> WeaponExclusion { get; private set; }

    private Harmony _harmony;

    private void Awake()
    {
        Log = Logger;

        AutoEquip = Config.Bind("General", "Enable Auto Shield", true,
            "Equip your marked shield whenever you equip a one-handed weapon.");
        AutoUnequip = Config.Bind("General", "Enable Auto Unequip", true,
            "Put the marked shield away when the one-handed weapon it came with is unequipped.");
        WeaponExclusion = Config.Bind("General", "Enable Weapon Exclusion", true,
            "Allow middle-clicking a one-handed weapon to exclude it from auto-shield (X badge).");

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Log.LogInfo("Headless/dedicated server detected. AutoShield is client-side only; nothing patched.");
            return;
        }

        _harmony = new Harmony(Guid);
        _harmony.PatchAll(typeof(Plugin).Assembly);
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
