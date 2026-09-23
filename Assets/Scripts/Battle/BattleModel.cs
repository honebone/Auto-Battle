using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class BattleModel : IBattleField
{
    private List<CharacterModel> _players = new List<CharacterModel>();
    private List<CharacterModel> _enemies = new List<CharacterModel>();
    public IReadOnlyList<CharacterModel> Players => _players;
    public IReadOnlyList<CharacterModel> Enemies => _enemies;
    public event Action<TriggerType, ActionResult> TriggerAction;

    public void ManualUpdate(float deltaTime)
    {
        _players.ForEach(player => player.ManualUpdate(deltaTime));
        _enemies.ForEach(enemy => enemy.ManualUpdate(deltaTime));
    }

    public void InvokeTriggerAction(TriggerType triggerType,ActionResult actionResult)
    {
        TriggerAction?.Invoke(triggerType, actionResult);
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

    public bool CheckObserveTarget(CharacterModel requester, ActionResult actionResult,TriggerType triggerType, TriggerObserveTargetType observeTargetType)
    {
        if (triggerType == TriggerType.NoAction) return true;
        CharacterModel checkTarget = triggerType == TriggerType.Active ? actionResult.Owner : 
                                     triggerType == TriggerType.Passive ? actionResult.Target : null;

        if(checkTarget == null)
        {
            Debug.LogWarning($"triggerType {triggerType} のチェック対象未定義");
            return false;
        }

        return observeTargetType switch
        {
            TriggerObserveTargetType.Self => checkTarget == requester,
            TriggerObserveTargetType.Anyone => true,
            TriggerObserveTargetType.AnyAllies => checkTarget.IsPlayer == requester.IsPlayer,
            TriggerObserveTargetType.OtherAlly => checkTarget != requester && checkTarget.IsPlayer == requester.IsPlayer,
            TriggerObserveTargetType.AnyOpponents => checkTarget.IsPlayer != requester.IsPlayer,
            _ => throw new ArgumentOutOfRangeException(nameof(observeTargetType)),
        };
    }
}
