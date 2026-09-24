using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// パッシブが購読するトリガーの種類。
/// 時間経過に関するもの(毎秒等)はここに含めず、ManualUpdateで個別実装する。
/// </summary>
public enum TriggerType
{
    CombatStart,

    OnNormalAttackDealt,
    OnActiveSkillCast,

    OnDamageDealt,
    //OnDamageReceived,

    OnHealDealt,
    //OnHealReceived,

    OnShieldGranted,
    //OnShieldReceived,

    OnStatusEffectApplied,
    //OnStatusEffectReceived,

    OnKilled,
    //OnDied,

    NoAction = CombatStart,
    //Active = OnNormalAttackDealt | OnActiveSkillCast | OnDamageDealt | OnHealDealt | OnShieldGranted | OnStatusEffectApplied | OnKilled, //何かをした
    //Passive = OnDamageReceived | OnHealReceived | OnShieldReceived | OnStatusEffectApplied | OnDied, //何かをされた
}

public enum TriggerObserveTargetType
{
    Self,//自分自身

    Anyone,//誰でも

    AnyAllies,//自身を含む味方
    OtherAlly,//自分ではない方の味方

    AnyOpponents//敵
}

[CreateAssetMenu(menuName = "PassiveData")]
public class PassiveData : ScriptableObject
{
    public string PassiveName;

    [Header("倍率補正 (0.2 = +20%)")]
    public float MaxHealthMul;
    public float AttackPowerMul;
    public float MagicPowerMul;
    public float AttackSpeedMul;
    public float CastSpeedMul;

    [Header("実数補正 (0.1 = 10%)")]
    public float CriticalChance;
    public float Drain;

    [Header("以下データ駆動用")]
    public bool AutoSubscribe;
    public TriggerType TriggerType;
    public TriggerObserveTargetType ObserveTargetType;
    [Header("行動の発動者をチェックするか\ntrue:したとき false:されたとき")]
    public bool ObserveActionOwner;
    public ActionDefinition ActionDefinition;

    public virtual PassiveModel CreateModel(CharacterModel owner, IBattleField battleField) { return new PassiveModel(owner, this, battleField); }
}
