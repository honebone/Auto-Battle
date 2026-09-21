using System.Collections.Generic;
using UnityEngine;

public enum FrontlineType { Front, Back, None } // プレイヤーキャラのみ有効。敵はNone

/// <summary>
/// キャラクターの静的な設計データ。インスペクタから編集する。
/// </summary>
[CreateAssetMenu(menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string CharacterName;
    public FrontlineType Frontline;

    public float BaseMaxHealth;
    public float BaseAttackPower;
    public float BaseMagicPower;
    public float BaseAttackSpeed;
    public float BaseCastSpeed;
    public float BaseCriticalChance;
    public float BaseDrain;

    public ActionDefinition NomalAttackDefinition;
    public ActionDefinition ActiveSkillDefinition;
    //public List<SynergyTag> Tags;
}
