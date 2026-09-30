using System;
using System.Collections.Generic;

/// <summary>
/// BattleModelのイベントを購読し、BattleView・各CharacterViewへ反映する
/// BattleModel.SetCharactersの後、StartBattleの前に生成すること
/// </summary>
public class BattlePresenter : IDisposable
{
    private readonly BattleModel _battle;
    private readonly BattleView _view;
    private readonly Dictionary<CharacterModel, CharacterView> _characterViews = new Dictionary<CharacterModel, CharacterView>();
    private readonly List<CharacterPresenter> _characterPresenters = new List<CharacterPresenter>();

    public BattlePresenter(BattleModel battle, BattleView view)
    {
        _battle = battle;
        _view = view;

        // キャラが存在しない枠は非表示のままにする
        foreach (var characterView in _view.CharacterViews) characterView.SetVisible(false);
        BindTeam(_battle.Players);
        BindTeam(_battle.Enemies);

        _battle.BattleStarted += OnBattleStarted;
        _battle.ActionPerformed += OnActionPerformed;
        _battle.BattleEnded += OnBattleEnded;
    }

    public void Dispose()
    {
        _battle.BattleStarted -= OnBattleStarted;
        _battle.ActionPerformed -= OnActionPerformed;
        _battle.BattleEnded -= OnBattleEnded;

        foreach (var presenter in _characterPresenters) presenter.Dispose();
        _characterPresenters.Clear();
        _characterViews.Clear();
    }

    private void BindTeam(IReadOnlyList<CharacterModel> team)
    {
        foreach (var character in team)
        {
            CharacterView characterView = _view.GetCharacterView(character.IsPlayer, character.Position);
            _characterViews[character] = characterView;
            _characterPresenters.Add(new CharacterPresenter(character, characterView));
        }
    }

    private void OnBattleStarted() => _view.PlayBattleStart();

    private void OnBattleEnded(BattleResult result) => _view.PlayBattleEnd(result);

    private void OnActionPerformed(ActionResult result)
    {
        if (_characterViews.TryGetValue(result.Owner, out var ownerView))
        {
            switch (result.ActionSource)
            {
                case ActionSource.NormalAttack:
                    ownerView.PlayNormalAttack();
                    break;
                case ActionSource.ActiveSkill:
                    ownerView.PlayActiveSkill();
                    break;
            }
        }

        if (_characterViews.TryGetValue(result.Target, out var targetView)) targetView.PlayActionEffect(result);
    }
}
