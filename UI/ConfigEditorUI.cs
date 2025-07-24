using System;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.ImGuiNotification;
using ImGuiNET;
using Dalamud.Interface.Utility;
using static QoLBar.BarCfg;
using static QoLBar.ShCfg;

namespace QoLBar;

// I hate this file with a passion
public static class ConfigEditorUI
{
    private static int _inputPos = 0;
    private static unsafe int GetCursorPosCallback(ImGuiInputTextCallbackData* data)
    {
        _inputPos = data->CursorPos;
        return 0;
    }

    private static void DrawInsertPrivateCharPopup(ref string input)
    {
        if (ImGui.BeginPopup($"Private Use Popup##{ImGui.GetCursorPos()}"))
        {
            var temp = input;
            static void InsertString(ref string str, string ins)
            {
                var bytes = Encoding.UTF8.GetBytes(str).ToList();
                _inputPos = Math.Min(_inputPos, bytes.Count);
                var newBytes = Encoding.UTF8.GetBytes(ins);
                for (int i = 0; i < newBytes.Length; i++)
                    bytes.Insert(_inputPos++, newBytes[i]);
                str = Encoding.UTF8.GetString(bytes.ToArray());
            }

            var bI = 0;
            void DrawButton(int i)
            {
                if (bI % 15 != 0)
                    ImGui.SameLine();

                var str = $"{(char)i}";
                ImGui.SetWindowFontScale(1.5f);
                if (ImGui.Button(str, new Vector2(36 * ImGuiHelpers.GlobalScale)))
                {
                    InsertString(ref temp, str);
                    QoLBar.Config.Save();
                }
                ImGui.SetWindowFontScale(1);
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.SetWindowFontScale(4);
                    ImGui.TextUnformatted(str);
                    ImGui.SetWindowFontScale(1);
                    ImGui.TextUnformatted($"{(Dalamud.Game.Text.SeIconChar)i}");
                    ImGui.EndTooltip();
                }

                bI++;
            }


            for (var i = 0xE020; i <= 0xE02B; i++)
                DrawButton(i);
            for (var i = 0xE031; i <= 0xE035; i++)
                DrawButton(i);
            for (var i = 0xE038; i <= 0xE044; i++)
                DrawButton(i);
            for (var i = 0xE048; i <= 0xE04E; i++)
                DrawButton(i);
            for (var i = 0xE050; i <= 0xE08A; i++)
                DrawButton(i);
            for (var i = 0xE08F; i <= 0xE0C6; i++)
                DrawButton(i);
            for (var i = 0xE0D0; i <= 0xE0DB; i++)
                DrawButton(i);

            input = temp;

            ImGui.EndPopup();
        }
    }

    private static void AddRightClickPrivateUsePopup(ref string input)
    {
        if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"Private Use Popup##{ImGui.GetCursorPos()}");

        DrawInsertPrivateCharPopup(ref input);
    }

    public static void AutoPasteIcon(ShCfg sh)
    {
        if (!IconBrowserUI.iconBrowserOpen || !IconBrowserUI.doPasteIcon) return;

        var split = sh.Name.Split(new[] { "##" }, 2, StringSplitOptions.None);
        var split2 = split[0].Split(new[] { "::" }, 2, StringSplitOptions.None);
        sh.Name = $"{split2[0]}::{IconBrowserUI.pasteIcon}" + (split.Length > 1 ? $"##{split[1]}" : "");
        QoLBar.Config.Save();
        IconBrowserUI.doPasteIcon = false;
    }

    public static void EditShortcutConfigBase(ShCfg sh, bool editing, bool hasIcon)
    {
        EditShortcutName(sh, editing);
        ImGuiEx.SetItemTooltip("在名称开头或结尾使用 ::x (x为数字) 来使用图标，例如 \"::2914\"。\n" +
                               "在名称中使用 ## 可将后面的文本设为提示信息，\n例如 \"名称##这是提示信息\"。"
                               + (hasIcon ?
                                   "\n\n图标可在 \"::\" 和ID之间添加参数，例如 \"::f21\"。\n" +
                                   "\t' f ' - 应用热键栏边框\n" +
                                   "\t' n ' - 移除热键栏边框\n" +
                                   "\t' l ' - 使用低分辨率图标\n" +
                                   "\t' h ' - 使用高分辨率图标（如果存在）\n" +
                                   "\t' g ' - 将图标转为灰度\n" +
                                   "\t' r ' - 反转图标"
                                   : string.Empty));

        var _t = (int)sh.Type;
        ImGui.TextUnformatted("类型");
        ImGui.RadioButton("命令", ref _t, 0);
        ImGui.SameLine(ImGui.GetWindowWidth() / 3);
        ImGui.RadioButton("分类", ref _t, 1);
        ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
        ImGui.RadioButton("间隔", ref _t, 2);
        if (_t != (int)sh.Type)
        {
            sh.Type = (ShortcutType)_t;
            if (sh.Type == ShortcutType.Category)
                sh.SubList ??= new List<ShCfg>();

            if (editing)
                QoLBar.Config.Save();
        }

        if (sh.Type != ShortcutType.Spacer && (sh.Type != ShortcutType.Category || sh.Mode == ShortcutMode.Default))
        {
            var height = ImGui.GetFontSize() * Math.Min(sh.Command.Split('\n').Length + 1, 7) + ImGui.GetStyle().FramePadding.Y * 2; // ImGui issue #238: can't disable multiline scrollbar and it appears a whole line earlier than it should, so thats cool I guess

            unsafe
            {
                if (ImGui.InputTextMultiline("命令##Input", ref sh.Command, 65535, new Vector2(0, height), ImGuiInputTextFlags.CallbackAlways, GetCursorPosCallback) && editing)
                    QoLBar.Config.Save();
            }
            AddRightClickPrivateUsePopup(ref sh.Command);
            ImGuiEx.SetItemTooltip("右键点击可添加特殊游戏符号，此外还有仅快捷方式可用的自定义命令：\n" +
                                   "\t' //m0 ' - 执行独立宏#0（最多到//m99）\n" +
                                   "\t' //m100 ' - 执行共享宏#0（最多到//m199）\n" +
                                   "\t' //m ' - 开始或结束自定义宏。后续行将作为宏执行（支持\n" +
                                   "/wait, /macrolock等），直到再次使用//m，最多30行\n" +
                                   "\t' //i <ID/名称> ' - 使用物品，不能与//m同时使用\n" +
                                   "\t' // <注释> ' - 添加注释");
        }
    }

    public static unsafe bool EditShortcutName(ShCfg sh, bool editing)
    {
        var ret = ImGui.InputText("名称", ref sh.Name, 256, ImGuiInputTextFlags.CallbackAlways, GetCursorPosCallback);
        AddRightClickPrivateUsePopup(ref sh.Name);

        if (ret && editing)
            QoLBar.Config.Save();

        return ret;
    }

    public static bool EditShortcutMode(ShortcutUI sh)
    {
        var _m = (int)sh.Config.Mode;
        ImGui.TextUnformatted("模式");
        ImGuiEx.SetItemTooltip("更改按下时的行为\n注意：不适用于包含子分类的分类");

        ImGui.RadioButton("默认", ref _m, 0);
        ImGuiEx.SetItemTooltip("默认行为，分类必须设为此项才能编辑其快捷方式！");

        ImGui.SameLine(ImGui.GetWindowWidth() / 3);
        ImGui.RadioButton("顺序", ref _m, 1);
        ImGuiEx.SetItemTooltip("多次按下时按顺序执行每行/快捷方式");

        ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
        ImGui.RadioButton("随机", ref _m, 2);
        ImGuiEx.SetItemTooltip("按下时随机执行一行/快捷方式");

        if (_m != (int)sh.Config.Mode)
        {
            sh.Config.Mode = (ShortcutMode)_m;

            if (sh.Config.Mode == ShortcutMode.Random)
            {
                var c = Math.Max(1, (sh.Config.Type == ShortcutType.Category) ? sh.children.Count : sh.Config.Command.Split('\n').Length);
                sh.Config._i = (int)(QoLBar.FrameCount % c);
            }
            else
            {
                sh.Config._i = 0;
            }

            QoLBar.Config.Save();

            return true;
        }

        return false;
    }

    public static bool EditShortcutColor(ShortcutUI sh)
    {
        var color = ImGui.ColorConvertU32ToFloat4(sh.Config.Color);
        color.W += sh.Config.ColorAnimation / 255f; // Temporary
        if (ImGui.ColorEdit4("颜色", ref color, ImGuiColorEditFlags.NoDragDrop | ImGuiColorEditFlags.AlphaPreviewHalf))
        {
            sh.Config.Color = ImGui.ColorConvertFloat4ToU32(color);
            sh.Config.ColorAnimation = Math.Max((int)Math.Round(color.W * 255) - 255, 0);
            QoLBar.Config.Save();
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void EditShortcutCategoryOptions(ShortcutUI sh)
    {
        if (ImGui.SliderInt("按钮宽度", ref sh.Config.CategoryWidth, 0, 200))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("设为0则使用文本宽度");

        if (ImGui.SliderInt("列数", ref sh.Config.CategoryColumns, 0, 12))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("每行显示的快捷方式数量，超过后换行\n设为0表示无限制");

        if (ImGui.DragFloat("缩放", ref sh.Config.CategoryScale, 0.002f, 0.7f, 2f, "%.2f"))
            QoLBar.Config.Save();

        if (ImGui.DragFloat("字体缩放", ref sh.Config.CategoryFontScale, 0.0018f, 0.5f, 1.0f, "%.2f"))
            QoLBar.Config.Save();

        var spacing = new Vector2(sh.Config.CategorySpacing[0], sh.Config.CategorySpacing[1]);
        if (ImGui.DragFloat2("间距", ref spacing, 0.12f, 0, 32, "%.f"))
        {
            sh.Config.CategorySpacing[0] = (int)spacing.X;
            sh.Config.CategorySpacing[1] = (int)spacing.Y;
            QoLBar.Config.Save();
        }

        if (ImGui.Checkbox("悬停时打开", ref sh.Config.CategoryOnHover))
            QoLBar.Config.Save();
        ImGui.SameLine(ImGui.GetWindowWidth() / 2);
        if (ImGui.Checkbox("非悬停时关闭", ref sh.Config.CategoryHoverClose))
            QoLBar.Config.Save();

        if (ImGui.Checkbox("选择后保持打开", ref sh.Config.CategoryStaysOpen))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("在分类内按下快捷方式时保持打开状态\n若快捷方式与其他插件交互可能无效");
        ImGui.SameLine(ImGui.GetWindowWidth() / 2);
        if (ImGui.Checkbox("无背景", ref sh.Config.CategoryNoBackground))
            QoLBar.Config.Save();
    }

    public static void EditShortcutIconOptions(ShortcutUI sh)
    {
        if (ImGui.DragFloat("缩放", ref sh.Config.IconZoom, 0.005f, 1.0f, 5.0f, "%.2f"))
            QoLBar.Config.Save();

        var offset = new Vector2(sh.Config.IconOffset[0], sh.Config.IconOffset[1]);
        if (ImGui.DragFloat2("偏移", ref offset, 0.0005f, -0.5f, 0.5f, "%.3f"))
        {
            sh.Config.IconOffset[0] = offset.X;
            sh.Config.IconOffset[1] = offset.Y;
            QoLBar.Config.Save();
        }

        var r = (float)(sh.Config.IconRotation * 180 / Math.PI) % 360;
        if (ImGui.DragFloat("旋转", ref r, 0.2f, -360, 360, "%.f"))
        {
            if (r < 0)
                r += 360;
            sh.Config.IconRotation = (float)(r / 180 * Math.PI);
            QoLBar.Config.Save();
        }

        static string formatName(Lumina.Excel.Sheets.Action a) => a.RowId switch
        {
            0 => "无",
            847 => "[847] 物品",
            _ => $"[{a.RowId}] {a.Name}"
        };
        if (ImGuiEx.ExcelSheetCombo<Lumina.Excel.Sheets.Action>("冷却动作ID", out var action, s => s.GetRowOrDefault(sh.Config.CooldownAction) is { } a ? formatName(a) : sh.Config.CooldownAction.ToString(),
            ImGuiComboFlags.None, (a, s) => (a.RowId == 0 || a is { CooldownGroup: > 0, ClassJobCategory.RowId: > 0 }) && formatName(a).Contains(s, StringComparison.CurrentCultureIgnoreCase),
            a => ImGui.Selectable(formatName(a), sh.Config.CooldownAction == a.RowId)))
        {
            sh.Config.CooldownAction = action.Value.RowId;

            if (sh.Config.CooldownAction == 0)
                sh.Config.CooldownStyle = 0;

            QoLBar.Config.Save();
        }

        if (sh.Config.CooldownAction > 0)
        {
            var save = ImGui.CheckboxFlags("##CooldownNumber", ref sh.Config.CooldownStyle, (int)ImGuiEx.IconSettings.CooldownStyle.Number);
            ImGuiEx.SetItemTooltip("数字显示");
            ImGui.SameLine();
            save |= ImGui.CheckboxFlags("##CooldownDisable", ref sh.Config.CooldownStyle, (int)ImGuiEx.IconSettings.CooldownStyle.Disable);
            ImGuiEx.SetItemTooltip("变暗（强制显示图标边框）");
            ImGui.SameLine();
            save |= ImGui.CheckboxFlags("##CooldownDefault", ref sh.Config.CooldownStyle, (int)ImGuiEx.IconSettings.CooldownStyle.Cooldown);
            ImGuiEx.SetItemTooltip("默认转圈（强制显示图标边框）");
            ImGui.SameLine();
            save |= ImGui.CheckboxFlags("##CooldownGCD", ref sh.Config.CooldownStyle, (int)ImGuiEx.IconSettings.CooldownStyle.GCDCooldown);
            ImGuiEx.SetItemTooltip("Orange GCD Spinner");
            ImGui.SameLine();
            save |= ImGui.CheckboxFlags("冷却样式标志##CooldownCharge", ref sh.Config.CooldownStyle, (int)ImGuiEx.IconSettings.CooldownStyle.ChargeCooldown);
            ImGuiEx.SetItemTooltip("Charge Spinner");
            if (save)
                QoLBar.Config.Save();
        }
    }

    public static void EditBarGeneralOptions(BarUI bar)
    {
        if (ImGui.InputText("名称", ref bar.Config.Name, 256))
            QoLBar.Config.Save();

        var _dock = (int)bar.Config.DockSide;
        if (ImGui.Combo("位置", ref _dock, (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0
                ? "顶部\0右侧\0底部\0左侧\0浮动"
                : "顶部\0右侧\0底部\0左侧"))
        {
            bar.Config.DockSide = (BarDock)_dock;
            if (bar.Config.DockSide == BarDock.Undocked && bar.Config.Visibility == BarVisibility.Slide)
                bar.Config.Visibility = BarVisibility.Always;
            bar.Config.Position[0] = 0;
            bar.Config.Position[1] = 0;
            bar.Config.LockedPosition = false;
            QoLBar.Config.Save();
            bar.SetupPivot();
        }

        if (bar.IsDocked)
        {
            var topbottom = bar.Config.DockSide == BarDock.Top || bar.Config.DockSide == BarDock.Bottom;
            var _align = (int)bar.Config.Alignment;
            ImGui.Text("对齐方式");
            ImGui.RadioButton(topbottom ? "左对齐" : "顶对齐", ref _align, 0);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3);
            ImGui.RadioButton("居中", ref _align, 1);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
            ImGui.RadioButton(topbottom ? "右对齐" : "底对齐", ref _align, 2);
            if (_align != (int)bar.Config.Alignment)
            {
                bar.Config.Alignment = (BarAlign)_align;
                QoLBar.Config.Save();
                bar.SetupPivot();
            }

            var _visibility = (int)bar.Config.Visibility;
            ImGui.Text("动画效果");
            ImGui.RadioButton("滑动出现", ref _visibility, 0);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3);
            ImGui.RadioButton("立即出现", ref _visibility, 1);
            ImGui.SameLine(ImGui.GetWindowWidth() / 3 * 2);
            ImGui.RadioButton("始终显示", ref _visibility, 2);
            if (_visibility != (int)bar.Config.Visibility)
            {
                bar.Config.Visibility = (BarVisibility)_visibility;
                QoLBar.Config.Save();
            }

            if ((bar.Config.Visibility != BarVisibility.Always) && ImGui.DragFloat("显示区域缩放", ref bar.Config.RevealAreaScale, 0.01f, 0.0f, 1.0f, "%.2f"))
                QoLBar.Config.Save();
        }
        else
        {
            var _visibility = (int)bar.Config.Visibility;
            ImGui.Text("动画效果");
            ImGui.RadioButton("立即出现", ref _visibility, 1);
            ImGui.SameLine(ImGui.GetWindowWidth() / 2);
            ImGui.RadioButton("始终显示", ref _visibility, 2);
            if (_visibility != (int)bar.Config.Visibility)
            {
                bar.Config.Visibility = (BarVisibility)_visibility;
                QoLBar.Config.Save();
            }
        }

        Keybind.KeybindInput(bar.Config);

        if (ImGui.Checkbox("编辑模式", ref bar.Config.Editing))
        {
            if (!bar.Config.Editing)
                Game.ExecuteCommand("/echo <se> 可以右键点击栏位本身（黑色背景）重新打开此设置菜单！也可使用Shift+右键添加新快捷方式。");
            QoLBar.Config.Save();
        }
        ImGui.SameLine(ImGui.GetWindowWidth() / 2);
        if (ImGui.Checkbox("点击穿透", ref bar.Config.ClickThrough))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("警告：这将阻止您与此栏位交互\n要重新编辑设置，需使用通用配置中栏位名称旁的\"O\"按钮");

        if (ImGui.Checkbox("锁定位置", ref bar.Config.LockedPosition))
            QoLBar.Config.Save();
        if (bar.IsDocked && bar.Config.Visibility != BarVisibility.Always)
        {
            ImGui.SameLine(ImGui.GetWindowWidth() / 2);
            if (ImGui.Checkbox("Hint", ref bar.Config.Hint))
                QoLBar.Config.Save();
            ImGuiEx.SetItemTooltip("防止栏位休眠，会增加CPU负载");
        }

        if (!bar.Config.LockedPosition)
        {
            var pos = bar.VectorPosition;
            var area = bar.UsableArea;
            var max = (area.X > area.Y) ? area.X : area.Y;
            if (ImGui.DragFloat2(bar.IsDocked ? "偏移量" : "位置", ref pos, 1, -max, max, "%.f"))
            {
                bar.Config.Position[0] = Math.Min(pos.X / area.X, 1);
                bar.Config.Position[1] = Math.Min(pos.Y / area.Y, 1);
                QoLBar.Config.Save();
                if (bar.IsDocked)
                    bar.SetupPivot();
                else
                    bar._setPos = true;
            }
        }
    }

    public static void EditBarStyleOptions(BarUI bar)
    {
        if (ImGui.SliderInt("按钮宽度", ref bar.Config.ButtonWidth, 0, 200))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("设为0则使用文本宽度");

        if (ImGui.SliderInt("列数", ref bar.Config.Columns, 0, 12))
            QoLBar.Config.Save();
        ImGuiEx.SetItemTooltip("每行显示的快捷方式数量，超过后换行\n设为0表示无限制");

        if (ImGui.DragFloat("缩放", ref bar.Config.Scale, 0.002f, 0.7f, 2.0f, "%.2f"))
            QoLBar.Config.Save();

        if (ImGui.DragFloat("字体缩放", ref bar.Config.FontScale, 0.0018f, 0.5f, 1.0f, "%.2f"))
            QoLBar.Config.Save();

        var spacing = new Vector2(bar.Config.Spacing[0], bar.Config.Spacing[1]);
        if (ImGui.DragFloat2("间距", ref spacing, 0.12f, 0, 32, "%.f"))
        {
            bar.Config.Spacing[0] = (int)spacing.X;
            bar.Config.Spacing[1] = (int)spacing.Y;
            QoLBar.Config.Save();
        }

        if (ImGui.Checkbox("无背景", ref bar.Config.NoBackground))
            QoLBar.Config.Save();
    }

    public static void DisplayRightClickDeleteMessage(string text = "右键点击删除！") =>
        DalamudApi.ShowNotification($"\t\t\t{text}\t\t\t\n\n", NotificationType.Info);
}