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
        if (_data.AutoSubscribe) _battleField.TriggerAction += TriggerActionFromDefinition;
        _battleField.TriggerAction += OnTriggered;
        _owner.ModifyAction += ModifyAction;
    }

    public virtual void ManualUpdate(float deltaTime)
    {
       
    }

    public virtual void ModifyAction(ref ActionParams actionParams) { }

    public void TriggerActionFromDefinition(TriggerType triggerType, ActionResult action)
    {
        if (triggerType != _data.TriggerType) return;
        if (!_battleField.CheckObserveTarget(_owner, action, triggerType, _data.ObserveTargetType, _data.ObserveActionOwner)) return;
        _owner.PerformActionsFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition);
    }

    public virtual void OnTriggered(TriggerType triggerType, ActionResult action) { }

    public void Disable()
    {
        ApplyStatusModifier(false);
        if (_data.AutoSubscribe) _battleField.TriggerAction -= TriggerActionFromDefinition;
        _battleField.TriggerAction -= OnTriggered;
        _owner.ModifyAction -= ModifyAction;
    }

    private void ApplyStatusModifier(bool set)
    {
        float sign = set ? 1f : -1f;

        _owner.MaxHealth.AddMultiplier(_data.MaxHealthMul * sign);
        _owner.AttackPower.AddMultiplier(_data.AttackPowerMul * sign);
        _owner.MagicPower.AddMultiplier(_data.MagicPowerMul * sign);
        _owner.AttackSpeed.AddMultiplier(_data.AttackSpeedMul * sign);
        _owner.CastSpeed.AddMultiplier(_data.CastSpeedMul * sign);

        _owner.CriticalChance.AddFlat(_data.CriticalChance * sign);
        _owner.Drain.AddFlat(_data.Drain * sign);
    }
}
