using System;
using UnityEngine;

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

    public CharacterModel(CharacterData data)
    {
        Data = data;

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

    public void CreateNormalAttack()
    {
        CharacterModel target = null;//TODO:‘ÎÛ‚ğ‚Á‚Ä‚­‚é
        ActionParams action = new ActionParams(this, target, ActionSource.NormalAttack, Data.NomalAttackDefinition);
        //TODO ‚±‚ê‚ğ‚Ç‚¤‚·‚é‚Ì
    }

    public virtual void CreateActiveSkill()
    {
        CharacterModel target = null;//TODO:‘ÎÛ‚ğ‚Á‚Ä‚­‚é
        ActionParams action = new ActionParams(this, target, ActionSource.ActiveSkill, Data.ActiveSkillDefinition);
        //TODO ‚±‚ê‚ğ‚Ç‚¤‚·‚é‚Ì
    }
}
