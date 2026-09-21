using System;
using test;
using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

public enum EffectType
{
    PhysicalAttack,   // 物理攻撃(攻撃力依存)
    MagicAttack,       // 魔法攻撃(魔力依存)
    Heal,               // 回復
    ShieldGrant,        // シールド付与
    SpChange,           // SP増加/減少
    StatusEffectApply,  // 状態異常の付与
}

public enum TargetRule
{
    FrontOpponent,
    BackOpponent,
    RandomOpponent,
    WeakestOpponent,

    FrontAlly,
    BackAlly,
    RandomAlly,
    WeakestAlly,

    Self,
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

/// <summary>
/// 行動の定義
/// データ駆動で行動を定義したい場合に活用
/// </summary>
[Serializable]
public class ActionDefinition
{
    public TargetRule TargetRule;
    public List<EffectDefinition> Effects;
}

/// <summary>
/// 効果1つの「定義」。通常攻撃・アクティブスキル・データ駆動パッシブが保持する。
/// SO化はせず、各スキル定義に埋め込むシリアライズ可能なデータとして扱う。
/// </summary>
[Serializable]
public class EffectDefinition
{
    public EffectType EffectType;
    public BaseValueSource ValueSource;
    [Tooltip("ValueSourceに対する倍率(%)")]
    public float ValueRatio = 100f;

    [EnableIf("ActionType", EffectType.StatusEffectApply)]
    public StatusEffectDefinition StatusEffectToApply;
}
