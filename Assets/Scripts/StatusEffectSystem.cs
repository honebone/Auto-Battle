using System;
using UnityEngine;

/// <summary>
/// 状態異常の「定義」。SO。発生源ごとに別インスタンス・別スタック管理となる。
/// </summary>
public abstract class StatusEffectDefinition : ScriptableObject
{
    public string EffectName;
    public int MaxStacks = 1;
    public abstract StatusEffectInstance CreateInstance(CharacterModel owner, CharacterModel source);
}

/// <summary>
/// 状態異常のランタイムインスタンス。
/// 同じ種類の状態異常でも、発生源(Source)が異なれば別インスタンスとして扱う
/// (CharacterModel側で (Definition, Source) をキーに管理する)。
/// 戦闘終了時、残っている状態異常はすべて解除される(継続時間の概念はなし)。
/// </summary>
public abstract class StatusEffectInstance
{
    public CharacterModel Owner { get; }
    public CharacterModel Source { get; }
    public int Stacks { get; private set; }
    public int MaxStacks { get; }

    protected StatusEffectInstance(CharacterModel owner, CharacterModel source, int maxStacks)
    {
        Owner = owner;
        Source = source;
        MaxStacks = maxStacks;
    }

    /// <summary>時間駆動の効果(毎秒ダメージ、毎秒スタック減少等)はこれをoverrideする。</summary>
    public virtual void OnManualUpdate(float deltaTime) { }

    /// <summary>初回付与時に呼ばれる。</summary>
    public virtual void OnApply() { }

    /// <summary>スタック0または戦闘終了によって消去される時に呼ばれる。</summary>
    public virtual void OnRemove() { }

    public void AddStacks(int amount)
    {
        int old = Stacks;
        Stacks = Mathf.Clamp(Stacks + amount, 0, MaxStacks);
        if (Stacks != old) OnStacksChanged(old, Stacks);
        if (Stacks <= 0) Owner.RemoveStatusEffect(this);
    }

    /// <summary>スタック数が変化した時の追加処理(ステータス直接編集等)はこれをoverrideする。</summary>
    protected virtual void OnStacksChanged(int oldStacks, int newStacks) { }
}

// =====================================================================
// 個別実装の例
// =====================================================================

/// <summary>
/// 例:「1スタックにつき攻撃速度+5%」のような、スタック数に比例するステータス直接編集。
/// この形は共通実装で使い回せるため、専用データ駆動定義として用意する。
/// </summary>
[CreateAssetMenu(menuName = "StatusEffect/StatBonusPerStack")]
public class StatBonusPerStackDefinition : StatusEffectDefinition
{
    public StatType TargetStat;
    [Tooltip("1スタックあたりの倍率補正量。例:0.05なら+5%/スタック")]
    public float BonusPerStack;

    public override StatusEffectInstance CreateInstance(CharacterModel owner, CharacterModel source)
        => new StatBonusPerStackInstance(owner, source, MaxStacks, TargetStat, BonusPerStack);
}

public class StatBonusPerStackInstance : StatusEffectInstance
{
    private readonly StatType _statType;
    private readonly float _bonusPerStack;

    public StatBonusPerStackInstance(CharacterModel owner, CharacterModel source, int maxStacks,
        StatType statType, float bonusPerStack) : base(owner, source, maxStacks)
    {
        _statType = statType;
        _bonusPerStack = bonusPerStack;
    }

    protected override void OnStacksChanged(int oldStacks, int newStacks)
    {
        var stat = Owner.GetStat(_statType);
        stat.AddMultiplier(_bonusPerStack * (newStacks - oldStacks));
    }

    public override void OnRemove()
    {
        // 消去時は付与した分の補正を打ち消す
        var stat = Owner.GetStat(_statType);
        stat.AddMultiplier(-_bonusPerStack * Stacks);
    }
}

/// <summary>
/// 例:「攻撃力+30%、毎秒1スタック減少する(0になると消去)」実質的な時限式バフ。
/// ステータス直接編集 + 時間駆動の両方を持つため個別実装する。
/// </summary>
public class DecayingAttackBuffInstance : StatusEffectInstance
{
    private readonly float _bonusPerStack;
    private float _timer;

    public DecayingAttackBuffInstance(CharacterModel owner, CharacterModel source, int maxStacks, float bonusPerStack)
        : base(owner, source, maxStacks) => _bonusPerStack = bonusPerStack;

    protected override void OnStacksChanged(int oldStacks, int newStacks)
        => Owner.AttackPower.AddMultiplier(_bonusPerStack * (newStacks - oldStacks));

    public override void OnManualUpdate(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < 1f) return;
        _timer -= 1f;
        AddStacks(-1);
    }

    public override void OnRemove() => Owner.AttackPower.AddMultiplier(-_bonusPerStack * Stacks);
}

/// <summary>
/// 例:「毎秒、スタック数に応じたダメージを受ける」。
/// 基礎値がスタック数依存という特殊計算のため、ActionResolver.ResolveWithExplicitValueを直接呼ぶ。
/// このダメージはクリティカル・ドレインの対象外(通常攻撃の解決単位に含まれない独立した行動のため)。
/// </summary>
public class StackDamageOverTimeInstance : StatusEffectInstance
{
    private readonly float _damagePerStackPerSecond;
    private float _timer;

    public StackDamageOverTimeInstance(CharacterModel owner, CharacterModel source, int maxStacks,
        float damagePerStackPerSecond) : base(owner, source, maxStacks)
        => _damagePerStackPerSecond = damagePerStackPerSecond;

    public override void OnManualUpdate(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < 1f) return;
        _timer -= 1f;
        if (Stacks <= 0) return;

        float damage = Stacks * _damagePerStackPerSecond;
        Owner.ActionResolver.ResolveWithExplicitValue(ActionType.MagicAttack, damage, Source, Owner);
    }
}

/// <summary>
/// 例:「クリティカル率+100%、通常攻撃をするごとに1スタック減少」。
/// スタック減少のトリガーが「通常攻撃時」であるため、
/// OnStacksChangedでのステータス編集 + 通常攻撃トリガーの購読の両方が必要。
/// </summary>
public class CritRateStackOnAttackInstance : StatusEffectInstance
{
    private readonly float _bonusPerStack;
    private IDisposable _subscription;

    public CritRateStackOnAttackInstance(CharacterModel owner, CharacterModel source, int maxStacks, float bonusPerStack)
        : base(owner, source, maxStacks) => _bonusPerStack = bonusPerStack;

    public override void OnApply()
    {
        // TODO: R3で Owner.GetTriggerObservable(TriggerType.OnNormalAttackDealt) を購読し、AddStacks(-1) する
        // _subscription = Owner.GetTriggerObservable(TriggerType.OnNormalAttackDealt).Subscribe(_ => AddStacks(-1));
    }

    protected override void OnStacksChanged(int oldStacks, int newStacks)
        => Owner.CriticalRate.AddMultiplier(_bonusPerStack * (newStacks - oldStacks));

    public override void OnRemove()
    {
        Owner.CriticalRate.AddMultiplier(-_bonusPerStack * Stacks);
        _subscription?.Dispose();
    }
}