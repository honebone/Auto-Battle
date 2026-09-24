using NaughtyAttributes;
using System.Collections.Generic;
using test;
using UnityEngine;

public enum ActionSource { Other, NormalAttack, ActiveSkill, PassiveSkill }

public struct ActionParams
{
    public CharacterModel Owner;
    public CharacterModel Target;
    public ActionSource Source;

    public List<EffectParams> Effects;
    /// <summary>行動補正能力によって追加された効果</summary>
    public List<EffectParams> AdditionalEffects;

    public bool canCritical;
    public bool guaranteeCritical;
    public bool canDrain;

    /// <summary>与ダメージ増加倍率 (0.2 = +20%)</summary>
    public float BonusDMG;
    /// <summary>回復量増加倍率 (0.2 = +20%)</summary>
    public float BonusHeal;
    /// <summary>シールド量増加倍率 (0.2 = +20%)</summary>
    public float BonusShield;

    public ActionParams(CharacterModel owner, ActionSource source, CharacterModel target, List<EffectDefinition> effects)
    {
        Owner = owner;
        Source = source;
        Target = target;

        Effects = new List<EffectParams>();
        foreach(var effect in effects)
        {
            float value = effect.ValueSource == BaseValueSource.FixedValue ? effect.ValueRatio : owner.GetBaseValue(effect.ValueSource) * effect.ValueRatio;
            EffectParams effectParams = new EffectParams(effect.EffectType, value);

            //TODO:状態異常に対応
            Effects.Add(effectParams);
        }

        AdditionalEffects = new List<EffectParams>();

        canCritical = source == ActionSource.NormalAttack;
        guaranteeCritical = false;
        canDrain = source == ActionSource.NormalAttack;

        BonusDMG = 0;
        BonusHeal = 0;
        BonusShield = 0;
    }
}

public struct EffectParams
{
    public EffectType EffectType;
    public float Value;

    //状態異常付与効果のみ
    public StatusEffectDefinition StatusEffectToApply;

    public  EffectParams(EffectType effectType,float value, StatusEffectDefinition statusEffectDefinition = null)
    {
        EffectType = effectType;
        Value = value;
        StatusEffectToApply = statusEffectDefinition;
    }
}


