using System.Collections.Generic;
using UnityEngine;

public interface IBattleField
{
    List<CharacterModel> Players { get; }
    List<CharacterModel> Enemies { get; }

    List<CharacterModel> GetTargets(CharacterModel requester, TargetRule targetRule);
}
