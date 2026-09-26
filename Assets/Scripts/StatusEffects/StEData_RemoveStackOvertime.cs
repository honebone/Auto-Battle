using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffectData/RemoveStackOvertime")]
public class StEData_RemoveStackOvertime : StatusEffectData
{
    [Header("何秒ごとにスタックを減少させるか")]
    public float RemoveTime;
}

public class StEModel_RemoveStackOvertime : StatusEffectModel
{
    private float _removeTime;
    private float _removeTimer;

    public StEModel_RemoveStackOvertime(CharacterModel owner, PassiveData data, IBattleField battleField, int maxStack, int stack,float removeTime ) : base(owner, data, battleField,maxStack,stack)
    {
        _removeTime = removeTime;
    }

    public override void ManualUpdate(float deltaTime)
    {
        base.ManualUpdate(deltaTime);

        _removeTimer += deltaTime;
        if(_removeTimer >= _removeTime)
        {
            _removeTimer -= _removeTime;
            ChangeStack(-1);
        }
    }
}
