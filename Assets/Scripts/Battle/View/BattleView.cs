using UnityEngine;
using System.Collections.Generic;

public class BattleView : MonoBehaviour
{
    [SerializeField,Header("4体分のView (P前, P後, E前, E後 の順)")]
   private List<CharacterView> _characterViews;

    public IReadOnlyList<CharacterView> CharacterViews => _characterViews;

    /// <param name="position">手前から0</param>
    public CharacterView GetCharacterView(bool isPlayer, int position)
    {
        return _characterViews[(isPlayer ? 0 : 2) + position];
    }

    public void PlayBattleStart()
    {
        //TODO:戦闘開始の演出
    }

    public void PlayBattleEnd(BattleResult result)
    {
        //TODO:戦闘終了の演出
    }
}
