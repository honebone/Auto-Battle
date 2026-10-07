using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffectData/Poison")]
public class StEData_Poison : StatusEffectData
{
    public float DamageInterval;
    public EffectDefinition Effect;

    public override StatusEffectModel CreateStatusEffectModel(CharacterModel owner, CharacterModel source, IBattleField battleField)
    {
        return new StEModel_Poison(owner, source, this, battleField);
    }
}

public class StEModel_Poison : StatusEffectModel
{
    private float _removeTimer;

    private StEData_Poison TypedData => (StEData_Poison)_data;

    public StEModel_Poison(CharacterModel owner, CharacterModel source, StEData_Poison data, IBattleField battleField) : base(owner, source, data, battleField) { }

    public override void ManualUpdate(float deltaTime)
    {
        base.ManualUpdate(deltaTime);

        float removeTime = TypedData.DamageInterval;
        if (removeTime <= 0) return;

        _removeTimer += deltaTime;
        if (_removeTimer >= removeTime)
        {
            _removeTimer -= removeTime;
            //TODO:å≈íËÉ_ÉÅÅ[ÉW
        }

        _icon.SetGauge(_removeTimer / removeTime);
    }
}
