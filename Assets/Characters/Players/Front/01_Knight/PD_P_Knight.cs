using UnityEngine;

[CreateAssetMenu(menuName = "PassiveData/Chara/Player/Knight")]
public class PD_P_Knight : PassiveData
{
    public float HPRatioTH = 0.5f;
    public float BonusShield = 1f;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new PM_P_Knight(owner, this, battleField,HPRatioTH,BonusShield);
    }
}
public class PM_P_Knight : PassiveModel
{
    private float _hpRatioTH;
    private float _bonusShield;
    public PM_P_Knight(CharacterModel owner, PassiveData data, IBattleField battleField, float hpRatioTH, float bonusShield) : base(owner, data, battleField)
    {
        _hpRatioTH = hpRatioTH;
        _bonusShield = bonusShield;
    }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (action.Owner == _owner && triggerType == TriggerType.OnActiveSkillCast)
        {
            if (_owner.TryCreateActionFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition, out var actionParams))
            {
                if (_owner.CurrentHpRatio <= _hpRatioTH) actionParams.BonusShield += _bonusShield;

                _owner.PerformAction(actionParams);
            }
        }
    }
}
