using UnityEngine;

public class StatusEffectModel : PassiveModel
{
    private protected int _maxStack;
    private protected int _stack;
    public StatusEffectModel(CharacterModel owner, PassiveData data, IBattleField battleField, int maxStack, int stack) : base(owner, data, battleField)
    {
        _maxStack = maxStack;
        _stack = stack;
    }

    public void ChangeStack(int amount)
    {
        int changed = Mathf.Clamp(amount, 0, _maxStack - _stack);

        _stack += changed;
        OnStackChanged(changed);

        //TODO:0Ç…Ç»Ç¡ÇΩÇÁè¡ãé
    }

    public virtual void OnStackChanged(int changed) { }
}
