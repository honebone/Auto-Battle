using UnityEngine;

[CreateAssetMenu(menuName = "ItemData/LightningWand")]
public class ID_LightningWand : ItemData
{
    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new IM_LightningWand(owner, this, battleField);
    }
}

public class IM_LightningWand : PassiveModel
{
    private int _count;
    public override bool IsIconVisibleDefault =>  true;
    public IM_LightningWand(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField) { }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (!CheckTrigger(triggerType, action, _data.TriggerCondition)) return;
        _count++;
        for (int i = 0; i < _count; i++) { _owner.PerformActionsFromDefinition(ActionSource.PassiveSkill, _data.ActionDefinition); }
        _icon.SetText(_count);
    }

    public override void OnBattleEnd()
    {
        _count = 0;
        _icon.SetText("");
    }
}