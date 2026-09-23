using UnityEngine;

public class PassiveModel
{
    private protected CharacterModel _owner;
    private protected PassiveData _data;
    private protected IBattleField _battleField;

    public  PassiveModel(CharacterModel owner, PassiveData data,IBattleField battleField)
    {
        _owner = owner;
        _data = data;
        _battleField = battleField;
    }

    public void Init()
    {
        ApplyStatusModifier(true);
        //if (_data.AutoSubscribe) _owner.TriggerPassiveAction += TriggerActionFromDefinition;
    }

    public void ManualUpdate(float deltaTime)
    {
       
    }

    public void TriggerActionFromDefinition(TriggerType triggerType, ActionResult action)
    {
        if (!_battleField.CheckObserveTarget(_owner, action, triggerType, _data.ObserveTargetType)) return;
        _owner.PerformActionsFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition);
    }

    public void Disable()
    {
        ApplyStatusModifier(false);
        //if (_data.AutoSubscribe) _owner.TriggerPassiveAction -= TriggerActionFromDefinition;
    }

    private void ApplyStatusModifier(bool set)
    {
        int sign = set ? 1 : -1;

        _owner.MaxHealth.AddMultiplier(_data.MaxHealthMul * sign);
        _owner.AttackPower.AddMultiplier(_data.AttackPowerMul * sign);
        _owner.MagicPower.AddMultiplier(_data.MagicPowerMul * sign);
        _owner.AttackSpeed.AddMultiplier(_data.AttackSpeedMul * sign);
        _owner.CastSpeed.AddMultiplier(_data.CastSpeedMul * sign);

        _owner.CriticalChance.AddFlat(_data.CriticalChance * sign);
        _owner.Drain.AddFlat(_data.Drain * sign);
    }
}
