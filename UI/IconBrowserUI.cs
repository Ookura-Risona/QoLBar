using System;
using System.Numerics;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using Dalamud.Interface.Utility;

namespace QoLBar;

public static class IconBrowserUI
{
    public static bool iconBrowserOpen = false;
    public static bool doPasteIcon = false;
    public static int pasteIcon = 0;

    private static bool _tabExists = false;
    private static int _i, _columns;
    private static string _name;
    private static float _iconSize;
    private static string _tooltip;
    private static bool _useLowQuality = false;
    private static List<(int, int)> _iconList;
    private static bool _displayOutsideMain = true;

    private const int iconMax = 250_000;
    private static HashSet<int> _iconExistsCache;
    private static readonly Dictionary<string, List<int>> _iconCache = new();

    public static void ToggleIconBrowser() => iconBrowserOpen = !iconBrowserOpen;

    public static void Draw()
    {
        if (!ImGuiEx.SetBoolOnGameFocus(ref _displayOutsideMain)) return;

        if (!iconBrowserOpen) { doPasteIcon = false; return; }

        var iconSize = 48 * ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSizeConstraints(new Vector2((iconSize + ImGui.GetStyle().ItemSpacing.X) * 11 + ImGui.GetStyle().WindowPadding.X * 2 + 8), ImGuiHelpers.MainViewport.Size); // whyyyyyyyyyyyyyyyyyyyy
        ImGui.Begin("图标浏览器", ref iconBrowserOpen);

        ImGuiEx.ShouldDrawInViewport(out _displayOutsideMain);

        if (ImGuiEx.AddHeaderIconButton("RebuildIconCache", TextureDictionary.FrameIconID + 105, 1.0f, Vector2.Zero, 0, 0xFFFFFFFF, "nhg"))
            BuildCache(true);

        if (ImGui.BeginTabBar("Icon Tabs", ImGuiTabBarFlags.NoTooltip))
        {
            BeginIconList(" ★ ", iconSize);
            AddIcons(0, 100, "系统");
            AddIcons(62_000, 62_600, "职业/特职");
            AddIcons(62_800, 62_900, "装备方案");
            AddIcons(66_000, 66_400, "宏");
            AddIcons(90_000, 100_000, "部队徽章/符号");
            AddIcons(114_000, 114_100, "New Game+");
            AddIcons(TextureDictionary.FrameIconID, TextureDictionary.FrameIconID + 3000, "额外");
            EndIconList();

            BeginIconList("自定义", iconSize);
            ImGuiEx.SetItemTooltip("将图片放入 \"%%AppData%%\\XIVLauncher\\pluginConfigs\\QoLBar\\icons\" 目录\n" +
                                   "即可加载为可用图标，文件名必须为 \"#.img\" 格式（# > 0）。\n" +
                                   "例如：\"1.jpg\" \"2.png\" \"3.png\" \"732487.jpg\" 等。");
            if (_tabExists)
            {
                if (ImGui.Button("刷新自定义图标"))
                    QoLBar.Plugin.AddUserIcons();
                ImGui.SameLine();
                if (ImGui.Button("打开图标文件夹"))
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = QoLBar.Config.GetPluginIconPath(),
                        UseShellExecute = true
                    });
            }
            foreach (var kv in QoLBar.GetUserIcons())
                AddIcons(kv.Key, kv.Key + 1);
            _tooltip = "";
            EndIconList();

            BeginIconList("杂项1", iconSize);
            AddIcons(60_000, 61_000, "用户界面");
            AddIcons(61_200, 61_250, "标记");
            AddIcons(61_290, 61_390, "标记2");
            AddIcons(61_390, 62_000, "用户界面2");
            AddIcons(62_600, 62_620, "高品质部队旗帜");
            AddIcons(63_900, 64_000, "地图标记");
            AddIcons(64_500, 64_600, "印章");
            AddIcons(65_000, 65_900, "货币");
            AddIcons(76_300, 78_000, "集体姿势");
            AddIcons(180_000, 180_060, "印章/陆行鸟竞赛");
            EndIconList();

            BeginIconList("杂项2", iconSize);
            AddIcons(62_900, 63_200, "成就/狩猎笔记");
            AddIcons(65_900, 66_000, "钓鱼");
            AddIcons(66_400, 66_500, "标签");
            AddIcons(67_000, 68_000, "时尚品鉴");
            AddIcons(71_000, 71_500, "任务");
            AddIcons(72_000, 72_500, "青魔法师界面");
            AddIcons(72_500, 76_000, "博兹雅界面");
            AddIcons(76_000, 76_200, "麻将");
            AddIcons(80_000, 80_200, "任务日志");
            AddIcons(80_730, 81_000, "魂武日志");
            AddIcons(83_000, 84_000, "部队等级");
            EndIconList();

            BeginIconList("技能", iconSize);
            AddIcons(100, 4_000, "职业/特职");
            AddIcons(5_100, 8_000, "特性");
            AddIcons(8_000, 9_000, "时装");
            AddIcons(9_000, 10_000, "PvP");
            AddIcons(61_100, 61_200, "活动");
            AddIcons(61_250, 61_290, "任务/讨伐");
            AddIcons(64_000, 64_200, "情感动作");
            AddIcons(64_200, 64_325, "部队");
            AddIcons(64_325, 64_500, "情感动作2");
            AddIcons(64_600, 64_800, "优雷卡");
            AddIcons(64_800, 65_000, "NPC");
            AddIcons(70_000, 70_200, "陆行鸟竞赛");
            EndIconList();

            BeginIconList("坐骑与宠物", iconSize);
            AddIcons(4_000, 4_400, "坐骑");
            AddIcons(4_400, 5_100, "宠物");
            AddIcons(59_000, 59_400, "坐骑...再次?");
            AddIcons(59_400, 60_000, "宠物物品");
            AddIcons(68_000, 68_400, "坐骑日志");
            AddIcons(68_400, 69_000, "宠物日志");
            EndIconList();

            BeginIconList("物品", iconSize);
            AddIcons(20_000, 30_000, "常规");
            AddIcons(50_000, 54_400, "房屋");
            AddIcons(58_000, 59_000, "时装");
            EndIconList();

            BeginIconList("装备", iconSize);
            AddIcons(30_000, 50_000, "装备");
            AddIcons(54_400, 58_000, "特殊装备");
            EndIconList();

            BeginIconList("美术素材", iconSize);
            AddIcons(130_000, 142_000);
            EndIconList();

            BeginIconList("状态效果", iconSize);
            AddIcons(10_000, 20_000);
            EndIconList();

            BeginIconList("Garbage", iconSize, true);
            AddIcons(61_000, 61_100, "启动标志");
            AddIcons(62_620, 62_800, "世界地图");
            AddIcons(63_200, 63_900, "区域地图");
            AddIcons(66_500, 67_000, "园艺日志");
            AddIcons(69_000, 70_000, "坐骑/宠物脚印");
            AddIcons(70_200, 71_000, "制作/采集日志");
            AddIcons(76_200, 76_300, "粉丝节");
            AddIcons(78_000, 80_000, "钓鱼日志");
            AddIcons(80_200, 80_730, "笔记");
            AddIcons(81_000, 82_060, "笔记2");
            AddIcons(84_000, 85_000, "狩猎");
            AddIcons(85_000, 90_000, "用户界面3");
            AddIcons(150_000, 170_000, "教程");
            //AddIcons(170_000, 180_000, "Placeholder"); // TODO: 170k - 180k are blank placeholder files, check if they get used in EW
            EndIconList();

            BeginIconList("剧透内容1", iconSize, true);
            AddIcons(82_100, 83_000, "幻卡");
            AddIcons(82_060, 82_100, "亲信战友");
            AddIcons(120_000, 130_000, "弹出文本");
            AddIcons(142_000, 150_000, "日文弹出文本");
            AddIcons(180_060, 180_100, "亲信战友名称");
            AddIcons(181_000, 181_500, "首领称号");
            AddIcons(181_500, iconMax, "占位符");
            EndIconList();

            BeginIconList("剧透内容2", iconSize, true);
            AddIcons(71_500, 72_000, "制作名单");
            AddIcons(100_000, 114_000, "任务图片");
            AddIcons(114_100, 120_000, "New Game+");
            EndIconList();

            ImGui.EndTabBar();
        }
        ImGui.End();

        if (iconBrowserOpen) return;
        QoLBar.CleanTextures(false);
    }

    private static bool BeginIconList(string name, float iconSize, bool useLowQuality = false)
    {
        _tooltip = "包含:";
        if (ImGui.BeginTabItem(name))
        {
            _name = name;
            _tabExists = true;
            _i = 0;
            _columns = (int)((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / (iconSize + ImGui.GetStyle().ItemSpacing.X)); // WHYYYYYYYYYYYYYYYYYYYYY
            _iconSize = iconSize;
            _iconList = new List<(int, int)>();

            if (useLowQuality)
                _useLowQuality = true;
        }
        else
        {
            _tabExists = false;
        }

        return _tabExists;
    }

    private static void EndIconList()
    {
        if (_tabExists)
        {
            if (!string.IsNullOrEmpty(_tooltip))
                ImGuiEx.SetItemTooltip(_tooltip);
            BuildTabCache();
            DrawIconList();
            ImGui.EndTabItem();
        }
        else if (!string.IsNullOrEmpty(_tooltip))
        {
            ImGuiEx.SetItemTooltip(_tooltip);
        }
    }

    private static void AddIcons(int start, int end, string desc = "")
    {
        _tooltip += $"\n\t{start} -> {end - 1}{(!string.IsNullOrEmpty(desc) ? ("   " + desc) : "")}";
        if (_tabExists)
            _iconList.Add((start, end));
    }

    private static void DrawIconList()
    {
        if (_columns <= 0) return;

        ImGui.BeginChild($"{_name}##IconList");

        var cache = _iconCache[_name];

        ImGuiListClipperPtr clipper;
        unsafe { clipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper()); }
        clipper.Begin((cache.Count - 1) / _columns + 1, _iconSize + ImGui.GetStyle().ItemSpacing.Y);

        var iconSize = new Vector2(_iconSize);
        var settings = new ImGuiEx.IconSettings { size = iconSize };
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                var start = row * _columns;
                var end = Math.Min(start + _columns, cache.Count);
                for (int i = start; i < end; i++)
                {
                    var icon = cache[i];
                    ShortcutUI.DrawIcon(icon, settings, _useLowQuality ? "ln" : "n");
                    if (ImGui.IsItemClicked())
                    {
                        doPasteIcon = true;
                        pasteIcon = icon;
                        ImGui.SetClipboardText($"::{icon}");
                    }

                    if (ImGui.IsItemHovered())
                    {
                        var tex = QoLBar.TextureDictionary[icon];
                        if (!ImGui.IsMouseDown(ImGuiMouseButton.Right))
                            ImGui.SetTooltip($"{icon}");
                        else if (tex != null && tex.ImGuiHandle != nint.Zero)
                        {
                            ImGui.BeginTooltip();
                            ImGui.Image(tex.ImGuiHandle, new Vector2(700 * ImGuiHelpers.GlobalScale));
                            ImGui.EndTooltip();
                        }
                    }
                    if (_i % _columns != _columns - 1)
                        ImGui.SameLine();
                    _i++;
                }
            }
        }

        clipper.Destroy();

        ImGui.EndChild();
    }

    private static void BuildTabCache()
    {
        if (_iconCache.ContainsKey(_name)) return;
        DalamudApi.LogInfo($"为标签页 \"{_name}\" 构建图标浏览器缓存");

        var cache = _iconCache[_name] = new();
        foreach (var (start, end) in _iconList)
        {
            for (int icon = start; icon < end; icon++)
            {
                if (_iconExistsCache.Contains(icon))
                    cache.Add(icon);
            }
        }

        DalamudApi.LogInfo($"标签页缓存构建完成！找到 {cache.Count} 个图标。");
    }

    public static void BuildCache(bool rebuild)
    {
        DalamudApi.LogInfo("构建图标浏览器缓存");

        _iconCache.Clear();
        _iconExistsCache = !rebuild ? QoLBar.Config.LoadIconCache() ?? new() : new();

        if (_iconExistsCache.Count == 0)
        {
            for (int i = 0; i < iconMax; i++)
            {
                if (TextureDictionary.IconExists((uint)i))
                    _iconExistsCache.Add(i);
            }

            _iconExistsCache.Remove(125052); // Remove broken image (TextureFormat R8G8B8X8 is not supported for image conversion)

            QoLBar.Config.SaveIconCache(_iconExistsCache);
        }

        foreach (var kv in QoLBar.textureDictionaryLR.GetUserIcons())
            _iconExistsCache.Add(kv.Key);

        foreach (var kv in QoLBar.textureDictionaryLR.GetTextureOverrides())
            _iconExistsCache.Add(kv.Key);

        DalamudApi.LogInfo($"缓存构建完成！找到 {_iconExistsCache.Count} 个图标。");
    }
}