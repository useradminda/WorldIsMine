using System;
using System.Threading.Tasks;
using PlayerProtocol;
using UnityEngine;

namespace WorldIsMine.Net.Runtime
{
    public sealed class TroopGrowthPanel : CollapsibleRuntimePanel
    {
        private const float Margin = 20f;
        private const float Width = 500f;
        private const float Height = 560f;

        private NetworkRuntime _runtime;
        private Rect _windowRect;
        private Vector2 _scroll;
        private string _playerId = "1";
        private string _troopId = "10001";
        private string _status = "输入 PlayerId 后查询兵种";
        private S2CTroopQueryResponse _query;
        private bool _sending;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _fieldStyle;

        public void Initialize(NetworkRuntime runtime)
        {
            if (_runtime == runtime)
                return;
            Unsubscribe();
            _runtime = runtime;
            if (_runtime == null)
                return;
            _runtime.TroopQueryResponseReceived += OnQuery;
            _runtime.TroopUpgradeResponseReceived += OnUpgrade;
        }

        private void OnGUI()
        {
            if (_runtime == null)
                return;
            EnsureStyles();
            float width = Mathf.Min(Width, Screen.width - Margin * 2f);
            float height = Mathf.Min(Height, Screen.height - Margin * 2f);
            _windowRect = new Rect(Margin, 90f, width, height);
            _windowRect = GUI.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "兵种成长测试",
                _windowStyle);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawCollapseButton(_buttonStyle);
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("玩家 PlayerId", _labelStyle);
            _playerId = GUILayout.TextField(_playerId, _fieldStyle, GUILayout.Height(38f));
            GUILayout.Label("兵种 TroopId（10001～10005 / 20001～20005）", _labelStyle);
            _troopId = GUILayout.TextField(_troopId, _fieldStyle, GUILayout.Height(38f));

            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !_sending && _runtime.IsAnchorSessionReady;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("查询兵种", _buttonStyle, GUILayout.Height(42f)))
                Send(() => _runtime.QueryTroopsAsync(ParsePlayerId()), "查询");
            if (GUILayout.Button("升级兵种", _buttonStyle, GUILayout.Height(42f)))
                Send(
                    () => _runtime.UpgradeTroopAsync(ParsePlayerId(), ParseTroopId()),
                    "升级");
            GUILayout.EndHorizontal();
            GUI.enabled = enabled;

            GUILayout.Space(12f);
            GUILayout.Label($"状态：{_status}", _labelStyle);
            if (_query?.Module != null)
            {
                GUILayout.Label($"模块版本：{_query.ModuleVersion}", _labelStyle);
                foreach (TroopData troop in _query.Module.Troops)
                {
                    GUILayout.Label(
                        $"TroopId={troop.TroopId}  Lv.{troop.Level}  Exp={troop.Exp}",
                        _labelStyle);
                }
            }

            GUILayout.EndScrollView();
        }

        private async void Send(Func<Task<long>> action, string name)
        {
            if (_sending)
                return;
            try
            {
                _sending = true;
                long msgId = await action();
                _status = $"{name}请求已发送，MsgId={msgId}";
            }
            catch (Exception ex)
            {
                _status = $"{name}失败：{ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                _sending = false;
            }
        }

        private void OnQuery(S2CTroopQueryResponse response)
        {
            _query = response?.Clone();
            _status = response?.Accepted == true
                ? $"查询成功，共 {response.Module?.Troops.Count ?? 0} 个兵种"
                : $"查询失败：{response?.Reason}";
        }

        private void OnUpgrade(S2CTroopUpgradeResponse response)
        {
            if (response?.PlayerId > 0)
                _playerId = response.PlayerId.ToString();
            if (response?.Troop?.TroopId > 0)
                _troopId = response.Troop.TroopId.ToString();
            _status = response?.Accepted == true
                ? $"升级成功{(response.Duplicate ? "（重复请求）" : string.Empty)}：" +
                  $"Lv.{response.Troop?.Level}，消耗 {response.Cost} {response.CurrencyCode}，" +
                  $"余额 {response.CurrencyBalance}"
                : $"升级失败：{response?.Reason}";
            if (response?.Accepted == true)
                Send(() => _runtime.QueryTroopsAsync(ParsePlayerId()), "刷新");
        }

        private ulong ParsePlayerId()
        {
            if (!ulong.TryParse(_playerId, out ulong value) || value == 0)
                throw new FormatException("PlayerId 必须是大于 0 的整数。");
            return value;
        }

        private uint ParseTroopId()
        {
            if (!uint.TryParse(_troopId, out uint value) || value == 0)
                throw new FormatException("TroopId 必须是大于 0 的整数。");
            return value;
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
                wordWrap = true
            };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            _fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = fontSize };
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_runtime == null)
                return;
            _runtime.TroopQueryResponseReceived -= OnQuery;
            _runtime.TroopUpgradeResponseReceived -= OnUpgrade;
            _runtime = null;
        }
    }
}
