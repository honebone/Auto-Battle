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
    public StatValue CriticalDamage { get; }
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

    /// <summary>ログ表示用の名前 例:[P前]騎士</summary>
    public string DisplayName => $"[{(_isPlayer ? "P" : "E")}{(_position == 0 ? "前" : "後")}]{Data.CharacterName}";

    private IBattleField _battleField;
    private PassiveModel _passiveSkill;
    //TODO:アイテム、状態異常のPassiveModelの管理

    public CharacterModel(CharacterData data, IBattleField battleField, CharaContext context)
    {
        Data = data;
        _battleField = battleField;

        MaxHealth = new ClampedStatValue(data.BaseMaxHealth, 1);
        AttackPower = new StatValue(data.BaseAttackPower);
        MagicPower = new StatValue(data.BaseMagicPower);
        AttackSpeed = new StatValue(data.BaseAttackSpeed);
        CastSpeed = new StatValue(data.BaseCastSpeed);
        CriticalChance = new StatValue(Database.Instance.BaseCriticalChance);
        CriticalDamage = new StatValue(Database.Instance.BaseCriticalDamageRate);
        Drain = new StatValue(data.BaseDrain);

        _isPlayer = context.IsPlayer;
        _position = context.Position;

        _hp = new(MaxHealth.IntValue);
        _shield = new(0);
        _ap = new(0);
        _naTimer = new(0);

        if (data.PassiveSkillData != null)
        {
            _passiveSkill = data.PassiveSkillData.CreateModel(this, battleField);
        }
    }

    /// <summary>
    /// 戦闘開始時の初期化。パッシブの補正を反映した後にHP等をリセットする
    /// </summary>
    public void InitBattle()
    {
        _passiveSkill?.Init();

        _hp.Value = MaxHealth.IntValue;
        _shield.Value = 0;
        _ap.Value = 0;
        _naTimer.Value = 0;
    }

    /// <summary>
    /// 戦闘終了時の後処理。パッシブの補正・購読を解除する
    /// </summary>
    public void EndBattle()
    {
        _passiveSkill?.Disable();
    }

    public void ManualUpdate(float deltaTime)
    {
        if (!IsAlive) return;

        _naTimer.Value += deltaTime;
        if (_naTimer.Value >= 1f / AttackSpeed.FloatValue)
        {
            _naTimer.Value -= 1f / AttackSpeed.FloatValue;
            PerformNormalAttack();
        }

        if (!IsAlive) return;

        _ap.Value += CastSpeed.FloatValue * deltaTime;
        if (_ap.Value >= 100)
        {
            _ap.Value -= 100;
            PerformActiveSkill();
        }

        //TODO:シールドの自然現象

        _passiveSkill?.ManualUpdate(deltaTime);
        //TODO:アイテム、状態異常のPassiveModelのManualUpdate
    }



    public DamageResult TakeDamage(int amount)
    {
        int remain = amount;
        if (remain <= 0) return new DamageResult(0, 0, false);

        bool wasAlive = IsAlive;

        int hpDamage = 0;
        int shieldDamage = 0;

        shieldDamage = DamageShield(remain);
        remain -= shieldDamage;
        if (remain > 0) hpDamage = DamageHP(remain);

        return new DamageResult(hpDamage, shieldDamage, wasAlive && !IsAlive);
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

    public Vector2Int Heal(int amount)
    {
        int heal = Mathf.Min(amount, MaxHealth.IntValue - _hp.Value);
        int overHeal = amount - heal;
        _hp.Value += heal;

        return new Vector2Int(heal, overHeal);
    }
    public int GrantShield(int amount)
    {
        int shield = Mathf.Min(amount, MaxHealth.IntValue - _shield.Value);
        _shield.Value += shield;

        return shield;
    }

    public float ChangeAP(float amount)
    {
        _ap.Value += amount;
        return amount;
    }

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
        if (actionDefinition?.Effects == null || actionDefinition.Effects.Count == 0) return;

        CharacterModel target = GetTarget(actionDefinition.TargetRule);
        if (target == null) return;//対象が存在しない(相手が全滅している等)場合は行動しない

        ActionParams action = new ActionParams(this, actionSource, target, actionDefinition.Effects);
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

    public ActionResult PerformAction(ActionParams actionParams)
    {
        //TODO:効果補正
        CharacterModel target = actionParams.Target;
        DamageResult damageResult = new DamageResult(0, 0, false);
        Vector2Int heal = Vector2Int.zero;
        int shield = 0;
        float ap = 0;
        //TODO:付与した状態異常を記録

        //TODO:クリティカル判定
        bool isCritical = false;
        foreach (var effect in actionParams.Effects)
        {
            float value = effect.Value;

            switch (effect.EffectType)
            {
                case EffectType.Attack://TODO:クリティカルによるダメージ量補正
                    DamageResult dmg = target.TakeDamage((value * (1f + actionParams.BonusDMG)).ToInt());
                    damageResult.HPDMG += dmg.HPDMG;
                    damageResult.ShieldDMG += dmg.ShieldDMG;
                    damageResult.Killed |= dmg.Killed;
                    break;
                case EffectType.Heal:
                    heal += target.Heal((value * (1f + actionParams.BonusHeal)).ToInt());
                    break;
                case EffectType.ShieldGrant:
                    shield += target.GrantShield((value * (1f + actionParams.BonusShield)).ToInt());
                    break;
                case EffectType.SpChange:
                    ap += target.ChangeAP(effect.Value);
                    break;
                case EffectType.StatusEffectApply:
                    Debug.Log("状態異常付与は未実装");
                    break;
            }
        }

        ActionResult result = new ActionResult(
            actionParams.Owner,
            target,
            actionParams.Source,
            damageResult,
            isCritical,
            heal.x,
            heal.y,
            shield,
            ap);

        _battleField.NotifyActionPerformed(result);

        if (result.ActionSource == ActionSource.NormalAttack) _battleField.InvokeTriggerAction(TriggerType.OnNormalAttackDealt, result);
        if (result.ActionSource == ActionSource.ActiveSkill) _battleField.InvokeTriggerAction(TriggerType.OnActiveSkillCast, result);
        if (result.DealtDamage()) _battleField.InvokeTriggerAction(TriggerType.OnDamageDealt, result);
        if (result.Healed()) _battleField.InvokeTriggerAction(TriggerType.OnHealDealt, result);
        if (result.Shield > 0) _battleField.InvokeTriggerAction(TriggerType.OnShieldGranted, result);
        //TODO:状態異常付与時
        if (result.Killed) _battleField.InvokeTriggerAction(TriggerType.OnKilled, result);


        return result;
    }

    private protected CharacterModel GetTarget(TargetRule targetRule) => _battleField.GetTarget(this, targetRule);
}

public struct DamageResult
{
    public int HPDMG;
    public int ShieldDMG;
    public bool Killed;

    public DamageResult(int hpDMG, int shieldDMG, bool killed)
    {
        HPDMG = hpDMG;
        ShieldDMG = shieldDMG;
        Killed = killed;
    }
}

public struct ActionResult
{
    public CharacterModel Owner;
    public CharacterModel Target;
    public ActionSource ActionSource;

    /// <summary>x:hpDMG y:shieldDMG</summary>
    public int HPDMG;
    public int ShieldDMG;
    public bool Killed;
    public bool IsCritical;
    public int Heal;
    public int OverHeal;
    public int Shield;
    public float AP;
    //TODO:付与した状態異常を記録

    public ActionResult(
        CharacterModel owner,
        CharacterModel target,
        ActionSource actionSource,
        DamageResult damageResult,
        bool isCritical,
        int heal,
        int overHeal,
        int shield,
        float ap)
    {
        Owner = owner;
        Target = target;
        ActionSource = actionSource;
        HPDMG = damageResult.HPDMG;
        ShieldDMG = damageResult.ShieldDMG;
        Killed = damageResult.Killed;
        IsCritical = isCritical;
        Heal = heal;
        OverHeal = overHeal;
        Shield = shield;
        AP = ap;
    }

    public bool DealtDamage() => HPDMG > 0 || ShieldDMG > 0;
    public bool Healed() => Heal > 0 || OverHeal > 0;
}
