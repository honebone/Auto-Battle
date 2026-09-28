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

public delegate void ActionParamsHandler(ref ActionParams param);

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

    private float _shieldTimer;

    public bool IsPlayer => _isPlayer;
    private bool _isPlayer;
    public int Position => _position;
    private int _position;

    /// <summary>ログ表示用の名前 例:[P前]騎士</summary>
    public string DisplayName => $"[{(_isPlayer ? "P" : "E")}{(_position == 0 ? "前" : "後")}]{Data.CharacterName}";

    public event ActionParamsHandler ModifyAction;

    private IBattleField _battleField;
    private PassiveModel _passiveSkill;
    //TODO:アイテムのPassiveModelの管理
    /// <summary>付与されている状態異常。同じ種類でも発生源ごとに別インスタンス。再現性のため順序が確定するListで持つ</summary>
    private readonly List<StatusEffectModel> _statusEffects = new();
    public IReadOnlyList<StatusEffectModel> StatusEffects => _statusEffects;

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

        // パッシブは取り外されないため、生成時に有効化して以降は常に有効のままにする
        // HPの初期値に最大体力補正を反映させるため、HPの初期化より前に行う
        if (data.PassiveSkillData != null)
        {
            _passiveSkill = data.PassiveSkillData.CreateModel(this, battleField);
            _passiveSkill.Init();
        }

        _hp = new(MaxHealth.IntValue);
        _shield = new(0);
        _ap = new(0);
        _naTimer = new(0);
    }

    /// <summary>
    /// 戦闘開始時の初期化。HP等の戦闘用の値をリセットする
    /// </summary>
    public void InitBattle()
    {
        _hp.Value = MaxHealth.IntValue;
        _shield.Value = 0;
        _ap.Value = 0;
        _naTimer.Value = 0;
        _shieldTimer = 0;
    }

    /// <summary>
    /// 戦闘終了時の後処理。状態異常をすべて解除し、パッシブに戦闘終了を通知する
    /// </summary>
    public void EndBattle()
    {
        ClearStatusEffects();

        _passiveSkill?.OnBattleEnd();
    }

    /// <summary>
    /// キャラクターを完全に破棄する際の後処理(置き換え時など)。パッシブの補正・購読と状態異常をすべて解除する
    /// </summary>
    public void Dispose()
    {
        ClearStatusEffects();

        _passiveSkill?.Disable();
    }

    private void ClearStatusEffects()
    {
        foreach (var statusEffect in _statusEffects) statusEffect.Disable();
        _statusEffects.Clear();
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

        UpdateShieldLoss(deltaTime);

        _passiveSkill?.ManualUpdate(deltaTime);
        //TODO:アイテムのPassiveModelのManualUpdate

        // ManualUpdate中に状態異常が追加される可能性があるため、indexでループする
        for (int i = 0; i < _statusEffects.Count; i++) _statusEffects[i].ManualUpdate(deltaTime);
        RemoveExpiredStatusEffects();
    }

    /// <summary>
    /// 状態異常を付与する。同じ種類・同じ発生源のものが既にあればスタックを加算する
    /// 実際に増減したスタック数と、付与後のスタック数を返す
    /// </summary>
    public StatusEffectApplyResult ApplyStatusEffect(StatusEffectData data, CharacterModel source, int stack)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        StatusEffectModel statusEffect = _statusEffects.Find(s => s.Data == data && s.Source == source);
        if (statusEffect == null)
        {
            if (stack <= 0) return new StatusEffectApplyResult(data, 0, 0);

            statusEffect = data.CreateStatusEffectModel(this, source, _battleField);
            statusEffect.Init();
            _statusEffects.Add(statusEffect);
        }

        // スタックが0になっても、ここでは除去しない(反復中の除去を避けるため、ManualUpdate末尾でまとめて除去)
        int applied = statusEffect.ChangeStack(stack);
        return new StatusEffectApplyResult(data, applied, statusEffect.Stack);
    }

    /// <summary>スタックが0になった状態異常を解除・除去する</summary>
    private void RemoveExpiredStatusEffects()
    {
        for (int i = _statusEffects.Count - 1; i >= 0; i--)
        {
            if (!_statusEffects[i].IsExpired) continue;

            _statusEffects[i].Disable();
            _statusEffects.RemoveAt(i);
        }
    }

    /// <summary>シールドの自然減少。毎秒 最大体力×ShieldLossOvertime 分を1ずつ減少させる</summary>
    private void UpdateShieldLoss(float deltaTime)
    {
        if (_shield.Value <= 0)
        {
            _shieldTimer = 0;
            return;
        }

        float lossPerSecond = MaxHealth.FloatValue * Database.Instance.ShieldLossOvertime;
        if (lossPerSecond <= 0) return;

        float interval = 1f / lossPerSecond;
        _shieldTimer += deltaTime;

        int loss = 0;
        while (_shieldTimer >= interval && loss < _shield.Value)
        {
            _shieldTimer -= interval;
            loss++;
        }
        if (loss > 0) _shield.Value -= loss;
        if (_shield.Value <= 0) _shieldTimer = 0;
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

    ///// <summary> ActionDefinitionをもとに自動で行動内容(ActionParams)を生成 </summary>
    //public ActionParams CreateActionFromDefinition(ActionSource actionSource, ActionDefinition actionDefinition)
    //{
    //    if (actionDefinition?.Effects == null || actionDefinition.Effects.Count == 0) throw new ArgumentNullException(nameof(actionDefinition));

    //    CharacterModel target = GetTarget(actionDefinition.TargetRule);
    //    if (target == null) throw new ArgumentNullException(nameof(target));//対象が存在しない(相手が全滅している等)場合は行動しない

    //    return new ActionParams(this, actionSource, target, actionDefinition.Effects);
    //}

    /// <summary>
    /// ActionDefinitionをもとにActionParamsを自動生成
    /// ActionParamsを生成できた場合はtrueを返す
    /// </summary>
    public bool TryCreateActionFromDefinition(
        ActionSource actionSource,
        ActionDefinition actionDefinition,
        out ActionParams actionParams)
    {
        actionParams = default;

        if (actionDefinition == null) throw new ArgumentNullException(nameof(actionDefinition));

        if (actionDefinition.Effects == null || actionDefinition.Effects.Count == 0) return false;

        CharacterModel target = GetTarget(actionDefinition.TargetRule);

        // 対象が存在しない場合は行動しない
        if (target == null) return false;

        actionParams = new ActionParams(
            this,
            actionSource,
            target,
            actionDefinition.Effects
        );

        return true;
    }

    /// <summary>ActionDefinitionをもとに自動で行動内容(ActionParams)を生成し実行</summary>
    public void PerformActionsFromDefinition(ActionSource actionSource, ActionDefinition actionDefinition)
    {
        if (TryCreateActionFromDefinition(actionSource, actionDefinition, out var actionParams)) PerformAction(actionParams);
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
        ModifyAction?.Invoke(ref actionParams);


        CharacterModel target = actionParams.Target;
        DamageResult damageResult = new DamageResult(0, 0, false);
        Vector2Int heal = Vector2Int.zero;
        int shield = 0;
        float ap = 0;
        int statusEffectStack = 0;
        List<StatusEffectApplyResult> statusEffects = null;

        //TODO:シード値をもとにした乱数の使用 攻撃しない場合はクリティカル判定処理をしない(無駄に乱数を使わないように)
        bool isCritical = actionParams.guaranteeCritical || (actionParams.canCritical && CriticalChance.FloatValue.Dice());
        foreach (var effect in actionParams.Effects)
        {
            float value = effect.Value;

            switch (effect.EffectType)
            {
                case EffectType.Attack:

                    float dmg = (value * (1f + actionParams.BonusDMG));
                    if (isCritical) dmg *= 1f + CriticalDamage.FloatValue;
                    DamageResult dResult = target.TakeDamage(dmg.ToInt());
                    damageResult.HPDMG += dResult.HPDMG;
                    damageResult.ShieldDMG += dResult.ShieldDMG;
                    damageResult.Killed |= dResult.Killed;
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
                    if (effect.StatusEffectToApply == null)
                    {
                        Debug.LogWarning($"{DisplayName}: 付与する状態異常が設定されていません");
                        break;
                    }
                    StatusEffectApplyResult steResult = target.ApplyStatusEffect(effect.StatusEffectToApply, actionParams.Owner, (int)value);
                    statusEffectStack += steResult.AppliedStack;
                    statusEffects ??= new List<StatusEffectApplyResult>();
                    statusEffects.Add(steResult);
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
            ap,
            statusEffectStack,
            statusEffects);

        _battleField.NotifyActionPerformed(result);

        if (result.ActionSource == ActionSource.NormalAttack) _battleField.InvokeTriggerAction(TriggerType.OnNormalAttackDealt, result);
        if (result.ActionSource == ActionSource.ActiveSkill) _battleField.InvokeTriggerAction(TriggerType.OnActiveSkillCast, result);
        if (result.DealtDamage()) _battleField.InvokeTriggerAction(TriggerType.OnDamageDealt, result);
        if (result.Healed()) _battleField.InvokeTriggerAction(TriggerType.OnHealDealt, result);
        if (result.Shield > 0) _battleField.InvokeTriggerAction(TriggerType.OnShieldGranted, result);
        if (result.StatusEffectStack > 0) _battleField.InvokeTriggerAction(TriggerType.OnStatusEffectApplied, result);
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

public struct StatusEffectApplyResult
{
    public StatusEffectData Data;
    /// <summary>実際に増減したスタック数(最大スタックで頭打ちの場合は0もありうる)</summary>
    public int AppliedStack;
    /// <summary>付与後のスタック数</summary>
    public int CurrentStack;

    public StatusEffectApplyResult(StatusEffectData data, int appliedStack, int currentStack)
    {
        Data = data;
        AppliedStack = appliedStack;
        CurrentStack = currentStack;
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
    /// <summary>付与した状態異常のスタック数の合計</summary>
    public int StatusEffectStack;
    /// <summary>状態異常ごとの付与結果(付与が無ければnull)</summary>
    public List<StatusEffectApplyResult> StatusEffects;

    public ActionResult(
        CharacterModel owner,
        CharacterModel target,
        ActionSource actionSource,
        DamageResult damageResult,
        bool isCritical,
        int heal,
        int overHeal,
        int shield,
        float ap,
        int statusEffectStack = 0,
        List<StatusEffectApplyResult> statusEffects = null)
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
        StatusEffectStack = statusEffectStack;
        StatusEffects = statusEffects;
    }

    public bool DealtDamage() => HPDMG > 0 || ShieldDMG > 0;
    public bool Healed() => Heal > 0 || OverHeal > 0;
}
