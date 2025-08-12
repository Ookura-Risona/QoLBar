using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Newtonsoft.Json;
using ImGuiNET;
using Dalamud.Interface.Utility;

namespace QoLBar;

public class PluginUI : IDisposable
{
    public bool IsVisible => true;

    public readonly List<BarUI> bars;

#if DEBUG
    public bool configOpen = true;
#else
        public bool configOpen = false;
#endif
    public void ToggleConfig() => configOpen = !configOpen;

    private bool lastConfigPopupOpen = false;
    private bool configPopupOpen = false;
    public bool IsConfigPopupOpen() => configPopupOpen || lastConfigPopupOpen;
    public void SetConfigPopupOpen() => configPopupOpen = true;

    public static readonly Vector2 defaultSpacing = new(8, 4);

    private bool _displayOutsideMain = true;

    public static Vector2 mousePos = ImGui.GetMousePos();

    public PluginUI()
    {
        bars = new List<BarUI>();
        for (int i = 0; i < QoLBar.Config.BarCfgs.Count; i++)
            bars.Add(new BarUI(i));

        if (!QoLBar.Config.FirstStart) return;

        AddBar(new BarCfg { Editing = true });
        AddDemoBar();
    }

    public void Reload()
    {
        Dispose();

        bars.Clear();
        for (int i = 0; i < QoLBar.Config.BarCfgs.Count; i++)
            bars.Add(new BarUI(i));
    }

    public void Draw()
    {
        if (!IsVisible) return;

        mousePos = ImGui.GetMousePos();

        IconBrowserUI.Draw();

        if (QoLBar.Config.AlwaysDisplayBars || QoLBar.IsLoggedIn())
        {
            foreach (var bar in bars)
                bar.Draw();
        }
        lastConfigPopupOpen = configPopupOpen;
        configPopupOpen = false;

        if (configOpen)
            DrawPluginConfig();

        PieUI.Draw();
        ImGuiEx.DoSlider();
    }

    private void DrawPluginConfig()
    {
        if (!ImGuiEx.SetBoolOnGameFocus(ref _displayOutsideMain)) return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(610, 650) * ImGuiHelpers.GlobalScale, ImGuiHelpers.MainViewport.Size);
        ImGui.Begin("QoL Bar 配置界面", ref configOpen);

        ImGuiEx.ShouldDrawInViewport(out _displayOutsideMain);

        if (ImGui.BeginTabBar("配置标签页"))
        {
            if (ImGui.BeginTabItem("快捷栏管理"))
            {
                DrawBarManager();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("条件设置"))
            {
                ConditionSetUI.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("备份管理"))
            {
                DrawBackupManager();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("设置"))
            {
                DrawSettingsMenu();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("调试"))
            {
                DrawDebugMenu();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void DrawBarManager()
    {
        Vector2 textsize = new(-1, 0);
        float textx = 0.0f;

        var letterButtonSize = new Vector2(ImGui.CalcTextSize("O").X + ImGui.GetStyle().FramePadding.X * 2, 0);

        ImGui.Columns(3, "QoL栏位列表", false);
        for (int i = 0; i < bars.Count; i++)
        {
            ImGui.PushID(i);

            var bar = QoLBar.Config.BarCfgs[i];

            ImGui.Text($"#{i + 1}");
            ImGui.SameLine();

            textx = ImGui.GetCursorPosX();

            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##Name", ref bar.Name, 32))
                QoLBar.Config.Save();

            textsize = ImGui.GetItemRectSize();

            ImGui.NextColumn();

            if (ImGui.Button("O", letterButtonSize))
                ImGui.OpenPopup($"BarConfig##{i}");
            ImGuiEx.SetItemTooltip("选项");
            bars[i].DrawConfig();
            ImGui.SameLine();
            ImGui.SetNextItemWidth(27 * ImGuiHelpers.GlobalScale);
            if (ImGui.Button(bar.Hidden ? "R" : "H", letterButtonSize))
                bars[i].IsHidden = !bars[i].IsHidden;
            ImGuiEx.SetItemTooltip(bar.Hidden ? "显示" : "隐藏");
            ImGui.SameLine();
            var preview = ((bar.ConditionSet >= 0) && (bar.ConditionSet < QoLBar.Config.CndSetCfgs.Count)) ? $"[{bar.ConditionSet + 1}] {QoLBar.Config.CndSetCfgs[bar.ConditionSet].Name}" : "条件设置";
            if (ImGui.BeginCombo("##Condition", preview))
            {
                if (ImGui.Selectable("无", bar.ConditionSet == -1))
                {
                    bar.ConditionSet = -1;
                    QoLBar.Config.Save();
                }
                for (int idx = 0; idx < QoLBar.Config.CndSetCfgs.Count; idx++)
                {
                    if (ImGui.Selectable($"[{idx + 1}] {QoLBar.Config.CndSetCfgs[idx].Name}", idx == bar.ConditionSet))
                    {
                        bar.ConditionSet = idx;
                        QoLBar.Config.Save();
                    }
                }
                ImGui.EndCombo();
            }
            ImGuiEx.SetItemTooltip("使用条件设置控制快捷栏的显示\n用于设置同时显示哪些快捷栏\n可在本窗口顶部的\"条件设置\"标签页创建");

            ImGui.NextColumn();

            if (ImGui.Button("↑"))
                ShiftBar(i, false);
            ImGui.SameLine();
            if (ImGui.Button("↓"))
                ShiftBar(i, true);
            ImGui.SameLine();
            if (ImGui.Button("导出"))
                ImGui.SetClipboardText(ExportBar(i, false));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("导出精简配置到剪贴板（可能随更新变化）\n右键点击导出完整配置（更长字符串，不会变化）");

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    ImGui.SetClipboardText(ExportBar(i, true));
            }

            if (bars.Count > 1)
            {
                ImGui.SameLine();
                if (ImGui.Button(QoLBar.Config.ExportOnDelete ? "剪切" : "删除"))
                    ConfigEditorUI.DisplayRightClickDeleteMessage();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"右键点击此按钮删除快捷栏 #{i + 1}!" +
                                     (QoLBar.Config.ExportOnDelete ? "\n删除前会将快捷栏导出到剪贴板" : ""));

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                    {
                        ImGui.SetWindowFocus(null); // Kill focus to prevent ImGui from overwriting text box on deletes
                        RemoveBar(i);
                    }
                }
            }

            ImGui.Separator();
            ImGui.NextColumn();

            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.SameLine(textx);
        if (ImGui.Button("+", textsize))
            AddBar(new BarCfg { Editing = true });
        ImGui.NextColumn();
        ImGui.NextColumn();
        if (ImGui.Button("导入", textsize))
            ImportBar(ImGuiEx.TryGetClipboardText());
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("从剪贴板导入快捷栏，或导入单个快捷方式创建新快捷栏\n右键点击添加演示栏（展示各种功能）");

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                AddDemoBar();
        }

        ImGui.Columns(1);
    }

    private void DrawSettingsMenu()
    {
        var halfWidth = ImGui.GetWindowWidth() / 2;
        var thirdWidth = ImGui.GetWindowWidth() / 3;
        var quarterWidth = ImGui.GetWindowWidth() / 4;
        if (ImGui.Checkbox("删除时导出", ref QoLBar.Config.ExportOnDelete))
            QoLBar.Config.Save();
        ImGui.SameLine(halfWidth);
        if (ImGui.Checkbox("始终显示栏位", ref QoLBar.Config.AlwaysDisplayBars))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("即使未登录游戏也保持快捷栏可见");

        if (ImGui.Checkbox("图标使用热键栏边框", ref QoLBar.Config.UseIconFrame))
            QoLBar.Config.Save();
        ImGui.SameLine(halfWidth);
        var _ = QoLBar.Config.UseHRIcons;
        if (ImGui.Checkbox("使用高清图标", ref _))
        {
            QoLBar.Config.UseHRIcons = _;
            QoLBar.Config.Save();
        }
        ImGuiEx.SetItemTooltip("加载高清图标。请注意：如果打开\"剧透内容\"标签页，\n图标浏览器可能占用高达5GB内存直到关闭！");

        if (ImGui.Checkbox("禁用条件缓存", ref QoLBar.Config.NoConditionCache))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("禁用条件检查的100ms延迟，会增加CPU负载");
        ImGui.SameLine(halfWidth);
        ImGui.SetNextItemWidth(quarterWidth);
        if (ImGui.InputFloat("快捷栏字体大小", ref QoLBar.Config.FontSize, 1, 8, "%.f", ImGuiInputTextFlags.EnterReturnsTrue))
        {
            QoLBar.Config.FontSize = Math.Min(Math.Max(QoLBar.Config.FontSize, 1), QoLBar.MaxFontSize);
            QoLBar.Config.Save();
            QoLBar.SetupFont();
        }
        ImGuiEx.SetItemTooltip($"默认: {QoLBar.DefaultFontSize}");

        ImGui.SetNextItemWidth(quarterWidth);
        if (ImGui.InputInt("备份间隔", ref QoLBar.Config.BackupTimer))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("距离上次保存后执行备份的分钟数。设为0禁用");

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextUnformatted("环形菜单设置");
        ImGui.SetNextItemWidth(quarterWidth);
        if (ImGui.DragInt("透明度", ref QoLBar.Config.PieOpacity, 0.2f, 0, 255))
            QoLBar.Config.Save();
        ImGui.SameLine(halfWidth);
        if (ImGui.Checkbox("备用角度", ref QoLBar.Config.PieAlternateAngle))
            QoLBar.Config.Save();
        /*ImGui.SetNextItemWidth(quarterWidth);
        var offset = (float)(QoLBar.Config.PieAngleOffset / Math.PI * 180);
        if (ImGui.DragFloat("Pie Angle Offset", ref offset, 0.1f, -10, 370, "%.1f"))
        {
            offset = (offset % 360 + 360) % 360;
            QoLBar.Config.PieAngleOffset = (float)(offset / 180f * Math.PI);
            QoLBar.Config.Save();
        }*/

        if (ImGui.Checkbox("居中显示", ref QoLBar.Config.PiesAlwaysCenter))
        {
            if (!QoLBar.Config.PiesAlwaysCenter)
            {
                QoLBar.Config.PiesMoveMouse = false;
                QoLBar.Config.PiesReturnMouse = false;
                QoLBar.Config.PiesReadjustMouse = false;
            }
            QoLBar.Config.Save();
        }
        ImGui.SameLine(halfWidth);
        if (QoLBar.Config.PiesAlwaysCenter && ImGui.Checkbox("打开时鼠标居中", ref QoLBar.Config.PiesMoveMouse))
        {
            if (!QoLBar.Config.PiesMoveMouse)
            {
                QoLBar.Config.PiesReturnMouse = false;
                QoLBar.Config.PiesReadjustMouse = false;
            }
            QoLBar.Config.Save();
        }

        if (QoLBar.Config.PiesMoveMouse && ImGui.Checkbox("关闭时鼠标归位", ref QoLBar.Config.PiesReturnMouse))
        {
            if (!QoLBar.Config.PiesReturnMouse)
                QoLBar.Config.PiesReadjustMouse = false;
            QoLBar.Config.Save();
        }
        ImGui.SameLine(halfWidth);
        if (QoLBar.Config.PiesReturnMouse && ImGui.Checkbox("校正原鼠标位置", ref QoLBar.Config.PiesReadjustMouse))
            QoLBar.Config.Save();
        ImGui.SameLine();

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextUnformatted("选择退出Dalamud的UI隐藏设置");
        if (ImGui.Checkbox("游戏UI关闭时", ref QoLBar.Config.OptOutGameUIOffHide))
        {
            QoLBar.Config.Save();
            QoLBar.Plugin.CheckHideOptOuts();
        }
        ImGui.SameLine(thirdWidth);
        if (ImGui.Checkbox("过场动画中", ref QoLBar.Config.OptOutCutsceneHide))
        {
            QoLBar.Config.Save();
            QoLBar.Plugin.CheckHideOptOuts();
        }
        ImGui.SameLine(thirdWidth * 2);
        if (ImGui.Checkbox("在/gpose中", ref QoLBar.Config.OptOutGPoseHide))
        {
            QoLBar.Config.Save();
            QoLBar.Plugin.CheckHideOptOuts();
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextUnformatted("临时设置，启用风险自负");
        ImGui.Checkbox("允许导入条件", ref Importing.allowImportConditions);
        ImGui.SameLine(halfWidth);
        ImGui.Checkbox("允许导入热键", ref Importing.allowImportHotkeys);
        ImGui.Checkbox("允许导出敏感条件集", ref Importing.allowExportingSensitiveConditionSets);
        ImGuiEx.SetItemTooltip("允许导出包含个人信息（如角色ID）的条件集");
    }

    private void DrawBackupManager()
    {
        var path = QoLBar.Config.GetPluginBackupPath();
        var configFile = Configuration.ConfigFile;

        if (ImGui.Button("打开文件夹"))
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        ImGui.SameLine();
        if (ImGui.Button("创建备份"))
            BackupFile(configFile);

        ImGui.Separator();
        ImGui.Columns(3, "QoLBar备份", false);
        if (!string.IsNullOrEmpty(path))
        {
            var i = 0;
            var directory = new DirectoryInfo(path);
            foreach (var file in directory.GetFiles())
            {
                if (file.Extension == ".json")
                {
                    ImGui.PushID(i);

                    ImGui.TextUnformatted(file.Name);
                    ImGui.NextColumn();
                    ImGui.TextUnformatted(file.LastWriteTime.ToString());
                    ImGui.NextColumn();
                    ImGui.SmallButton("加载");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"双击此按钮将覆盖并\n使用 {file.Name} 重新加载当前配置");

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            QoLBar.Config.LoadConfig(file);
                    }

                    ImGui.SameLine();
                    if (ImGui.SmallButton("删除"))
                        ConfigEditorUI.DisplayRightClickDeleteMessage("双击右键删除！");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"双击右键删除 {file.Name}");

                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right))
                            DeleteFile(file);
                    }

                    ImGui.Separator();
                    ImGui.NextColumn();

                    ImGui.PopID();
                    i++;
                }
            }
        }
        ImGui.Columns(1);
    }

    private string debug_SerializedImport = string.Empty;
    private string debug_DeserializedImport = string.Empty;
    private unsafe void DrawDebugMenu()
    {
        ImGui.TextUnformatted("游戏数据指针");
        ImGui.Indent();
        ImGui.Columns(3, "调试指针", false);

        ImGui.TextUnformatted("UI模块（文本输入激活）");
        ImGui.NextColumn();
        ImGuiEx.TextCopyable($"{(nint)Game.uiModule:X}");
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{Game.IsGameTextInputActive}");
        ImGui.NextColumn();

        ImGui.TextUnformatted("Rapture Shell模块");
        ImGui.NextColumn();
        ImGuiEx.TextCopyable($"{(nint)Game.raptureShellModule:X}");
        ImGui.NextColumn();
        ImGui.NextColumn();

        ImGui.TextUnformatted("Rapture宏模块");
        ImGui.NextColumn();
        ImGuiEx.TextCopyable($"{(nint)Game.raptureMacroModule:X}");
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{Game.IsMacroRunning}");
        ImGui.NextColumn();

        ImGui.TextUnformatted("插件配置（HUD布局编号）");
        ImGui.NextColumn();
        ImGuiEx.TextCopyable($"{(nint)Game.addonConfig:X}");
        ImGui.NextColumn();
        ImGui.TextUnformatted($"{Game.CurrentHUDLayout}");
        ImGui.NextColumn();

        ImGui.TextUnformatted("物品上下文菜单代理");
        ImGui.NextColumn();
        ImGuiEx.TextCopyable($"{(nint)Game.agentInventoryContext:X}");
        ImGui.NextColumn();
        ImGui.NextColumn();

        ImGui.Columns(1);
        ImGui.Unindent();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.TreeNodeEx("导出编辑器", ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.NoTreePushOnOpen))
        {
            var available = ImGui.GetContentRegionAvail();
            ImGui.TextUnformatted("序列化字符串");
            ImGui.SetNextItemWidth(available.X);
            if (ImGui.InputText("##Serialized", ref debug_SerializedImport, 1000000, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.NoHorizontalScroll))
            {
                var import = Importing.TryImport(debug_SerializedImport);
                if (import.bar != null)
                    debug_DeserializedImport = JsonConvert.SerializeObject(import.bar, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
                else if (import.shortcut != null)
                    debug_DeserializedImport = JsonConvert.SerializeObject(import.shortcut, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });
            }

            ImGui.TextUnformatted("反序列化字符串");
            if (ImGui.InputTextMultiline("##Deserialized", ref debug_DeserializedImport, 1000000, new Vector2(available.X, available.Y / 2)))
            {
                try
                {
                    debug_SerializedImport = Importing.ExportBar(Importing.ImportObject<BarCfg>(Importing.CompressString(debug_DeserializedImport)), true);
                }
                catch
                {
                    try
                    {
                        debug_SerializedImport = Importing.ExportShortcut(Importing.ImportObject<ShCfg>(Importing.CompressString(debug_DeserializedImport)), true);
                    }
                    catch { }
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Keybind.DrawDebug();
    }

    private void AddBar(BarCfg bar)
    {
        QoLBar.Config.BarCfgs.Add(bar);
        bars.Add(new BarUI(bars.Count));
        QoLBar.Config.Save();
    }

    private void RemoveBar(int i)
    {
        if (QoLBar.Config.ExportOnDelete)
            ImGui.SetClipboardText(Importing.ExportBar(QoLBar.Config.BarCfgs[i], false));

        bars[i].Dispose();
        bars.RemoveAt(i);
        QoLBar.Config.BarCfgs.RemoveAt(i);
        QoLBar.Config.Save();
        RefreshBarIndexes();
    }

    private void ShiftBar(int i, bool increment)
    {
        if (!increment ? i > 0 : i < (bars.Count - 1))
        {
            var j = (increment ? i + 1 : i - 1);
            var b = bars[i];
            bars.RemoveAt(i);
            bars.Insert(j, b);

            var b2 = QoLBar.Config.BarCfgs[i];
            QoLBar.Config.BarCfgs.RemoveAt(i);
            QoLBar.Config.BarCfgs.Insert(j, b2);
            QoLBar.Config.Save();
            RefreshBarIndexes();
        }
    }

    public string ExportBar(int i, bool saveAllValues) => Importing.ExportBar(QoLBar.Config.BarCfgs[i], saveAllValues);

    public void ImportBar(string import)
    {
        var imports = Importing.TryImport(import, true);
        if (imports.bar != null)
            AddBar(imports.bar);
        else if (imports.shortcut != null)
        {
            var bar = new BarCfg { Editing = true };
            bar.ShortcutList.Add(imports.shortcut);
            AddBar(bar);
        }
    }

    public void AddDemoBar()
    {
        var prev = Importing.allowImportHotkeys;
        Importing.allowImportHotkeys = true;
        ImportBar("H4sIAAAAAAAEALVa/W7byBF/lTVVIFecLJOURH2gKJAoyTlFAwd2DodrnT9W5Ercmtxld5dWdIcDDu0D9QX6THmGzuySlGgrsS26QCKL5HK+d+Y3s/rV+4PZFsybe8zre8vQm//a3IGrvifgy+nrN+8vTuFC/9Wb/323QNcL8Dl59fIS" +
                  "Lo03D/tenL305kHf4xfwgj/w+/D/E1xeenO88uKrAh5M+6NPv/UP0HtHaE4oMeyzITE1bC3V1tEOvi7EQmZSscS+1et9dO8KsmQkrp78o9SGZPyGEbjPYyn25PXmo3A2mgXDyex4uT8opjXJGVlJRfIyM7zIGGFxKjWsiGHFGV6Qc5Zl8lq4C5" +
                  "MyxU687kyNtKxI0CdhnwwJF0SqhKkW66BmG9ZfhvA87+KtltaUKCoSmVtRWpx/liXZcHFS88XrTGo2GAycBOHREvR6VcQsMypuyLI0xjq3W/jpclkHX693Lm+Z4mJNZMEEatsnW1AAI4xmWpLCGmFhVEa+J1fgC9kE7A18DoNgPP567M7nke9P" +
                  "gzvxOJmG4/Ew8o9W5AcpIerl8qQh/AhCn76x6if4An8W7k/zDnw/9+ZGleygGG8+s7g0DAIy4bc8KWlGchorSXp+HSFn5GPKNcRHlhH29eUQO2e5f7xna0l0SjEhPEaKO0utBIH/DDLEkI1gn1jCdwSAf7T13HKFD3uRyfgGvm8oN7vtbFMK+R" +
                  "PeHAR/bucW8uXfv9cU7O1XW1JqDOZrD+96oLFJZWmAqyjzJVO74NYFi/lqSzYpj1NSUGU0kSskDGk1z2Gv26UbWWaJy62QhlQJuwJ1cFbjQhtGk0fmuAfiL/Tt8gsXb4/cyy7dk912/tGqjzd1LZ5VC6sGWJ9RvT0hf8F6oZmx2grqUuw1bNYA" +
                  "TAa5Tku4JdHGldkerFHz+SqY9noXkEMs0ZXiTKDdtKlDwN2q7hwZYsAmGvlB0Ou9piJmNekEL0guDe+SHVEHVOK9LIUhChzPjGl40Bgs5B5dVo+uvW6aBMMRaPKCrMgLQtW6zBkQ/26lwCF/bFWYl5Wbm0VryTQAALNhYG90HPgNI9b603oMPZ" +
                  "rLBCM8lRv7wJKAGEi4LjK6ZUkX6QVKH6L0oiW9kKRR4GjiGRIfIvGsRTwDTaAcyaxET3dikSKLCbJIWyxSvk6fi8caeYyQx7rFY63oVsc062YkhdTHSF21qCsGBV0zckZWGS86sTiF8Kzz9gYzbp27bSj1gio70qJgVBG+shmTJgmUFSlYK4QX" +
                  "KYtvdmG4VHKjmXqhIXoXliZEsKFLDNsMiAkXtRKp4Vt5FzUiwCABQqkKNlOxddDZZXgqbI7aIZMoGHfndgFbEBCjhsqCbgHcdFJxCf2h74+GwbATF3+MO+Tnqpr9ItErwlaiv3nz8eB4bDWfz/wwGO9o65SvjE0vShooNM6LOV3vg/twCmzCsO" +
                  "EzmY6Hs2n4GE2m0KDsuJXaMsBPuWa2BkEYVPqh6zZNW9FS9BR0m/Xxc9oIMRoPQY4IZHtYjGAchpiO30tXGw3UUV1HTCLLJTQ8EDExAuE44/ENdmSQY6kAHJdtQbgCcMYtzUqmK+Gme14IRla26ImOGEXg5CvZoJYEoQ/XTbxCZ+cHE0TStjOd" +
                  "OMb+YLyzymiKnMfBE4tTNAxGLoprfkA/cvRPg07x5T8FtR8mMYkg+yHaARxb0BgSnk0WGV8j/rCuw54NkofFMjZa61a7C9/OoluEtJ8Cd5Fv2y3pEBQkXAdkca+B8AIKOV6uM6oR2FU4VQnYHgDvdCqVAfANt8SKr5um/J8ys1Cw9t/0OYBqEE" +
                  "RtoApd07AmEPr9AN+Ir2AhhmH8Fl+NooPWWFS4lbxiKb3lUuleb5FSsW5UlwlDBWkDcV0TkznwmpN4G2eYMACuQeGGWxwKkTBMCeiuarPoB/FrywcxSgBloquva5FfIFZz+tV1oBPddyJWDIs9zeZdaRmmTa+Hnw/aKDi+BoctLPDRpvfGOc7o" +
                  "dVvqXFi1YLrD+Gj46K6s05Do0k6FOvnhodbKwsjOrca4E4QCAtFTDNp9JNNOMA2SgIWHhXwpIMsbqMp2Xqr38qp74CAfbMWt2+B1inFIkGZFSrGGYAOAmAOWEMCCD7onHEdQobEzuaRcLOXmuWbF4Rjak4M0j9/t4Rj627cUEM5BwsMOhGeVFd" +
                  "7ShLWpjo6nGvko7l2Cx+PmMAoq/e9TjTpQhUb4EiLvPtFJB6II73H4dcimjyvkh+kCrPtB4eDgPtlZB7LQMSy29BDV4HiwGEawu14Bpj5EtsPeimBvfSgVnmIcINxhg0WwwX5KublHF9H6dOqPZ9D91Xw67Ldohnah0FMfkP/4HWfn9ZPAJc8c" +
                  "x4R3GhzXmrk2B1Ppoc7oQGb9qiEeVyQezE44Px19HZV+Y2YNTTp5J1aSfPnXfx5M9gspEo5zIXLFDJSYN4Ki9tgbJ1zb70uKNYZqOwMBQykuS000Ns66Xw9dc1IWNYq/vkO1GoRI97TISqhUFa7vCvbOpblhW5S7Alt01znUk/LGg9z09w8WSS" +
                  "oN6DYg7xCNL3Heo2ulkz6s1rgCyROqGE7JN5C8Bl1FfvO5AAFBHigveGDh7IsG3+FHrOj7cXddvQWWdCdmOLwShKGR3ZK8MwTPG7HcV9tn2s7TiiML265BOmSf3WOj3A07zhI7ga8rWnhYYR8oRq2Yzsg4ja8U7SrzK8gWZaFxpMeqgLIHMqWR" +
                  "gI94bHf5ktrJBoSnlWYvBLElLRJEWLtDlExSN54jKyXzyvgVn/9jHL9my3JN3jNRYruIg8Wm5eOwlRWqA3z3ZLJvfEMi2JllnNqzndjwW1bHcldJP3BIk2eAthIOwqHEuj3qcl62Z0oFh34XVmAA0SRxMeDkwFyBO6yeSz2DWOSSYUzGaCmQyQ" +
                  "pqW+vqeABDAOBwELpQtbltt+UKgMgY0H0b0W7p0N7R5LuwbtaRZsKYHUJ3kndRncjZs0HMBLUcYEYhjbWkO5Jzx3o8bjrIxrHXnn372uuD6xkkaHea1z7Ks2mwPj3sKvUVygJuh3ZDQVjhkOrL7//d+V9Bq1ElXAxKexB31pw+2nprx1ogG8MU" +
                  "co/cUwT8Vqu13199e3K1sLulVLQ6aqunTBDEx7eX5zxhbublyNREb7nmFlysVsT9VKc+5/1RpPvvHKgAl7DZBR56GlndqSh4Lq0eyAH4I45EbvqYczGgrttCiFqGR576we3EQhBqf1SwBIgyhc6lcOPyWTQZjcbw+nA4DWajCYIcad2gq8HZ6s" +
                  "oNzuCOHaz1Q7Ced4vgbxAMgJv32/8AKNeQw+UkAAA=");
        Importing.allowImportHotkeys = prev;
    }

    private void RefreshBarIndexes()
    {
        for (int i = 0; i < bars.Count; i++)
            bars[i].ID = i;
    }

    public void SetBarHidden(int i, bool toggle, bool b = false)
    {
        if (i < 0 || i >= bars.Count)
            QoLBar.PrintError($"栏位 #{i + 1} 不存在");
        else
        {
            if (toggle)
                bars[i].IsHidden = !bars[i].IsHidden;
            else
                bars[i].IsHidden = b;
        }
    }

    public void SetBarHidden(string name, bool toggle, bool b = false)
    {
        var found = false;
        for (int i = 0; i < bars.Count; ++i)
        {
            if ((QoLBar.Config.BarCfgs[i]).Name == name)
            {
                found = true;
                SetBarHidden(i, toggle, b);
            }
        }
        if (!found)
            QoLBar.PrintError($"栏位 \"{name}\" 不存在");
    }

    private void BackupFile(FileInfo file, string name = "", bool overwrite = false)
    {
        try
        {
            if (file.Extension != ".json")
                throw new InvalidOperationException("文件必须是json格式！");

            if (string.IsNullOrEmpty(name))
                name = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");

            var path = QoLBar.Config.GetPluginBackupPath() + $"\\{name}.json";
            file.CopyTo(path, overwrite);
            DalamudApi.LogInfo($"文件已保存到 {path}");
        }
        catch (Exception e)
        {
            QoLBar.PrintError($"保存失败: {e.Message}");
        }
    }

    private void DeleteFile(FileInfo file)
    {
        try
        {
            if (file.Extension != ".json")
                throw new InvalidOperationException("文件必须是json格式！");

            file.Delete();
            DalamudApi.LogInfo($"已删除文件 {file.FullName}");
        }
        catch (Exception e)
        {
            QoLBar.PrintError($"删除失败: {e.Message}");
        }
    }

    public void Dispose()
    {
        foreach (var bar in bars)
            bar.Dispose();
    }

    // Helper function
    public static void DrawExternalWindow(Action draw, bool noViewport)
    {
        if (noViewport)
            ImGuiHelpers.ForceNextWindowMainViewport();
        QoLBar.Font.Pop();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, defaultSpacing);
        ImGuiEx.PushFontSize(QoLBar.DefaultFontSize);
        draw();
        ImGuiEx.PopFontSize();
        ImGui.PopStyleVar();
        QoLBar.Font.Push();
    }
}