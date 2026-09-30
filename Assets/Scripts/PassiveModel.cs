using UnityEngine;

public class PassiveModel
{
    private protected CharacterModel _owner;
    private protected PassiveData _data;
    private protected IBattleField _battleField;

    /// <summary>Viewが無い場合はNullPassiveIconのまま。派生クラスはViewの有無を気にせず呼んでよい</summary>
    private protected IPassiveIcon _icon = NullPassiveIcon.Instance;
    private protected virtual PassiveIconType IconType => PassiveIconType.Other;

    public  PassiveModel(CharacterModel owner, PassiveData data,IBattleField battleField)
    {
        _owner = owner;
        _data = data;
        _battleField = battleField;
    }

    public virtual void Init()
    {
        ApplyStatusModifier(1f);
        Subscribe();
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
    public virtual void OnBattleStart() { }

    public virtual void OnTriggered(TriggerType triggerType, ActionResult action) { }

    /// <summary>
    /// 戦闘終了時に呼ばれる。戦闘中限定の効果(戦闘終了時まで続く補正など)をリセットする
    /// パッシブ自体は有効のまま
    /// </summary>
    public virtual void OnBattleEnd() { }

    public virtual void Disable()
    {
        ApplyStatusModifier(-1f);
        Unsubscribe();

        _icon.Release();
        _icon = NullPassiveIcon.Instance;
    }

    /// <summary>
    /// 表示用のアイコンを紐づける(Presenterから呼ばれる)。既に紐づいているアイコンは破棄する
    /// nullを渡すと紐づけを解除する
    /// </summary>
    public void BindIcon(IPassiveIcon icon)
    {
        _icon.Release();
        _icon = icon ?? NullPassiveIcon.Instance;
        _icon.Init(_data.PassiveIcon, IconType);
    }

    private protected void Subscribe()
    {
        if (_data.AutoSubscribe) _battleField.TriggerAction += TriggerActionFromDefinition;
        _battleField.TriggerAction += OnTriggered;
        _owner.ModifyAction += ModifyAction;
    }

    private protected void Unsubscribe()
    {
        if (_data.AutoSubscribe) _battleField.TriggerAction -= TriggerActionFromDefinition;
        _battleField.TriggerAction -= OnTriggered;
        _owner.ModifyAction -= ModifyAction;
    }

    /// <summary>PassiveDataの補正値にscaleを掛けて反映する(1で付与、-1で解除)</summary>
    private protected void ApplyStatusModifier(float scale)
    {
        _owner.MaxHealth.AddMultiplier(_data.MaxHealthMul * scale);
        _owner.AttackPower.AddMultiplier(_data.AttackPowerMul * scale);
        _owner.MagicPower.AddMultiplier(_data.MagicPowerMul * scale);
        _owner.AttackSpeed.AddMultiplier(_data.AttackSpeedMul * scale);
        _owner.CastSpeed.AddMultiplier(_data.CastSpeedMul * scale);

        _owner.CriticalChance.AddFlat(_data.CriticalChance * scale);
        _owner.Drain.AddFlat(_data.Drain * scale);
    }
}
