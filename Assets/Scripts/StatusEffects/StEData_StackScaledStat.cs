using UnityEngine;

/// <summary>
/// スタック数に比例してステータスを補正する状態異常。PassiveDataの補正値は「1スタックあたり」として扱う。
/// 例: 1スタックにつき攻撃速度+5%
/// </summary>
[CreateAssetMenu(menuName = "StatusEffectData/StackScaledStat")]
public class StEData_StackScaledStat : StatusEffectData
{
    public override StatusEffectModel CreateStatusEffectModel(CharacterModel owner, CharacterModel source, IBattleField battleField)
    {
        return new StEModel_StackScaledStat(owner, source, this, battleField);
    }
}

public class StEModel_StackScaledStat : StatusEffectModel
{
    public StEModel_StackScaledStat(CharacterModel owner, CharacterModel source, StEData_StackScaledStat data, IBattleField battleField) : base(owner, source, data, battleField) { }

    // 生成時は0スタックなので補正は反映しない
    public override void Init() => Subscribe();

    public override void Disable()
    {
        ApplyStatusModifier(-Stack);
        Unsubscribe();
    }

    private protected override void OnStackChanged(int changed) => ApplyStatusModifier(changed);
}
