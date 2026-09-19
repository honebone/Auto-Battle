using System;
using UnityEngine;
using System.Collections.Generic;
public enum StatType
{
    MaxHealth, AttackPower, MagicPower, AttackSpeed, CastSpeed, CriticalRate, Drain,
}

public class CharacterModel
{
    public CharacterData Data { get; }

    public ClampedStatValue MaxHealth { get; }
    public StatValue AttackPower { get; }
    public StatValue MagicPower { get; }
    public StatValue AttackSpeed { get; }
    public StatValue CastSpeed { get; }
    public StatValue CriticalRate { get; }
    public StatValue Drain { get; }

    public float CurrentHpRatio => _hp / MaxHealth.FloatValue;
    private int _hp;
    private int _shield;
    private float _sp;

    public int HP => _hp;
    public int Shield => _shield;
    public float SP => _sp;

    private IBattleField _battleField;

    public CharacterModel(CharacterData data, IBattleField battleField)
    {
        Data = data;
        _battleField = battleField;

        MaxHealth = new ClampedStatValue(data.BaseMaxHealth, 1);
        AttackPower = new StatValue(data.BaseAttackPower);
        MagicPower = new StatValue(data.BaseMagicPower);
        AttackSpeed = new StatValue(data.BaseAttackSpeed);
        CastSpeed = new StatValue(data.BaseCastSpeed);
        CriticalRate = new StatValue(data.BaseCriticalRate);
        Drain = new StatValue(data.BaseDrain);

        //foreach (var passiveDefinition in data.PassiveSkills)
        //{
        //    var instance = passiveDefinition.CreateInstance();
        //    instance.Initialize(this);
        //    _passives.Add(instance);
        //}
    }

    public StatValue GetStat(StatType type) => type switch
    {
        StatType.MaxHealth => MaxHealth,
        StatType.AttackPower => AttackPower,
        StatType.MagicPower => MagicPower,
        StatType.AttackSpeed => AttackSpeed,
        StatType.CastSpeed => CastSpeed,
        StatType.CriticalRate => CriticalRate,
        StatType.Drain => Drain,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public float GetBaseValue(BaseValueSource type) => type switch
    {
        BaseValueSource.AttackPower => AttackPower.FloatValue,
        BaseValueSource.MagicPower => MagicPower.FloatValue,
        BaseValueSource.MaxHealth => MaxHealth.FloatValue,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };


    /// <summary>
    /// ActionDefinitionをもとに自動で行動内容(ActionParams)を生成し実行
    /// </summary>
    /// <param name="targets"></param>
    /// <param name="actionSource"></param>
    /// <param name="actionDefinition"></param>
    public void PerformActionsFromDefinition(ActionSource actionSource, ActionDefinition actionDefinition)
    {
        GetTargets(actionDefinition.TargetRule).ForEach(target =>
        {
            ActionParams action = new ActionParams(this, actionSource, target, actionDefinition.Effects);
            PerformAction(action);
        });
    }

    public void PerformNormalAttack()
    {
        PerformActionsFromDefinition(ActionSource.NormalAttack, Data.NomalAttackDefinition);
    }

    public virtual void PerformActiveSkill()
    {
        PerformActionsFromDefinition(ActionSource.ActiveSkill, Data.ActiveSkillDefinition);
    }

    public void PerformAction(ActionParams actionParams)
    {
        //TODO:効果補正
        //TODO:実行
    }

    private protected List<CharacterModel> GetTargets(TargetRule targetRule) => _battleField.GetTargets(this, targetRule);
}
