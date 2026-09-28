using UnityEngine;

/// <summary>
/// 状態異常のランタイムインスタンス。同じ種類でも発生源(Source)ごとに別インスタンスとして管理する。
/// スタックが0になったら IsExpired となり、CharacterModel側で除去される。
/// </summary>
public class StatusEffectModel : PassiveModel
{
    public CharacterModel Source { get; }
    public StatusEffectData Data => (StatusEffectData)_data;
    public int Stack { get; private set; }
    public bool IsExpired => Stack <= 0;

    public StatusEffectModel(CharacterModel owner, CharacterModel source, StatusEffectData data, IBattleField battleField) : base(owner, data, battleField)
    {
        Source = source;
    }

    /// <summary>スタック数を増減させ、実際に変化した量を返す</summary>
    public int ChangeStack(int amount)
    {
        int changed = Mathf.Clamp(amount, -Stack, Data.MaxStack - Stack);
        if (changed == 0) return 0;

        Stack += changed;
        OnStackChanged(changed);

        return changed;
    }

    private protected virtual void OnStackChanged(int changed) { }
}
