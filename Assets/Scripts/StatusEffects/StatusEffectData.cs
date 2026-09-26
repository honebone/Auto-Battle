using UnityEngine;

[CreateAssetMenu(menuName = "StatusEffectData")]
public class StatusEffectData : PassiveData
{
    public int MaxStack;

    //stack‚ğˆø”‚É‚à‚Ä‚È‚¢‚¶‚á‚ñI
    //public override PassiveModel CreateModel(CharacterModel owner, IBattleField battleField)
    //{
    //    return new StatusEffectModel(owner,this,battleField,MaxStack)
    //}
}
