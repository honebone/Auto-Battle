using R3;
using System;

/// <summary>
/// CharacterModelの状態変化を購読し、CharacterViewへ反映する
/// </summary>
public class CharacterPresenter : IDisposable
{
    private readonly CharacterModel _model;
    private readonly CharacterView _view;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private bool _isDead;

    public CharacterPresenter(CharacterModel model, CharacterView view)
    {
        _model = model;
        _view = view;

        _view.SetVisible(true);
        _view.Setup(_model.Data);

        // MaxHealth等のStatValueは変更通知が無いため、値の通知時に都度参照する
        _model.HP.Subscribe(OnHPChanged).AddTo(_disposables);
        _model.Shield.Subscribe(shield => _view.SetShield(shield, _model.MaxHealth.IntValue)).AddTo(_disposables);
        _model.AP.Subscribe(ap => _view.SetAP(ap / 100f)).AddTo(_disposables);
        _model.NATimer.Subscribe(timer => _view.SetNATimer(timer * _model.AttackSpeed.FloatValue)).AddTo(_disposables);

        // コンストラクタ内で有効化されたパッシブは購読前なので、既存分もここで紐づける
        foreach (var passive in _model.ActivePassives) BindPassiveIcon(passive);
        _model.PassiveEnabled += BindPassiveIcon;
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _model.PassiveEnabled -= BindPassiveIcon;

        foreach (var passive in _model.ActivePassives) passive.BindIcon(null);
    }

    private void OnHPChanged(int hp)
    {
        _view.SetHP(hp, _model.MaxHealth.IntValue);

        bool isDead = hp <= 0;
        if (isDead && !_isDead) _view.PlayDeath();
        _isDead = isDead;
    }

    private void BindPassiveIcon(PassiveModel passive)
    {
        passive.BindIcon(_view.CreatePassiveIcon());
    }
}
