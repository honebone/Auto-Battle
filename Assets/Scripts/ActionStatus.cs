using NaughtyAttributes;
using System.Collections.Generic;
using test;
using UnityEngine;

public enum ActionSource { Other, NormalAttack, ActiveSkill, PassiveSkill, Item, StatusEffect }

public struct ActionParams
{
    public CharacterModel Owner;
    public CharacterModel Target;
    public ActionSource Source;

    public List<EffectParams> Effects;
    /// <summary>行動補正能力によって追加された効果</summary>
    public List<EffectParams> AdditionalEffects;

    /// <summary>与ダメージ増加(%)</summary>
    public float BonusAllDMG;
    public float BonusPhysicalDMG;
    public float BonusMagicDMG;
    public float BonusHeal;
    public float BonusShield;

    public ActionParams(CharacterModel owner, CharacterModel target, ActionSource source, ActionDefinition definition)
    {
        Owner = owner;
        Target = target;
        Source = source;

        Effects = new List<EffectParams>();
        foreach(var action in definition.Actions)
        {
            float value = action.ValueSource == BaseValueSource.FixedValue ? action.ValueRatio : owner.GetBaseValue(action.ValueSource) * action.ValueRatio / 100f;
            EffectParams effect = new EffectParams(action.EffectType, value);

            //TODO:状態異常に対応
            Effects.Add(effect);
        }

        AdditionalEffects = new List<EffectParams>();

        BonusAllDMG = 0;
        BonusPhysicalDMG = 0;
        BonusMagicDMG = 0;
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


