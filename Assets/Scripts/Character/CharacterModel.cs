using R3;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
public enum StatType
{
    MaxHealth, AttackPower, MagicPower, AttackSpeed, CastSpeed, CriticalChance, Drain,
}

public struct CharaContext
{
    public bool IsPlayer;

    /// <summary>手前から0</summary>
    public int Position;
}

public class CharacterModel
{
    public CharacterData Data { get; }

    public ClampedStatValue MaxHealth { get; }
    public StatValue AttackPower { get; }
    public StatValue MagicPower { get; }
    public StatValue AttackSpeed { get; }
    public StatValue CastSpeed { get; }
    public StatValue CriticalChance { get; }
    public StatValue Drain { get; }

    public float CurrentHpRatio => _hp.Value / MaxHealth.FloatValue;
    public bool IsAlive => _hp.Value > 0;

    public ReadOnlyReactiveProperty<int> HP => _hp;
    private readonly ReactiveProperty<int> _hp;
    public ReadOnlyReactiveProperty<int> Shield => _shield;
    private readonly ReactiveProperty<int> _shield;
    public ReadOnlyReactiveProperty<float> AP => _ap;
    private readonly ReactiveProperty<float> _ap;
    //通常攻撃タイマー
    public ReadOnlyReactiveProperty<float> NATimer => _naTimer;
    private readonly ReactiveProperty<float> _naTimer;
    public bool IsPlayer => _isPlayer;
    private bool _isPlayer;
    public int Position => _position;
    private int _position;

    private IBattleField _battleField;


    public CharacterModel(CharacterData data, IBattleField battleField, CharaContext context)
    {
        Data = data;
        _battleField = battleField;

        MaxHealth = new ClampedStatValue(data.BaseMaxHealth, 1);
        AttackPower = new StatValue(data.BaseAttackPower);
        MagicPower = new StatValue(data.BaseMagicPower);
        AttackSpeed = new StatValue(data.BaseAttackSpeed);
        CastSpeed = new StatValue(data.BaseCastSpeed);
        CriticalChance = new StatValue(data.BaseCriticalChance);
        Drain = new StatValue(data.BaseDrain);

        _isPlayer = context.IsPlayer;
        _position = context.Position;

        _hp = new(MaxHealth.IntValue);
        _shield = new(0);
        _ap = new(0);

        //foreach (var passiveDefinition in data.PassiveSkills)
        //{
        //    var instance = passiveDefinition.CreateInstance();
        //    instance.Initialize(this);
        //    _passives.Add(instance);
        //}
    }

    public void ManualUpdate(float deltaTime)
    {
        _naTimer.Value += deltaTime;
        if(_naTimer.Value >= 1f / AttackSpeed.FloatValue)
        {
            _naTimer.Value -= 1f / AttackSpeed.FloatValue;
            PerformNormalAttack();
        }

        _ap.Value += deltaTime;
        if (_ap.Value >= 100)
        {
            _ap.Value -= 100;
            PerformActiveSkill();
        }
    }

    public void TakeDamage(int amount)
    {
        int remain = amount;
        if (remain <= 0) return;

        int hpDamage = 0;
        int shieldDamage = 0;

        shieldDamage = DamageShield(remain);
        remain -= shieldDamage;
        if (remain > 0) hpDamage = DamageHP(remain);
    }

    /// <summary>Shieldにダメージを与え、実際に減少した分を返す</summary>
    private int DamageShield(int amount)
    {
        if (_shield.Value <= 0) return 0;

        int shieldDMG = _shield.Value > amount ? amount : _shield.Value;
        _shield.Value -= shieldDMG;

        return shieldDMG;
    }
    /// <summary>HPにダメージを与え、実際に減少した分を返す</summary>
    private int DamageHP(int amount)
    {
        if (amount <= 0 || _hp.Value <= 0) return 0;

        int hpDMG = _hp.Value > amount ? amount : _hp.Value;
        _hp.Value -= hpDMG;

        if (_hp.Value <= 0)
        {
            //死亡
        }

        return hpDMG;
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Min(amount, MaxHealth.IntValue - _hp.Value);
        _hp.Value += heal;
    }
    public void GrantShield(int amount)
    {
        int shield = Mathf.Min(amount, MaxHealth.IntValue - _shield.Value);
        _shield.Value += shield;
    }

    public void ChangeAP(float amount) { _ap.Value += amount; }

    public StatValue GetStat(StatType type) => type switch
    {
        StatType.MaxHealth => MaxHealth,
        StatType.AttackPower => AttackPower,
        StatType.MagicPower => MagicPower,
        StatType.AttackSpeed => AttackSpeed,
        StatType.CastSpeed => CastSpeed,
        StatType.CriticalChance => CriticalChance,
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
        ActionParams action = new ActionParams(this, actionSource, GetTarget(actionDefinition.TargetRule), actionDefinition.Effects);
        PerformAction(action);
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

    private protected CharacterModel GetTarget(TargetRule targetRule) => _battleField.GetTarget(this, targetRule);
}
