using UnityEngine;

public enum Rarity { Common, Rare, Epic }

[CreateAssetMenu(menuName = "ItemData/Basic")]
public class ItemData : PassiveData
{
    public Rarity Rarity;
}
