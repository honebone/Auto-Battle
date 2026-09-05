using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 単一ステータス値を保持する汎用クラス
/// 最終値 = (基礎値 + 実数増加量) * 倍率
/// </summary>
public class StatValue
{
    private float _base;
    private float _flatBonus;
    private float _multiplier;
    private readonly bool _hasMultiplier;

    public virtual float FloatValue =>
        _hasMultiplier ? (_base + _flatBonus) * _multiplier : (_base + _flatBonus);
    public int IntValue => (int)FloatValue;

    public StatValue(float baseValue, bool hasMultiplier = true)
    {
        _base = baseValue;
        _flatBonus = 0f;
        _multiplier = 1f;
        _hasMultiplier = hasMultiplier;
    }

    /// <summary>基礎値変動</summary>
    public void AddBase(float amount) { _base += amount; }

    /// <summary>実数増加</summary>
    public void AddFlat(float amount) { _flatBonus += amount; }
    public void RemoveFlat(float amount) { _flatBonus -= amount; }

    /// <summary>倍率変動 (+0.2f = 20%増, -0.5f = 50%減)</summary>
    public void AddMultiplier(float amount) { _multiplier += amount; }
    public void RemoveMultiplier(float amount) { _multiplier -= amount; }
}

public class ClampedStatValue : StatValue
{
    public float MinValue { get; }

    /// <summary>
    /// コンストラクタで基礎値と、下回らせたくない最小値を指定
    /// </summary>
    public ClampedStatValue(float baseValue, float minValue = 0f, bool hasMultiplier = true)
        : base(baseValue, hasMultiplier)
    {
        MinValue = minValue;
    }

    /// <summary>
    /// 計算された最終値と最小値を比較し、大きい方を返す
    /// </summary>
    public override float FloatValue => Mathf.Max(MinValue, base.FloatValue);
}