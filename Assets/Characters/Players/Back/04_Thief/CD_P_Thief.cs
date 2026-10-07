using UnityEngine;

[CreateAssetMenu(menuName ="CharacterData/Player/Thief")]
public class CD_P_Thief : CharacterData
{
    public StatusEffectData PoisonData;
    public float DamageRatio;

    public override CharacterModel CreateModel(IBattleField battleField, CharaContext context)
    {
        return new CM_P_Thief(this, battleField, context);
    }
}

public class CM_P_Thief : CharacterModel
{
    public CD_P_Thief TypedData => (CD_P_Thief)Data;
    public CM_P_Thief(CharacterData data, IBattleField battleField, CharaContext context) : base(data, battleField, context) { }

    public override void PerformActiveSkill()
    {
        ActionParams actionParams;
        TryCreateActionFromDefinition(ActionSource.ActiveSkill, Data.ActiveSkillDefinition, out actionParams);

        int poisonStack = actionParams.Target.GetStEStack(TypedData.PoisonData);
        if (poisonStack > 0)
        {
            EffectParams effect = new EffectParams(EffectType.FixedDamage, poisonStack * TypedData.DamageRatio);
            actionParams.Effects.Add(effect);
        }

        PerformAction(actionParams);
    }
}


