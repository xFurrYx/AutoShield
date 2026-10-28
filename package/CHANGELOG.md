# Changelog

## 3.0.0
First release. A rewrite of [ShieldMeBruh](https://github.com/Vapok/ShieldMeBruh) 2.1.1 (MIT) for Valheim 1.0, keeping only the shield behaviour: no splash popup, no telemetry, no promo links, no closed-source shared library. Changes compared with ShieldMeBruh:

- **Dependencies**: BepInEx only. Jötunn and YamlDotNet are gone.
- **Marks live on the items** (`m_customData`) instead of a remembered inventory slot. Moving, swapping, storing, dying and logging out can no longer lose or misplace the mark. About 330 lines of slot-tracking and death/tombstone patches were removed.
- **Fixed**: moving an *unequipped* one-handed weapon into a chest no longer unequips your shield.
- **Fixed**: switching from one one-handed weapon to another keeps the shield up. Before, it was lowered and raised again.
- **Fixed**: loading a character no longer adds a shield you didn't have equipped. Spawning no longer strips the weapons out of your hands.
- **Changed**: marking a shield while holding a one-handed weapon with a free off hand equips it immediately.
- **Changed**: unmarking a shield no longer unequips it.
- New badge art (based on a CC0 shield icon from SVG Repo) and a new mod icon. Upstream branding and stock art were removed.
- Badges never block clicks or drags on the slot, and a badge error can't break the inventory screen.
