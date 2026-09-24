using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 戦闘の動作確認用テスター。
/// 空のGameObjectにアタッチし、インスペクタで指定したプレイヤー・敵の編成で戦闘を行う
/// </summary>
public class BattleTester : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private CharacterData _playerFront;
    [SerializeField] private CharacterData _playerBack;

    [Header("敵")]
    [SerializeField] private CharacterData _enemyFront;
    [SerializeField] private CharacterData _enemyBack;

    [Header("設定")]
    [Tooltip("有効にすると、Databaseの制限時間の代わりに下の値を使う(時間切れの確認用)")]
    [SerializeField] private bool _overrideTimeLimit = false;
    [SerializeField, EnableIf(nameof(_overrideTimeLimit)), Min(0f)] private float _timeLimitOverride = 10f;
    [Tooltip("戦闘の進行速度倍率")]
    [SerializeField, Min(0f)] private float _timeScale = 1f;
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _enableLog = true;

    [Header("即時シミュレーション")]
    [Tooltip("即時シミュレーション時の1ステップの秒数")]
    [SerializeField, Min(0.001f)] private float _simulateDeltaTime = 1f / 60f;

    private BattleModel _battle;
    private BattleLogger _logger;

    public BattleModel Battle => _battle;

    private void Start()
    {
        if (_playOnStart) StartBattle();
    }

    private void Update()
    {
        if (_battle == null || _battle.IsFinished) return;
        _battle.ManualUpdate(Time.deltaTime * _timeScale);
    }

    private void OnDestroy()
    {
        _logger?.Dispose();
    }

    /// <summary>
    /// インスペクタで指定した編成で戦闘を開始する(Update毎に進行)
    /// </summary>
    [Button]
    public void StartBattle()
    {
        _logger?.Dispose();
        _logger = null;

        _battle = new BattleModel(_overrideTimeLimit ? _timeLimitOverride : null);
        _battle.SetCharacters(true, _playerFront, _playerBack);
        _battle.SetCharacters(false, _enemyFront, _enemyBack);

        if (_enableLog) _logger = new BattleLogger(_battle);

        _battle.StartBattle();
    }

    /// <summary>
    /// 戦闘を開始し、決着まで即座に進行させる
    /// </summary>
    [Button]
    public void SimulateInstant()
    {
        StartBattle();

        //制限時間を超えれば必ずTimeUpで終了するが、念のため上限を設ける
        int maxSteps = Mathf.CeilToInt(_battle.TimeLimit / _simulateDeltaTime) + 10;
        for (int i = 0; i < maxSteps && !_battle.IsFinished; i++)
        {
            _battle.ManualUpdate(_simulateDeltaTime);
        }

        if (!_battle.IsFinished) Debug.LogWarning("[BattleTester] 上限ステップ数に達しましたが戦闘が終了しませんでした");
    }
}
