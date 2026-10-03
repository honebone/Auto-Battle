using System;
using UnityEngine;

/// <summary>キャラクターのステータス補正値</summary>
[Serializable]
public class CharaStatusMod
{
    [Header("倍率補正 (0.2 = +20%)")]
    public float MaxHealthMul;
    public float AttackPowerMul;
    public float MagicPowerMul;
    public float AttackSpeedMul;
    public float CastSpeedMul;

    [Header("実数補正 (0.1 = 10%)")]
    public float CriticalChance;
    public float Drain;

    public bool Stun;
    public bool Bind;
    public bool Fear;
}
