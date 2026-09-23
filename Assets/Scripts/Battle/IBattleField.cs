using System.Collections.Generic;
using UnityEngine;
using System;

public interface IBattleField
{
    IReadOnlyList<CharacterModel> Players { get; }
    IReadOnlyList<CharacterModel> Enemies { get; }
    event Action<TriggerType, ActionResult> TriggerAction;

    CharacterModel GetTarget(CharacterModel requester, TargetRule targetRule);

    bool CheckObserveTarget(CharacterModel requester, ActionResult actionResult, TriggerType triggerType, TriggerObserveTargetType observeTargetType);
    void InvokeTriggerAction(TriggerType triggerType, ActionResult actionResult);
}
