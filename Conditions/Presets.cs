using Dalamud.Game.ClientState.Conditions;

namespace QoLBar.Conditions;

public class CurrentJobPreset : IConditionSetPreset
{
    public string Name => "当前职业";
    public CndSetCfg Generate()
    {
        var jobRow = DalamudApi.ClientState.LocalPlayer?.ClassJob;
        if (jobRow == null) return null;

        var set = new CndSetCfg { Name = jobRow.Value.ValueNullable?.Abbreviation.ExtractText() };
        set.Conditions.Add(new() { ID = new JobCondition().ID, Arg = jobRow.Value.RowId });

        return set;
    }
}

public class CurrentRolePreset : IConditionSetPreset
{
    public string Name => "当前职能";
    public CndSetCfg Generate()
    {
        var jobRow = DalamudApi.ClientState.LocalPlayer?.ClassJob;
        if (jobRow == null) return null;

        var role = jobRow.Value.ValueNullable?.Role;
        var set = new CndSetCfg { Name = role is > 0 and < 5 ? RoleCondition.roleDictionary[(int)role] : string.Empty };
        set.Conditions.Add(new() { ID = new RoleCondition().ID, Arg = role });

        return set;
    }
}

public class AllCurrentConditionFlagsPreset : IConditionSetPreset
{
    public string Name => "所有当前激活的状态标识";
    public CndSetCfg Generate()
    {
        var set = new CndSetCfg { Name = "激活的状态标识" };

        for (int i = 0; i < DalamudApi.Condition.MaxEntries; i++)
        {
            if (!DalamudApi.Condition[i]) continue;
            set.Conditions.Add(new() { ID = ConditionFlagCondition.constID, Arg = i });
        }

        return set;
    }
}

public class OutOfCombatPreset : IConditionSetPreset
{
    public string Name => "非战斗状态";
    public CndSetCfg Generate()
    {
        var set = new CndSetCfg { Name = "非战斗状态" };
        set.Conditions.Add(new() { ID = ConditionFlagCondition.constID, Arg = (int)ConditionFlag.InCombat, Negate = true });
        return set;
    }
}

public class OutofTheWayPreset : IConditionSetPreset
{
    public string Name => "非副本/过场/加载状态";
    public CndSetCfg Generate()
    {
        var set = new CndSetCfg { Name = "安全区域状态" };
        set.Conditions.Add(new() { ID = ConditionFlagCondition.constID, Arg = (int)ConditionFlag.BoundByDuty, Negate = true });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 732, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 763, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 795, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 827, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 920, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 975, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = new ZoneCondition().ID, Arg = 1055, Operator = ConditionManager.BinaryOperator.OR });
        set.Conditions.Add(new() { ID = ConditionFlagCondition.constID, Arg = (int)ConditionFlag.BetweenAreas, Negate = true });
        set.Conditions.Add(new() { ID = ConditionFlagCondition.constID, Arg = (int)ConditionFlag.OccupiedInCutSceneEvent, Negate = true });

        return set;
    }
}