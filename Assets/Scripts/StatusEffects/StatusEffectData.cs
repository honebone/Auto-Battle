using UnityEngine;

public enum StackPolicy
{
    //発生源に関わらずスタックを加算
    Shared,
    //発生源ごとに個別に管理(毒など持続ダメージ系)
    PerSource,
}

/// <summary>
/// 状態異常の定義。PassiveDataの補正値は、状態異常が存在する間(スタック数によらず)固定で反映される。
/// スタック比例の補正が必要な場合はStEData_StackScaledStatを使う。
/// </summary>
[CreateAssetMenu(menuName = "StatusEffectData/Basic")]
public class StatusEffectData : PassiveData
{
    [Min(1)] public int MaxStack = 1;
    [Header("falseならデバフ")]
    public bool IsBuff;
    [Header("消去不可か (バフデバフ除去効果を実装するときよう)")]
    public bool Undeletable;
    [Header("発生源が異なる場合のスタックの扱い")]
    public StackPolicy StackPolicy = StackPolicy.Shared;

    public virtual StatusEffectModel CreateStatusEffectModel(CharacterModel owner, CharacterModel source, IBattleField battleField)
    {
        return new StatusEffectModel(owner, source, this, battleField);
    }
}
