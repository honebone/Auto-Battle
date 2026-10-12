using UnityEngine;

[CreateAssetMenu(menuName = "ItemData/ModifyStatOnTrigger")]
public class ID_ModifyStatOnTrigger : ItemData
{
    public int MaxActivation;
    public int ActivateCountReq = 1;
    public TriggerCondition StatusModeCondition;
    public CharaStatusMod StatusModOnTrigger;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new IM_ModifyStatOnTrigger(owner, this, battleField);
    }
}

public class IM_ModifyStatOnTrigger : PassiveModel
{
    private int _count;
    private int _activateCount;

    public ID_ModifyStatOnTrigger Data => (ID_ModifyStatOnTrigger)_data;

    public override bool IsIconVisibleDefault => true;

    public IM_ModifyStatOnTrigger(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField) { }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (!CheckTrigger(triggerType, action, Data.StatusModeCondition)) return;
        if (Data.MaxActivation > 0 && _activateCount == Data.MaxActivation) return;
        _count++;
        if(_count == Data.ActivateCountReq)
        {
            _count = 0;
            _activateCount++;
            _icon.SetText(_activateCount);
            _owner.ApplyStatusModifier(Data.StatusModOnTrigger, 1);
        }
        if (Data.ActivateCountReq >= 1) _icon.SetGauge((float)_count / Data.ActivateCountReq);
    }

    public override void OnBattleEnd()
    {
        _count = 0;
        if (_activateCount > 0) _owner.ApplyStatusModifier(Data.StatusModOnTrigger, -_activateCount);
        _activateCount = 0;
    }
}
