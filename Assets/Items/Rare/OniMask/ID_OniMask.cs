using UnityEngine;

[CreateAssetMenu(menuName = "ItemData/OniMask")]
public class ID_OniMask : ItemData
{
    public float HPTH;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new IM_OniMask(owner, this, battleField);
    }
}

public class IM_OniMask : PassiveModel
{
    private bool _activated;
    public ID_OniMask TypedData => (ID_OniMask)_data;
    public override bool IsIconVisibleDefault => true;
    public IM_OniMask(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField) { }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (!CheckTrigger(triggerType, action, _data.TriggerCondition) || _activated) return;
        if (_owner.CurrentHpRatio <= TypedData.HPTH)
        {
            _activated = true;
            _icon.SetActiveState(PassiveIconState.Disabled);

            _owner.PerformActionsFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition);
        }
    }

    public override void OnBattleEnd()
    {
        _activated = false;
        _icon.SetActiveState(PassiveIconState.Default);
        _icon.SetText("");
    }
}
