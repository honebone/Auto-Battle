using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// パッシブが購読するトリガーの種類。
/// 時間経過に関するもの(毎秒等)はここに含めず、ManualUpdateで個別実装する。
/// </summary>
public enum TriggerType
{
    BattleStart,

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

    NoAction = BattleStart,
    //Active = OnNormalAttackDealt | OnActiveSkillCast | OnDamageDealt | OnHealDealt | OnShieldGranted | OnStatusEffectApplied | OnKilled, //何かをした
    //Passive = OnDamageReceived | OnHealReceived | OnShieldReceived | OnStatusEffectApplied | OnDied, //何かをされた
}

public enum CharaRelation
{
    Self,//自分自身

    Anyone,//誰でも

    AnyAllies,//自身を含む味方
    OtherAlly,//自分ではない方の味方

    AnyOpponents//敵
}

[CreateAssetMenu(menuName = "PassiveData/DataDriven")]
public class PassiveData : ScriptableObject
{
    public string PassiveName;
    public Sprite PassiveIcon;

    public CharaStatusMod StatusMod;

    [Header("以下データ駆動用")]
    public bool AutoSubscribe;
    public TriggerCondition TriggerCondition;
    public ActionDefinition ActionDefinition;

    public virtual PassiveModel CreateModel(CharacterModel owner, IBattleField battleField) { return new PassiveModel(owner, this, battleField); }
}
