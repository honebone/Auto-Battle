using UnityEngine;

[CreateAssetMenu(menuName = "PassiveData/Chara/Player/Hunter")]
public class PD_P_Hunter : PassiveData
{
    public int MaxNACount = 2;

    public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    {
        return new PM_P_Hunter(owner, this, battleField, MaxNACount);
    }
}
public class PM_P_Hunter : PassiveModel
{
    private int _maxNACount;
    private int _naCount = 0;
    public PM_P_Hunter(CharacterModel owner, PassiveData data, IBattleField battleField, int maxNACount) : base(owner, data, battleField)
    {
        _maxNACount = maxNACount;
    }

    public override void ModifyAction(ref ActionParams actionParams)
    {
        if (_naCount == _maxNACount)
        {
            actionParams.guaranteeCritical = true;
            Debug.Log("確定クリティカル");
        }
    }

    public override void OnTriggered(TriggerType triggerType, ActionResult action)
    {
        if (action.Owner == _owner && triggerType == TriggerType.OnNormalAttackDealt)
        {
            if (_naCount == _maxNACount) _naCount = 0;
            else _naCount++;
        }
    }
}
