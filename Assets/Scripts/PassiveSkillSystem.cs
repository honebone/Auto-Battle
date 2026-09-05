using System;
using System.Collections.Generic;
using UnityEngine;
// using R3;

/// <summary>
/// データ駆動パッシブが持つ「トリガー + 実行する行動」の組。インスペクタで編集する。
/// </summary>
[Serializable]
public class PassiveSkillSpec
{
    public TriggerType Trigger;
    public List<ActionSpec> Actions;
}

/// <summary>
/// パッシブスキルの「定義」。SO。データ駆動版・個別実装版どちらもこれを継承する。
/// CharacterData/ItemDataは抽象型のリストとして保持することで、
/// データ駆動/個別実装のどちらでも同じ枠に入れられるようにする。
/// </summary>
public abstract class PassiveSkillDefinition : ScriptableObject
{
    public string DisplayName;
    public abstract PassiveSkillInstance CreateInstance();
}

/// <summary>
/// トリガー成立時にActionSpecを実行するだけの、汎用的なパッシブ定義。
/// 例:「アクティブスキル発動時、自身を回復する」等はこれで表現できる。
/// </summary>
[CreateAssetMenu(menuName = "PassiveSkill/DataDriven")]
public class DataDrivenPassiveSkillDefinition : PassiveSkillDefinition
{
    [SerializeField] private PassiveSkillSpec _spec;
    public override PassiveSkillInstance CreateInstance() => new DataDrivenPassiveSkillInstance(_spec);
}

/// <summary>
/// パッシブスキルのランタイムインスタンスの基底クラス。
/// 時間駆動・特殊なロジックが必要なものは、これを継承した専用クラスを個別に作る。
/// </summary>
public abstract class PassiveSkillInstance
{
    protected CharacterModel Owner;
    private readonly List<IDisposable> _subscriptions = new();

    public virtual void Initialize(CharacterModel owner) => Owner = owner;

    /// <summary>フレームが進むごとに呼ばれる。時間経過で発動する効果はこれをoverrideして実装する。</summary>
    public virtual void OnManualUpdate(float deltaTime) { }

    public virtual void OnCombatEnd()
    {
        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();
    }

    protected void Track(IDisposable subscription) => _subscriptions.Add(subscription);
}

/// <summary>
/// トリガー成立時にActionSpecを実行する、汎用的なパッシブインスタンス。
/// </summary>
public class DataDrivenPassiveSkillInstance : PassiveSkillInstance
{
    private readonly PassiveSkillSpec _spec;

    public DataDrivenPassiveSkillInstance(PassiveSkillSpec spec) => _spec = spec;

    public override void Initialize(CharacterModel owner)
    {
        base.Initialize(owner);
        // TODO: R3で該当トリガーのObservableを購読する
        // var subscription = Owner.GetTriggerObservable(_spec.Trigger).Subscribe(_ => Execute());
        // Track(subscription);
    }

    private void Execute()
    {
        foreach (var actionSpec in _spec.Actions)
            Owner.ActionResolver.Resolve(actionSpec, Owner);
    }
}

// =====================================================================
// 個別実装の例
// =====================================================================
// 今回の議論に出た例の中で、純粋なトリガー+行動で表現できないもの
// (時間駆動・特殊計算が必要なもの)は StatusEffectSystem.cs 側の個別実装を参照。
// パッシブスキル単体で個別実装が必要になるのは、例えば「発動条件が複数のトリガーの組み合わせ」
// のような複雑なケースが今後出てきた場合を想定。