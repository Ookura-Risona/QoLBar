using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace QoLBar.Conditions;

public class RoleCondition : ICondition, IDrawableCondition, IArgCondition, IConditionCategory
{
    public static readonly Dictionary<int, string> roleDictionary = new()
    {
        [1] = "坦克",
        [2] = "近战",
        [3] = "远程",
        [4] = "奶妈",
        [30] = "远敏",
        [31] = "法系",
        [32] = "采集",
        [33] = "制作"
    };

    public string ID => "r";
    public string ConditionName => "Role";
    public string CategoryName => "职能";
    public int DisplayPriority => 0;
    public bool Check(dynamic arg) => DalamudApi.ClientState.LocalPlayer is { } player
        && ((uint)arg < 30 ? player.ClassJob.ValueNullable?.Role : player.ClassJob.ValueNullable?.ClassJobCategory.RowId) == (uint)arg;
    public string GetTooltip(CndCfg cndCfg) => null;
    public string GetSelectableTooltip(CndCfg cndCfg) => null;
    public void Draw(CndCfg cndCfg)
    {
        roleDictionary.TryGetValue((int)cndCfg.Arg, out string s);
        if (!ImGui.BeginCombo("##Role", s)) return;

        foreach (var (id, name) in roleDictionary)
        {
            if (!ImGui.Selectable(name, id == cndCfg.Arg)) continue;

            cndCfg.Arg = id;
            QoLBar.Config.Save();
        }
        ImGui.EndCombo();
    }
    public dynamic GetDefaultArg(CndCfg cndCfg) => roleDictionary.FirstOrDefault(kv => Check(kv.Key)).Key;
}