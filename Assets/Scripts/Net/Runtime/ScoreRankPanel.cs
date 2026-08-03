using System;
using ClientProtocol;
using UnityEngine;

namespace WorldIsMine.Net.Runtime
{
    public sealed class ScoreRankPanel : CollapsibleRuntimePanel
    {
        private const float Margin = 20f;
        private const float Width = 430f;
        private const float Height = 620f;

        private NetworkRuntime _runtime;
        private SessionSnapshot _fight;
        private S2CScoreRankQueryResponse _total;
        private Rect _windowRect;
        private Vector2 _scroll;
        private string _status = "点击刷新读取玩家总榜";
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;

        public void Initialize(NetworkRuntime runtime)
        {
            if (_runtime == runtime)
                return;
            Unsubscribe();
            _runtime = runtime;
            if (_runtime == null)
                return;
            _runtime.PkBattleStarted += OnFightStarted;
            _runtime.PkBattleUpdated += OnFightChanged;
            _runtime.PkBattleEnded += OnFightEnded;
            _runtime.ScoreRankResponseReceived += OnTotalRank;
            _fight = _runtime.Client?.Pk.CurrentSession;
        }

        private void OnGUI()
        {
            if (_runtime == null)
                return;
            EnsureStyles();
            var collapsed = new Rect(
                Mathf.Max(Margin, Screen.width - DefaultCollapsedButtonWidth - Margin),
                Mathf.Max(100f, Screen.height - DefaultCollapsedButtonHeight - Margin),
                DefaultCollapsedButtonWidth,
                DefaultCollapsedButtonHeight);
            if (DrawCollapsedState(collapsed, _buttonStyle, "展开排行榜"))
                return;

            float width = Mathf.Min(Width, Screen.width - Margin * 2f);
            float height = Mathf.Min(Height, Screen.height - 120f);
            _windowRect = new Rect(Screen.width - width - Margin, 100f, width, height);
            _windowRect = GUI.Window(
                GetInstanceID(), _windowRect, DrawWindow, "礼物积分排行榜", _windowStyle);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawCollapseButton(_buttonStyle);
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("本场战斗 TOP 10", _headerStyle);
            if (_fight == null)
                GUILayout.Label("当前没有 PK 战斗", _labelStyle);
            else if (_fight.FightRank.Count == 0)
                GUILayout.Label("等待玩家送礼", _labelStyle);
            else
                DrawRows(_fight.FightRank, true);

            GUILayout.Space(16f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("玩家总榜 TOP 10", _headerStyle);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && _runtime.IsAnchorSessionReady;
            if (GUILayout.Button("刷新", _buttonStyle, GUILayout.Width(90f), GUILayout.Height(34f)))
                Refresh();
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();

            if (_total?.Accepted == true && _total.Entries.Count > 0)
                DrawRows(_total.Entries, false);
            else
                GUILayout.Label(_status, _labelStyle);

            GUILayout.EndScrollView();
        }

        private void DrawRows(Google.Protobuf.Collections.RepeatedField<PlayerScoreRankEntry> rows, bool showSide)
        {
            foreach (PlayerScoreRankEntry row in rows)
            {
                string name = string.IsNullOrWhiteSpace(row.Nickname)
                    ? $"玩家 {row.PlayerId}"
                    : row.Nickname;
                string side = showSide ? $" [{row.Side}]" : string.Empty;
                GUILayout.Label($"#{row.Rank}  {name}{side}    {row.Score}", _labelStyle);
            }
        }

        private async void Refresh()
        {
            try
            {
                _status = "正在读取总榜...";
                await _runtime.QueryScoreRankAsync(10);
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Debug.LogException(ex);
            }
        }

        private void OnFightChanged(SessionSnapshot snapshot)
        {
            _fight = snapshot?.Clone();
        }

        private void OnFightStarted(SessionSnapshot snapshot)
        {
            OnFightChanged(snapshot);
            Refresh();
        }

        private void OnFightEnded(SubmitGiftResponse response)
        {
            if (response?.Accepted == true)
                _fight = null;
        }

        private void OnTotalRank(S2CScoreRankQueryResponse response)
        {
            _total = response?.Clone();
            _status = response?.Accepted == true
                ? "总榜暂无数据"
                : $"读取失败：{response?.Reason}";
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
                return;
            int fontSize = Mathf.Clamp(Screen.height / 48, 16, 22);
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = fontSize + 2,
                padding = new RectOffset(16, 16, 28, 14)
            };
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = false
            };
            _headerStyle = new GUIStyle(_labelStyle) { fontStyle = FontStyle.Bold };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_runtime == null)
                return;
            _runtime.PkBattleStarted -= OnFightStarted;
            _runtime.PkBattleUpdated -= OnFightChanged;
            _runtime.PkBattleEnded -= OnFightEnded;
            _runtime.ScoreRankResponseReceived -= OnTotalRank;
            _runtime = null;
        }
    }
}
