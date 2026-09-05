using System.Collections.Generic;
using UnityEngine;

public enum RoleType { Fighter, Tank, Dealer, Support, None } // 敵はNoneを想定
public enum FrontlineType { Front, Back, None } // プレイヤーキャラのみ有効。敵はNone

/// <summary>
/// キャラクターの静的な設計データ。インスペクタから編集する。
/// </summary>
[CreateAssetMenu(menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string CharacterName;
    public RoleType Role;
    public FrontlineType Frontline;

    public float BaseMaxHealth;
    public float BaseAttackPower;
    public float BaseMagicPower;
    public float BaseAttackSpeed;
    public float BaseCastSpeed;
    public float BaseCriticalRate;
    public float BaseDrain;

    public SkillDefinition NormalAttackDefinition;
    public SkillDefinition ActiveSkillDefinition;
    public List<PassiveSkillDefinition> PassiveSkills;
    public List<SynergyTag> Tags;
}
