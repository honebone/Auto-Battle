using System.Collections.Generic;
using UnityEngine;

public interface IBattleField
{
    IReadOnlyList<CharacterModel> Players { get; }
    IReadOnlyList<CharacterModel> Enemies { get; }

    CharacterModel GetTarget(CharacterModel requester, TargetRule targetRule);
}
