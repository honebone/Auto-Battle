using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffectData/RemoveStackOvertime")]
public class StEData_RemoveStackOvertime : StatusEffectData
{
    [Header("何秒ごとにスタックを1減少させるか")]
    public float RemoveTime = 1;

    public override StatusEffectModel CreateStatusEffectModel(CharacterModel owner, CharacterModel source, IBattleField battleField)
    {
        return new StEModel_RemoveStackOvertime(owner, source, this, battleField);
    }
}

public class StEModel_RemoveStackOvertime : StatusEffectModel
{
    private float _removeTimer;

    private StEData_RemoveStackOvertime TypedData => (StEData_RemoveStackOvertime)_data;

    public StEModel_RemoveStackOvertime(CharacterModel owner, CharacterModel source, StEData_RemoveStackOvertime data, IBattleField battleField) : base(owner, source, data, battleField) { }

    public override void ManualUpdate(float deltaTime)
    {
        base.ManualUpdate(deltaTime);

        float removeTime = TypedData.RemoveTime;
        if (removeTime <= 0) return;

        _removeTimer += deltaTime;
        if (_removeTimer >= removeTime)
        {
            _removeTimer -= removeTime;
            ChangeStack(-1);
        }

        _icon.SetGauge(1 - _removeTimer / removeTime);
    }
}
