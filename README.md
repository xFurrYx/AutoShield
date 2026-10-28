# AutoShield

Mark a shield once. Draw any one-handed weapon and the shield comes up with it.

Client-side only. No telemetry, no network calls, no popups, no banners. Only depends on BepInEx.

## Usage

Everything happens with a **middle-click in your inventory**:

| Middle-click on… | Result |
|---|---|
| an unmarked shield | marks it as your auto-shield (shield badge). If you're holding a one-handed weapon with a free off hand, or holding a different shield, it's equipped right away. |
| your marked shield | unmarks it. Whatever you're holding stays in your hands. |
| a one-handed weapon | toggles an X badge: that weapon never pulls the shield out (handy for a woodcutting axe or a knife). |

After that:

- Equip a one-handed weapon with your off hand free → the marked shield is equipped.
- Switch between one-handed weapons → the shield stays up.
- Unequip the weapon or put it away → the shield goes away with it. You can turn this off.
- Two-handed weapons, bows, torches and tools behave like vanilla.

The marks are stored on the items, so they follow the shield or weapon through inventory moves, chests, death and tombstone recovery, and logouts. If an older marked shield comes back out of a chest, the one you marked most recently wins.

## Configuration

`BepInEx/config/bladeoffurry.autoshield.cfg`, or in game through BepInEx Configuration Manager:

| Setting | Default | |
|---|---|---|
| Enable Auto Shield | `true` | master switch for marking and auto-equip |
| Enable Auto Unequip | `true` | put the shield away along with the weapon |
| Enable Weapon Exclusion | `true` | allow X-badge exclusions on weapons |

## Installation

Install with a mod manager, or copy `plugins/AutoShield.dll` into `Valheim/BepInEx/plugins/`. The only requirement is BepInExPack_Valheim.

**Client only.** Dedicated servers don't need it. If it's installed on one anyway, it detects the headless server and does nothing. Other players don't need it either.

## Compatibility

- Doesn't replace any game UI. It only adds two small badges and a middle-click handler to inventory slots.
- Works alongside inventory, backpack and quick-slot mods. Grids from other mods don't get badges, only the main player inventory does.
- Don't run it next to the original ShieldMeBruh. Both would try to equip shields.
- Marking needs a mouse (middle-click). Gamepad-only players can't mark items.

## Building

Requirements: .NET SDK 6 or newer, and publicized Valheim assemblies.

```sh
cp Environment.props.example Environment.props   # edit the paths
dotnet build -c Release
```

`Release` also writes an upload-ready `dist/AutoShield-<version>.zip` (Hexium layout: `manifest.json`, `icon.png` and `README.md` at the zip root, the DLL in `plugins/`).

## Credits and license

Shield and exclusion badges based on ["Shield" from SVG Repo](https://www.svgrepo.com/svg/119525/shield) (CC0).

MIT. This is a fork of [ShieldMeBruh](https://github.com/Vapok/ShieldMeBruh) by Vapok. The gameplay idea and the original implementation are his. This fork keeps only the shield behaviour and rewrites the rest.
