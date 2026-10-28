using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AutoShield;

/// <summary>
/// Hooks middle-click on the player's inventory slots and draws the two badges.
/// </summary>
internal static class InventoryUi
{
    private static InventoryElement _hookedFirst;
    private static int _hookedCount = -1;
    private static readonly Dictionary<InventoryElement, SlotBadges> Badges = new();

    private static Sprite _shieldSprite;
    private static Sprite _excludedSprite;
    private static bool _reported;

    internal static Sprite ShieldSprite => _shieldSprite ??= LoadSprite("shield.png");
    internal static Sprite ExcludedSprite => _excludedSprite ??= LoadSprite("excluded.png");

    internal static void OnGridUpdated(InventoryGrid grid)
    {
        InventoryGui gui = InventoryGui.instance;
        Player player = Player.m_localPlayer;
        if (gui == null || player == null || grid == null || grid != gui.m_playerGrid)
            return;

        Inventory inventory = grid.m_inventory;
        IEnumerable<InventoryElement> elements = grid.m_elements;
        if (inventory == null || inventory != player.GetInventory() || elements == null)
            return;

        // The grid rebuilds its slot objects when the inventory size changes
        // (and some inventory-extension mods do it on their own schedule).
        int count = 0;
        InventoryElement first = null;
        foreach (InventoryElement element in elements)
        {
            if (count++ == 0)
                first = element;
        }

        if (count == 0)
            return;

        if (count != _hookedCount || first != _hookedFirst)
        {
            Rehook(elements);
            _hookedCount = count;
            _hookedFirst = first;
        }

        ItemDrop.ItemData marked = Plugin.AutoEquip.Value ? ShieldLogic.GetMarkedShield(inventory) : null;

        foreach (InventoryElement element in elements)
        {
            if (element == null)
                continue;

            ItemDrop.ItemData item = inventory.GetItemAt(element.Position.x, element.Position.y);
            bool showShield = marked != null && item == marked;
            bool showExcluded = ShieldLogic.IsExcluded(item);

            if (!Badges.TryGetValue(element, out SlotBadges badges) || badges == null)
            {
                if (!showShield && !showExcluded)
                    continue; // nothing to draw, don't create anything yet

                badges = element.GetComponent<SlotBadges>();
                if (badges == null)
                    badges = element.gameObject.AddComponent<SlotBadges>();
                badges.Init(element);
                Badges[element] = badges;
            }

            badges.Show(showShield, showExcluded);
        }
    }

    private static void Rehook(IEnumerable<InventoryElement> elements)
    {
        Badges.Clear();

        foreach (InventoryElement element in elements)
        {
            if (element == null)
                continue;

            UIInputHandler handler = element.gameObject.GetComponentInChildren<UIInputHandler>();
            if (handler == null)
                continue;

            handler.m_onMiddleDown -= OnMiddleClick; // never double-subscribe
            handler.m_onMiddleDown += OnMiddleClick;
        }
    }

    private static void OnMiddleClick(UIInputHandler handler)
    {
        InventoryGui gui = InventoryGui.instance;
        Player player = Player.m_localPlayer;
        if (gui == null || player == null || handler == null)
            return;

        InventoryGrid grid = gui.m_playerGrid;
        if (grid == null || grid.m_inventory == null)
            return;

        Vector2i pos = grid.GetButtonPos(handler.gameObject);
        ShieldLogic.OnMiddleClick(player, grid.m_inventory.GetItemAt(pos.x, pos.y));
    }

    internal static void ReportOnce(Exception e)
    {
        if (_reported)
            return;
        _reported = true;
        Plugin.Log.LogWarning($"Inventory badge update failed (further errors suppressed): {e}");
    }

    private static Sprite LoadSprite(string file)
    {
        string resource = $"AutoShield.Resources.{file}";
        using Stream stream = typeof(InventoryUi).Assembly.GetManifestResourceStream(resource);
        if (stream == null)
        {
            Plugin.Log.LogError($"Missing embedded resource {resource}");
            return null;
        }

        byte[] bytes = new byte[stream.Length];
        int read = 0;
        while (read < bytes.Length)
        {
            int n = stream.Read(bytes, read, bytes.Length - read);
            if (n <= 0) break;
            read += n;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        if (!texture.LoadImage(bytes))
        {
            Plugin.Log.LogError($"Could not decode {resource}");
            return null;
        }

        BleedIntoTransparent(texture);

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        // Slightly prefer the sharper mip level; badges are drawn at ~1/4 of their source size.
        texture.mipMapBias = -0.5f;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Fully transparent pixels still carry a colour (image editors usually store white).
    /// Texture filtering and mipmaps blend that hidden colour into the visible edge, which
    /// shows up as a light fringe. Copy the nearest visible colour outwards instead, then
    /// rebuild the mipmaps. Alpha is left untouched, so the image looks the same.
    /// </summary>
    private static void BleedIntoTransparent(Texture2D texture)
    {
        const byte Visible = 8;
        int w = texture.width, h = texture.height;
        Color32[] px = texture.GetPixels32();
        bool[] known = new bool[px.Length];
        long sr = 0, sg = 0, sb = 0, count = 0;

        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a <= Visible)
                continue;
            known[i] = true;
            sr += px[i].r; sg += px[i].g; sb += px[i].b; count++;
        }

        if (count == 0 || count == px.Length)
            return;

        // Grow the visible colours outwards a ring at a time.
        var next = new List<int>();
        for (int pass = 0; pass < 32; pass++)
        {
            next.Clear();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (known[i])
                    continue;

                int r = 0, g = 0, b = 0, n = 0;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= w || ny >= h)
                        continue;
                    int j = ny * w + nx;
                    if (!known[j])
                        continue;
                    r += px[j].r; g += px[j].g; b += px[j].b; n++;
                }

                if (n == 0)
                    continue;
                px[i] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), px[i].a);
                next.Add(i);
            }

            if (next.Count == 0)
                break;
            foreach (int i in next)
                known[i] = true;
        }

        // Anything still untouched (far from all visible pixels) gets the average colour.
        var average = new Color32((byte)(sr / count), (byte)(sg / count), (byte)(sb / count), 0);
        for (int i = 0; i < px.Length; i++)
        {
            if (!known[i])
                px[i] = new Color32(average.r, average.g, average.b, px[i].a);
        }

        texture.SetPixels32(px);
        texture.Apply(updateMipmaps: true);
    }
}

/// <summary>
/// Lives on an inventory slot object and owns that slot's badge images.
/// Destroyed together with the slot, so nothing leaks when the grid rebuilds.
/// </summary>
internal sealed class SlotBadges : MonoBehaviour
{
    private InventoryElement _element;
    private Image _shield;
    private Image _excluded;

    internal void Init(InventoryElement element)
    {
        _element = element;
    }

    internal void Show(bool shield, bool excluded)
    {
        Apply(ref _shield, shield, "AutoShield_ShieldBadge", InventoryUi.ShieldSprite);
        Apply(ref _excluded, excluded, "AutoShield_ExcludedBadge", InventoryUi.ExcludedSprite);
    }

    private void Apply(ref Image image, bool visible, string name, Sprite sprite)
    {
        if (image == null)
        {
            if (!visible || sprite == null)
                return;
            image = CreateBadge(name, sprite);
            if (image == null)
                return;
        }

        if (image.enabled != visible)
            image.enabled = visible;

        // Keep the badge drawn above the icon, durability bar and stack text.
        if (visible && image.transform.GetSiblingIndex() != image.transform.parent.childCount - 1)
            image.transform.SetAsLastSibling();
    }

    private Image CreateBadge(string name, Sprite sprite)
    {
        if (_element == null || _element.m_icon == null)
            return null;

        Transform parent = _element.m_icon.transform.parent;
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = _element.m_icon.gameObject.layer;
        go.transform.SetParent(parent, false);

        // Lower-left quarter of the slot, just above the durability bar.
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.04f, 0.14f);
        rect.anchorMax = new Vector2(0.46f, 0.56f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false; // clicks and drags must still reach the slot
        image.enabled = false;
        return image;
    }

    private void OnDestroy()
    {
        if (_shield != null) Object.Destroy(_shield.gameObject);
        if (_excluded != null) Object.Destroy(_excluded.gameObject);
    }
}
