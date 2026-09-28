using UnityEngine;
using System.Collections.Generic;

public class BattleView : MonoBehaviour
{
    [SerializeField,Header("4‘Ì•ª‚ÌView")]
   private List<CharacterView> _characterViews;

    public IReadOnlyList<CharacterView> CharacterViews => _characterViews;
}
