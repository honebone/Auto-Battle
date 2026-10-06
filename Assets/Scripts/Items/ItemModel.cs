using UnityEngine;

//TODO:そもそもItemModelが必要かを考える
//アイテムの具象クラスはPassiveModelを継承するだけでいいのでは？
public class ItemModel : PassiveModel
{
   public ItemModel(CharacterModel owner, StatusEffectData data, IBattleField battleField):base(owner, data, battleField) { }
}
