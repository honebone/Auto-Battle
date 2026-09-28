using UnityEngine;

[CreateAssetMenu(menuName = "PassiveData/Chara/Player/Hunter")]
public class PD_P_Hunter : PassiveData
{
    public int MaxNACount = 2;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new PM_P_Hunter(owner, this, battleField);
    }
}
public class PM_P_Hunter : PassiveModel
{
    private int _naCount = 0;

    public PD_P_Hunter Data => (PD_P_Hunter)_data;

    public PM_P_Hunter(CharacterModel owner, PassiveData data, IBattleField battleField) : base(owner, data, battleField)
    {
    }

    public override void ModifyAction(ref ActionParams actionParams)
    {
        if (_naCount == Data.MaxNACount)
        {
            actionParams.guaranteeCritical = true;
            Debug.Log("確定クリティカル");
        }
    }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (action.Owner == _owner && triggerType == TriggerType.OnNormalAttackDealt)
        {
            if (_naCount == Data.MaxNACount) _naCount = 0;
            else _naCount++;
        }
    }

    public override void OnBattleEnd()
    {
        _naCount = 0;
    }
}
