using System.Collections.Generic;
using UnityEngine;

public interface IBattleField
{
    IReadOnlyList<CharacterModel> Players { get; }
    IReadOnlyList<CharacterModel> Enemies { get; }

    CharacterModel GetTarget(CharacterModel requester, TargetRule targetRule);

    bool CheckObserveTarget(CharacterModel requester, ActionResult actionResult, TriggerType triggerType, TriggerObserveTargetType observeTargetType);
}
