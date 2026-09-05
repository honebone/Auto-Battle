using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using R3; // トリガー通知(Subject<TriggerContext>)で使用想定

// =====================================================================
// 列挙型
// =====================================================================

public enum ActionType
{
    PhysicalAttack,   // 物理攻撃(攻撃力依存)
    MagicAttack,       // 魔法攻撃(魔力依存)
    Heal,               // 回復
    ShieldGrant,        // シールド付与
    SpChange,           // SP増加/減少
    StatusEffectApply,  // 状態異常の付与
}

/// <summary>
/// ActionSpecの基礎値の参照元。
/// レアケース(スタック数依存ダメージ等)はここに含めず、
/// ActionResolver.ResolveWithExplicitValueを個別クラスから直接呼ぶ形で対応する。
/// </summary>
public enum BaseValueSource
{
    AttackPower,  // 自身の攻撃力 × ValueRatio
    MagicPower,   // 自身の魔力 × ValueRatio
    MaxHealth,    // 自身の最大体力 × ValueRatio
    FixedValue,   // ValueRatioをそのまま固定値として使用
}

public enum TargetRule
{
    FrontPriority,  // 前衛優先(前衛不在なら後衛)
    BackPriority,   // 後衛優先
    AllEnemies,     // 敵全体
    RandomEnemy,    // ランダムな敵単体
    Self,           // 自分自身
    // TODO: 「HPが最も低い味方」等、サポート系スキルに必要なルールを別途洗い出す
}

/// <summary>
/// パッシブスキル・状態異常が購読するトリガーの種類。
/// 時間経過に関するもの(毎秒等)はここに含めず、ManualUpdateで個別実装する。
/// </summary>
public enum TriggerType
{
    CombatStart,
    RoundStart,
    RoundEnd,
    OnNormalAttackDealt,
    OnNormalAttackReceived,
    OnActiveSkillCastSelf,
    OnActiveSkillCastAlly,
    OnActiveSkillCastEnemy,
    OnDamageDealt,
    OnDamageReceived,
    OnHealDealt,
    OnHealReceived,
    OnStatusEffectApplied,
    OnStatusEffectReceived,
    OnHpBelowThreshold,
    OnAllyDefeated,
    OnSelfDefeated,
}

/// <summary>
/// シナジータグ。具体的な分類は未定なので仮のもの。
/// </summary>
public enum SynergyTag
{
    CriticalFocus,
    NormalAttackFocus,
    // TODO: 分類確定後に追加
}

public enum StatType
{
    Health, AttackPower, MagicPower, AttackSpeed, CastSpeed, CriticalRate, Drain,
}

// =====================================================================
// ActionSpec / ActionContext / ActionResult
// =====================================================================

/// <summary>
/// 行動の「定義」。通常攻撃・アクティブスキル・データ駆動パッシブが保持する。
/// SO化はせず、各スキル定義に埋め込むシリアライズ可能なデータとして扱う。
/// </summary>
[Serializable]
public class ActionSpec
{
    public ActionType ActionType;
    public BaseValueSource ValueSource;
    [Tooltip("ValueSourceに対する倍率。例:1.2なら120%、FixedValueの場合はそのまま数値")]
    public float ValueRatio = 1f;
    public TargetRule TargetRule;

    // ActionType.StatusEffectApply の場合のみ使用
    public StatusEffectDefinition StatusEffectToApply;
    public int StatusEffectStacks;
}

/// <summary>
/// 解決パイプラインの途中経過を保持する。対象確定後、補正フェーズで書き換えられる。
/// </summary>
public class ActionContext
{
    public CharacterModel Source;
    public CharacterModel Target;
    public ActionType ActionType;
    public float BaseValue;
    public bool IsCritical;

    // 補正の集計。固定加算→割合加算(合算後1回乗算)→倍率(重ねがけ)の順で反映する
    public float FlatBonus;
    public float PercentBonus;
    public float MultiplierProduct = 1f;

    public IReadOnlyList<SynergyTag> Tags;

    public float ValueBeforeCritical => (BaseValue + FlatBonus) * (1f + PercentBonus) * MultiplierProduct;

    public bool DoesAttack => ActionType == ActionType.PhysicalAttack || ActionType == ActionType.MagicAttack;
}

/// <summary>
/// 行動が解決した結果。統計収集(デバッグ機能.md参照)や、
/// 「ダメージを与えた時」等の後続トリガーはこれを購読して処理する。
/// </summary>
public readonly struct ActionResult
{
    public readonly CharacterModel Source;
    public readonly CharacterModel Target;
    public readonly ActionType ActionType;
    public readonly float Value;
    public readonly bool IsCritical;

    public ActionResult(CharacterModel source, CharacterModel target, ActionType actionType, float value, bool isCritical)
    {
        Source = source;
        Target = target;
        ActionType = actionType;
        Value = value;
        IsCritical = isCritical;
    }
}

// =====================================================================
// ActionModifier (案A′: 補正はキャラクター自身に紐づける)
// =====================================================================

/// <summary>
/// CharacterModel.SelfModifiers に登録される、自分自身の行動にのみ影響する補正。
/// 他キャラのSelfModifiersは参照しない。
/// </summary>
public abstract class ActionModifier
{
    public abstract bool AppliesTo(ActionContext context);
    public abstract void Apply(ActionContext context);
}

/// <summary>
/// 例:「自身のすべての攻撃において、対象HPが一定割合以下ならダメージ+X%」
/// </summary>
public class TargetLowHpDamageBoostModifier : ActionModifier
{
    private readonly float _hpThresholdRatio;
    private readonly float _bonusPercent;

    public TargetLowHpDamageBoostModifier(float hpThresholdRatio, float bonusPercent)
    {
        _hpThresholdRatio = hpThresholdRatio;
        _bonusPercent = bonusPercent;
    }

    public override bool AppliesTo(ActionContext context)
        => context.DoesAttack
           && context.Target != null
           && context.Target.CurrentHpRatio <= _hpThresholdRatio;

    public override void Apply(ActionContext context) => context.PercentBonus += _bonusPercent;
}

// =====================================================================
// ActionResolver
// =====================================================================

public class ActionResolver
{
    // TODO: R3のSubject<ActionResult>を想定。統計収集・OnDamageDealt等のトリガー発火に使う
    // private readonly Subject<ActionResult> _onActionResolved = new();
    // public IObservable<ActionResult> OnActionResolved => _onActionResolved;

    /// <summary>通常経路。ActionSpecの定義から基礎値・対象を算出してパイプラインへ流す。</summary>
    public ActionResult Resolve(ActionSpec spec, CharacterModel source, bool? forcedCritical = null)
    {
        var target = SelectTarget(spec.TargetRule, source);
        var baseValue = CalcBaseValue(spec, source);
        return ResolveWithExplicitValue(spec.ActionType, baseValue, source, target, forcedCritical,
            spec.StatusEffectToApply, spec.StatusEffectStacks);
    }

    /// <summary>
    /// 基礎値を呼び出し元(状態異常の個別実装クラス等)が計算済みの場合の経路。
    /// 対象確定後の共通処理(補正?適用?結果発行)はここに集約する。
    /// </summary>
    public ActionResult ResolveWithExplicitValue(
        ActionType type, float baseValue, CharacterModel source, CharacterModel target,
        bool? forcedCritical = null, StatusEffectDefinition statusEffect = null, int statusStacks = 0)
    {
        var ctx = new ActionContext
        {
            Source = source,
            Target = target,
            ActionType = type,
            BaseValue = baseValue,
        };
        ctx.IsCritical = forcedCritical ?? RollCritical(source, type);

        // 対象確定後、Source自身の補正のみを適用する(案A′)
        foreach (var modifier in source.SelfModifiers)
        {
            if (modifier.AppliesTo(ctx)) modifier.Apply(ctx);
        }

        float finalValue = ctx.ValueBeforeCritical;
        if (ctx.IsCritical && (type == ActionType.PhysicalAttack || type == ActionType.MagicAttack))
            finalValue *= 2f; // TODO: クリティカル倍率は現状固定2倍。ステータス化するかは検討中

        Apply(type, source, target, finalValue, statusEffect, statusStacks);

        var result = new ActionResult(source, target, type, finalValue, ctx.IsCritical);
        // _onActionResolved.OnNext(result);
        return result;
    }

    /// <summary>
    /// 通常攻撃(基本ActionSpec + パッシブ由来の追加ActionSpec)をまとめて1解決単位として処理する。
    /// クリティカル判定はこの単位で1回のみ行い、全成分に反映する。
    /// ドレインは全成分の合計ダメージに対して適用する。
    /// </summary>
    public void ResolveNormalAttack(CharacterModel source)
    {
        var specs = source.GetNormalAttackActionSpecs();
        if (specs.Count == 0) return;

        var target = SelectTarget(specs[0].TargetRule, source); // 通常攻撃は単一ターゲット前提
        bool isCritical = RollCritical(source, ActionType.PhysicalAttack);

        float totalDamage = 0f;
        foreach (var spec in specs)
        {
            var baseValue = CalcBaseValue(spec, source);
            var result = ResolveWithExplicitValue(spec.ActionType, baseValue, source, target, isCritical,
                spec.StatusEffectToApply, spec.StatusEffectStacks);

            if (spec.ActionType == ActionType.PhysicalAttack || spec.ActionType == ActionType.MagicAttack)
                totalDamage += result.Value;
        }

        if (totalDamage > 0f && source.Drain.FloatValue > 0f)
        {
            float healAmount = totalDamage * source.Drain.FloatValue / 100f;
            ResolveWithExplicitValue(ActionType.Heal, healAmount, source, source);
        }

        // TODO: 通常攻撃回数の統計は「解決1回」につき+1でカウントする(成分数によらない)
    }

    private float CalcBaseValue(ActionSpec spec, CharacterModel source)
    {
        return spec.ValueSource switch
        {
            BaseValueSource.AttackPower => source.AttackPower.FloatValue * spec.ValueRatio,
            BaseValueSource.MagicPower => source.MagicPower.FloatValue * spec.ValueRatio,
            BaseValueSource.MaxHealth => source.MaxHealth.FloatValue * spec.ValueRatio,
            BaseValueSource.FixedValue => spec.ValueRatio,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private bool RollCritical(CharacterModel source, ActionType type)
    {
        if (type != ActionType.PhysicalAttack && type != ActionType.MagicAttack) return false;
        return UnityEngine.Random.value < source.CriticalRate.FloatValue; // TODO: 乱数ストリームを用途別に分離する(デバッグ機能.md参照)
    }

    private CharacterModel SelectTarget(TargetRule rule, CharacterModel source)
    {
        // TODO: 実際の対象選択には敵/味方リスト(Battlefield相当)への参照が必要。
        // CharacterModelにIBattlefield等を持たせて委譲する設計を別途検討する。
        throw new NotImplementedException();
    }

    private void Apply(ActionType type, CharacterModel source, CharacterModel target, float value,
        StatusEffectDefinition statusEffect, int statusStacks)
    {
        switch (type)
        {
            case ActionType.PhysicalAttack:
            case ActionType.MagicAttack:
                target.TakeDamage((int)value); // シールド→HPの順で減少させる処理を内部で行う想定
                break;
            case ActionType.Heal:
                target.Heal((int)value);
                break;
            case ActionType.ShieldGrant:
                target.GrantShield((int)value);
                break;
            case ActionType.SpChange:
                target.ChangeSp(value);
                break;
            case ActionType.StatusEffectApply:
                target.ApplyStatusEffect(statusEffect, source, statusStacks);
                break;
        }
    }
}