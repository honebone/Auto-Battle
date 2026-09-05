using System;
using System.Collections.Generic;
using UnityEngine;
// using R3;

/// <summary>
/// 通常攻撃・アクティブスキルの定義。複数の行動を順に発生させるケース
/// (例:ダメージ→回復)を表現できるよう、ActionSpecのリストとして持つ。
/// </summary>
[Serializable]
public class SkillDefinition
{
    public List<ActionSpec> Actions;
}

/// <summary>
/// キャラクターのランタイム状態。CharacterDataから戦闘開始時に生成する。
/// </summary>
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

    public ActionResolver ActionResolver { get; }

    /// <summary>
    /// 自分自身の行動にのみ影響する補正のリスト(案A′)。
    /// 自身のパッシブ・装備アイテムのパッシブから登録される。他キャラのリストは参照しない。
    /// </summary>
    public List<ActionModifier> SelfModifiers { get; } = new();

    private readonly List<PassiveSkillInstance> _passives = new();
    private readonly Dictionary<(StatusEffectDefinition definition, CharacterModel source), StatusEffectInstance> _statusEffects = new();
    private readonly List<ActionSpec> _additionalNormalAttackActions = new();

    public float CurrentHpRatio => MaxHealth.FloatValue / Data.BaseMaxHealth; // TODO: 最大値の持たせ方(装備等で最大体力が変動する場合)を要検討

    private int _hp;
    private int _shield;
    private float _sp;

    public int HP => _hp;
    public int Shield => _shield;
    public float SP => _sp;

    public CharacterModel(CharacterData data, ActionResolver actionResolver)
    {
        Data = data;
        ActionResolver = actionResolver;

        MaxHealth = new ClampedStatValue(data.BaseMaxHealth,1);
        AttackPower = new StatValue(data.BaseAttackPower);
        MagicPower = new StatValue(data.BaseMagicPower);
        AttackSpeed = new StatValue(data.BaseAttackSpeed);
        CastSpeed = new StatValue(data.BaseCastSpeed);
        CriticalRate = new StatValue(data.BaseCriticalRate);
        Drain = new StatValue(data.BaseDrain);

        foreach (var passiveDefinition in data.PassiveSkills)
        {
            var instance = passiveDefinition.CreateInstance();
            instance.Initialize(this);
            _passives.Add(instance);
        }
    }

    public StatValue GetStat(StatType type) => type switch
    {
        StatType.Health => MaxHealth,
        StatType.AttackPower => AttackPower,
        StatType.MagicPower => MagicPower,
        StatType.AttackSpeed => AttackSpeed,
        StatType.CastSpeed => CastSpeed,
        StatType.CriticalRate => CriticalRate,
        StatType.Drain => Drain,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>フレームが進むごとに呼ばれる。パッシブ・状態異常の時間駆動処理を回す。</summary>
    public void ManualUpdate(float deltaTime)
    {
        foreach (var passive in _passives) passive.OnManualUpdate(deltaTime);
        foreach (var effect in _statusEffects.Values) effect.OnManualUpdate(deltaTime);
    }

    /// <summary>ラウンド開始時、HP/SP/シールドをリセットする。</summary>
    public void ResetForNewRound()
    {
        _hp = MaxHealth.IntValue;
        _shield = 0;
        _sp = 0;
    }

    /// <summary>戦闘終了時、状態異常はすべて解除する。</summary>
    public void OnCombatEnd()
    {
        foreach (var passive in _passives) passive.OnCombatEnd();
        foreach (var effect in new List<StatusEffectInstance>(_statusEffects.Values)) effect.OnRemove();
        _statusEffects.Clear();
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
        if (_shield <= 0) return 0;

        int shieldDMG = _shield > amount ? amount : _shield;
        _shield -= shieldDMG;

        return shieldDMG;
    }
    /// <summary>HPにダメージを与え、実際に減少した分を返す</summary>
    private int DamageHP(int amount)
    {
        if (amount <= 0 || _hp <= 0) return 0;

        int hpDMG = _hp > amount ? amount : _hp;
        _hp -= hpDMG;

        if (_hp <= 0)
        {
            //死亡
        }

        return hpDMG;
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Min(amount, MaxHealth.IntValue - _hp);
        _hp += heal;
    }
    public void GrantShield(int amount)
    {
        int shield = Mathf.Min(amount, MaxHealth.IntValue - _shield);
        _shield += shield;
    }
    public void ChangeSp(float amount) => _sp += amount; // TODO: 100到達時の消費・繰越有無は要検討

    public void ApplyStatusEffect(StatusEffectDefinition definition, CharacterModel source, int stacks)
    {
        var key = (definition, source);
        if (!_statusEffects.TryGetValue(key, out var instance))
        {
            instance = definition.CreateInstance(this, source);
            _statusEffects[key] = instance;
            instance.OnApply();
        }
        instance.AddStacks(stacks);
    }

    public void RemoveStatusEffect(StatusEffectInstance instance)
    {
        instance.OnRemove();
        foreach (var kvp in _statusEffects)
        {
            if (ReferenceEquals(kvp.Value, instance))
            {
                _statusEffects.Remove(kvp.Key);
                break;
            }
        }
    }

    /// <summary>通常攻撃の基本ActionSpec + パッシブ由来の追加ActionSpecをまとめて返す。</summary>
    public List<ActionSpec> GetNormalAttackActionSpecs()
    {
        var list = new List<ActionSpec>(Data.NormalAttackDefinition.Actions);
        list.AddRange(_additionalNormalAttackActions);
        return list;
    }

    /// <summary>
    /// 例:「通常攻撃は自身の最大体力X%分の魔法ダメージを追加で与える」というパッシブが、
    /// 取得/装備タイミングでここに登録する。
    /// </summary>
    public void RegisterAdditionalNormalAttackAction(ActionSpec spec) => _additionalNormalAttackActions.Add(spec);

    // TODO: R3で GetTriggerObservable(TriggerType) を実装する。
    // Dictionary<TriggerType, Subject<TriggerContext>> を持ち、各所からOnNextを呼ぶ形を想定。
}