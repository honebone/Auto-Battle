using UnityEngine;

[CreateAssetMenu(menuName = "PassiveData/Chara/Enemy/Goblin")]
public class PD_E_Goblin : PassiveData
{
    public float ASGrowth;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new PM_E_Goblin(owner, this, battleField);
    }
}

public class PM_E_Goblin : PassiveModel
{
    public PD_E_Goblin Data => (PD_E_Goblin)_data;
    private int _count;

    public PM_E_Goblin(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField)
    {

    }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (action.Owner == _owner && triggerType == TriggerType.OnActiveSkillCast)
        {
            _owner.AttackSpeed.AddMultiplier(Data.ASGrowth);
            _count++;
        }
    }

    public override void OnBattleEnd()
    {
        _owner.AttackSpeed.AddMultiplier(-_count * Data.ASGrowth);
        _count = 0;
    }
}