using UnityEngine;

[CreateAssetMenu(menuName = "PassiveData/Chara/Player/Knight")]
public class PD_P_Knight : PassiveData
{
    public float HPRatioTH = 0.5f;
    public float BonusShield = 1f;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new PM_P_Knight(owner, this, battleField);
    }
}
public class PM_P_Knight : PassiveModel
{
    public PD_P_Knight Data => (PD_P_Knight)_data;

    public PM_P_Knight(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField)
    {
        
    }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (action.Owner == _owner && triggerType == TriggerType.OnActiveSkillCast)
        {
            if (_owner.TryCreateActionFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition, out var actionParams))
            {
                if (_owner.CurrentHpRatio <= Data.HPRatioTH) actionParams.BonusShield += Data.BonusShield;

                _owner.PerformAction(actionParams);
            }
        }
    }
}
