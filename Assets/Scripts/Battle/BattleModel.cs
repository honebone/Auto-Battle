using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public enum BattleResult
{
    InProgress,
    PlayerWin,
    /// <summary>プレイヤー全滅(相討ちを含む)</summary>
    PlayerLose,
    /// <summary>制限時間切れ(プレイヤーの敗北扱い)</summary>
    TimeUp,
}

[Serializable]
public struct TriggerCondition
{
    public TriggerType TriggerType;
    public CharaRelation ObserveType_Owner;
    public CharaRelation ObserveType_Target;
}

public class BattleModel : IBattleField
{
    private List<CharacterModel> _players = new List<CharacterModel>();
    private List<CharacterModel> _enemies = new List<CharacterModel>();
    public IReadOnlyList<CharacterModel> Players => _players;
    public IReadOnlyList<CharacterModel> Enemies => _enemies;
    public event Action<TriggerType, ActionResult> TriggerAction;

    /// <summary>行動が解決されるたびに通知(ログ・統計用)</summary>
    public event Action<ActionResult> ActionPerformed;
    public event Action BattleStarted;
    public event Action<BattleResult> BattleEnded;

    public float TimeLimit { get; }
    public float ElapsedTime { get; private set; }
    public BattleResult Result { get; private set; } = BattleResult.InProgress;
    public bool IsStarted { get; private set; }
    public bool IsFinished => Result != BattleResult.InProgress;

    /// <param name="timeLimitOverride">制限時間を上書きする場合に指定(デバッグ用)。nullならDatabaseの値を使用</param>
    public BattleModel(float? timeLimitOverride = null)
    {
        TimeLimit = timeLimitOverride ?? Database.Instance.BattleTimeLimit;
    }

    /// <summary>
    /// 指定した陣営に前衛・後衛のキャラクターをセットする(既存のキャラクターは置き換え)
    /// </summary>
    /// <param name="frontItems">前衛に装着するアイテム(省略可)</param>
    /// <param name="backItems">後衛に装着するアイテム(省略可)</param>
    public void SetCharacters(
        bool isPlayer,
        CharacterData front,
        CharacterData back,
        IReadOnlyList<ItemData> frontItems = null,
        IReadOnlyList<ItemData> backItems = null)
    {
        List<CharacterModel> team = isPlayer ? _players : _enemies;
        // パッシブは常に購読しているため、置き換える前に解除しておく
        team.ForEach(chara => chara.Dispose());
        team.Clear();

        //GetTargetはリスト順を前衛優先として扱うため、前衛→後衛の順に追加する
        if (front != null)
        {
            CharacterModel chara = front.CreateModel(this, new CharaContext { IsPlayer = isPlayer, Position = 0 });
            EquipItems(chara, frontItems);
            team.Add(chara);
        }
        if (back != null)
        {
            CharacterModel chara = back.CreateModel(this, new CharaContext { IsPlayer = isPlayer, Position = 1 });
            EquipItems(chara, backItems);
            team.Add(chara);
        }
    }

    private void EquipItems(CharacterModel chara, IReadOnlyList<ItemData> items)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (item == null) continue;
            if (!chara.TryEquipItem(item))
            {
                Debug.LogWarning($"{chara.DisplayName}: 装備上限({Database.Instance.MaxItemSlots})に達しているため {item.PassiveName} を装着できませんでした");
            }
        }
    }

    public void StartBattle()
    {
        ElapsedTime = 0;
        Result = BattleResult.InProgress;
        IsStarted = true;

        _players.ForEach(player => player.InitBattle());
        _enemies.ForEach(enemy => enemy.InitBattle());

        BattleStarted?.Invoke();
        InvokeTriggerAction(TriggerType.BattleStart, default);

        CheckResult();
    }

    public void ManualUpdate(float deltaTime)
    {
        if (!IsStarted || IsFinished) return;

        ElapsedTime += deltaTime;
        _players.ForEach(player => player.ManualUpdate(deltaTime));
        _enemies.ForEach(enemy => enemy.ManualUpdate(deltaTime));

        CheckResult();
    }

    /// <summary>
    /// 勝敗判定。両陣営同時の全滅(相討ち)はプレイヤーの敗北
    /// </summary>
    private void CheckResult()
    {
        if (IsFinished) return;

        bool playersDefeated = _players.All(c => !c.IsAlive);
        bool enemiesDefeated = _enemies.All(c => !c.IsAlive);

        if (playersDefeated) Result = BattleResult.PlayerLose;
        else if (enemiesDefeated) Result = BattleResult.PlayerWin;
        else if (ElapsedTime >= TimeLimit) Result = BattleResult.TimeUp;
        else return;

        _players.ForEach(player => player.EndBattle());
        _enemies.ForEach(enemy => enemy.EndBattle());

        BattleEnded?.Invoke(Result);
    }

    public void InvokeTriggerAction(TriggerType triggerType,ActionResult actionResult)
    {
        TriggerAction?.Invoke(triggerType, actionResult);
    }

    public void NotifyActionPerformed(ActionResult actionResult)
    {
        ActionPerformed?.Invoke(actionResult);
    }

    public CharacterModel GetTarget(CharacterModel requester, TargetRule targetRule)
    {
        if (targetRule == TargetRule.Self) return requester;

        bool isPlayer = requester.IsPlayer;
        IReadOnlyList<CharacterModel> allies = isPlayer ? _players : _enemies;
        IReadOnlyList<CharacterModel> opponents = isPlayer ? _enemies : _players;

        return targetRule switch
        {
            TargetRule.FrontOpponent => opponents.Where(c => c.IsAlive).FirstOrDefault(),
            TargetRule.BackOpponent => opponents.Where(c => c.IsAlive).LastOrDefault(),
            TargetRule.RandomOpponent => GetRandomAlive(opponents),
            TargetRule.WeakestOpponent => opponents.Where(c => c.IsAlive).OrderBy(c => c.CurrentHpRatio).FirstOrDefault(),

            TargetRule.FrontAlly => allies.Where(c => c.IsAlive).FirstOrDefault(),
            TargetRule.BackAlly => allies.Where(c => c.IsAlive).LastOrDefault(),
            TargetRule.RandomAlly => GetRandomAlive(allies),
            TargetRule.WeakestAlly => allies.Where(c => c.IsAlive).OrderBy(c => c.CurrentHpRatio).FirstOrDefault(),

            _ => throw new ArgumentOutOfRangeException(nameof(targetRule)),
        };
    }

    private CharacterModel GetRandomAlive(IReadOnlyList<CharacterModel> characters)
    {
        //TODO: 本来はシード値で初期化された乱数を使うべき。今は仮でUnityEngine.Randomを使用している。
        var alive = characters.Where(c => c.IsAlive).ToList();
        if (alive.Count == 0) return null;

        return alive[UnityEngine.Random.Range(0, alive.Count)];
    }

    //public bool CheckObserveTarget(
    //    CharacterModel requester,
    //    ActionResult actionResult,
    //    TriggerType triggerType,
    //    CharaRelation observeTargetType,
    //    bool observeActionOwner
    //    )
    //{
    //    if (triggerType == TriggerType.NoAction) return true;
    //    //CharacterModel checkTarget = triggerType == TriggerType.Active ? actionResult.Owner : 
    //    //                             triggerType == TriggerType.Passive ? actionResult.Target : null;

    //    CharacterModel checkTarget = observeActionOwner ? actionResult.Owner : actionResult.Target;

    //    return observeTargetType switch
    //    {
    //        CharaRelation.Self => checkTarget == requester,
    //        CharaRelation.Anyone => true,
    //        CharaRelation.AnyAllies => checkTarget.IsPlayer == requester.IsPlayer,
    //        CharaRelation.OtherAlly => checkTarget != requester && checkTarget.IsPlayer == requester.IsPlayer,
    //        CharaRelation.AnyOpponents => checkTarget.IsPlayer != requester.IsPlayer,
    //        _ => throw new ArgumentOutOfRangeException(nameof(observeTargetType)),
    //    };
    //}

    public bool CheckObserveTarget(
       CharacterModel requester,
       ActionResult actionResult,
       TriggerCondition triggerCondition
       )
    {
        if (triggerCondition.TriggerType == TriggerType.NoAction) return true;
        return CheckRelation(requester, actionResult.Owner, triggerCondition.ObserveType_Owner) && CheckRelation(requester, actionResult.Target, triggerCondition.ObserveType_Target);
    }

    private bool CheckRelation(CharacterModel requester,CharacterModel checkTarget, CharaRelation observeTargetType)
    {
        return observeTargetType switch
        {
            CharaRelation.Self => checkTarget == requester,
            CharaRelation.Anyone => true,
            CharaRelation.AnyAllies => checkTarget.IsPlayer == requester.IsPlayer,
            CharaRelation.OtherAlly => checkTarget != requester && checkTarget.IsPlayer == requester.IsPlayer,
            CharaRelation.AnyOpponents => checkTarget.IsPlayer != requester.IsPlayer,
            _ => throw new ArgumentOutOfRangeException(nameof(observeTargetType)),
        };
    }
}
