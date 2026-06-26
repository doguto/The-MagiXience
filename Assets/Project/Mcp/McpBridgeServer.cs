#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Project.Scripts.Extensions.Message;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;

namespace Project.Mcp
{
    /// <summary>
    /// TCP ローカルサーバー。外部 MCP クライアント（Claude Code）からの
    /// JSON コマンドを受け取り、InputManager と同じ MessageBroker イベントに変換する。
    /// ポート 7780 で待受。GameObject に AddComponent して使う。
    /// </summary>
    public class McpBridgeServer : MonoBehaviour
    {
        [SerializeField] int port = 7780;

        TcpListener listener;
        Thread listenerThread;
        CancellationTokenSource cts;

        // TCP スレッド → Unity メインスレッドへのアクション橋渡し
        readonly ConcurrentQueue<Action> mainThreadQueue = new();

        // メインスレッドで毎フレーム更新し、TCP スレッドが参照するスナップショット
        volatile GameStateSnapshot latestSnapshot = new();

        PlayerEntityPresenter playerPresenter;
        BossEntityPresenter bossPresenter;
        EnemyTracker enemyTracker;

        void Start()
        {
            cts = new CancellationTokenSource();
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listenerThread = new Thread(ListenLoop) { IsBackground = true };
            listenerThread.Start();
            Debug.Log($"[McpBridgeServer] Listening on 127.0.0.1:{port}");
        }

        void Update()
        {
            TryFindComponents();
            BuildSnapshot();

            while (mainThreadQueue.TryDequeue(out var action))
                action();
        }

        // ---- Component discovery (expensive only when null) ----

        void TryFindComponents()
        {
            if (playerPresenter == null)
                playerPresenter = FindFirstObjectByType<PlayerEntityPresenter>();
            if (bossPresenter == null)
                bossPresenter = FindFirstObjectByType<BossEntityPresenter>();
            if (enemyTracker == null)
                enemyTracker = FindFirstObjectByType<EnemyTracker>();
        }

        // ---- State snapshot (main thread → background thread) ----

        void BuildSnapshot()
        {
            var s = new GameStateSnapshot();

            var pm = playerPresenter?.Model;
            if (pm != null)
            {
                s.hasPlayer = true;
                s.playerHp = pm.CurrentHp.Value;
                s.playerMaxHp = pm.MaxHp;
                s.playerIsAlive = pm.IsAlive;
                s.playerIsSneaking = pm.IsSneaking.Value;
                s.playerIsChargeComplete = pm.IsChargeComplete;
                s.playerIsInvincible = pm.IsInvincible.Value;
                s.playerPosX = playerPresenter.transform.position.x;
                s.playerPosY = playerPresenter.transform.position.y;
            }

            var bm = bossPresenter?.Model;
            if (bm != null)
            {
                s.hasBoss = true;
                s.bossHp = bm.CurrentHp.Value;
                s.bossMaxHp = bm.MaxHp;
                s.bossIsAlive = bm.IsAlive;
            }

            s.enemyCount = enemyTracker?.ActiveEnemyCount ?? 0;
            s.screenMinX = ScreenBoundsCache.MinX;
            s.screenMaxX = ScreenBoundsCache.MaxX;
            s.screenMinY = ScreenBoundsCache.MinY;
            s.screenMaxY = ScreenBoundsCache.MaxY;

            latestSnapshot = s; // 参照の代入はアトミック
        }

        // ---- TCP listener (background thread) ----

        void ListenLoop()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    var t = new Thread(() => HandleClient(client)) { IsBackground = true };
                    t.Start();
                }
                catch when (!cts.Token.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.Length > 0)
                            writer.WriteLine(ProcessCommand(trimmed));
                    }
                }
                catch (Exception e) when (!cts.Token.IsCancellationRequested)
                {
                    Debug.LogWarning($"[McpBridgeServer] Client error: {e.Message}");
                }
            }
        }

        // ---- Command dispatcher (background thread, enqueues to main thread) ----

        string ProcessCommand(string json)
        {
            try
            {
                var cmd = JsonUtility.FromJson<McpCommand>(json);
                if (cmd == null) return Error("parse failed");

                switch (cmd.command)
                {
                    case "get_state":
                        return latestSnapshot.ToJson();

                    case "move":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerMoveMessage { value = new Vector2(cmd.x, cmd.y) }));
                        return OK;

                    case "stop_move":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerMoveMessage { value = Vector2.zero }));
                        return OK;

                    case "attack":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerAttackMessage()));
                        return OK;

                    case "charge_start":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerChargeMessage { isPressed = true }));
                        return OK;

                    case "charge_release":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerChargeMessage { isPressed = false }));
                        return OK;

                    case "pause":
                        mainThreadQueue.Enqueue(() =>
                            MessageBroker.Default.Publish(new PlayerPauseMessage()));
                        return OK;

                    case "ui_navigate":
                        mainThreadQueue.Enqueue(() =>
                        {
                            MessageBroker.Default.Publish(new UINavigateMessage { value = new Vector2(cmd.x, cmd.y) });
                            var es = EventSystem.current;
                            if (es != null)
                            {
                                var axisData = new AxisEventData(es);
                                var dir = new Vector2(cmd.x, cmd.y);
                                axisData.moveDir = dir.x < 0 ? MoveDirection.Left
                                    : dir.x > 0 ? MoveDirection.Right
                                    : dir.y > 0 ? MoveDirection.Up
                                    : MoveDirection.Down;
                                ExecuteEvents.Execute(es.currentSelectedGameObject, axisData, ExecuteEvents.moveHandler);
                            }
                        });
                        return OK;

                    case "ui_submit":
                        mainThreadQueue.Enqueue(() =>
                        {
                            MessageBroker.Default.Publish(new UISubmitMessage());
                            var es = EventSystem.current;
                            if (es?.currentSelectedGameObject != null)
                                ExecuteEvents.Execute(es.currentSelectedGameObject, new BaseEventData(es), ExecuteEvents.submitHandler);
                        });
                        return OK;

                    case "ui_cancel":
                        mainThreadQueue.Enqueue(() =>
                        {
                            MessageBroker.Default.Publish(new UICancelMessage());
                            var es = EventSystem.current;
                            if (es?.currentSelectedGameObject != null)
                                ExecuteEvents.Execute(es.currentSelectedGameObject, new BaseEventData(es), ExecuteEvents.cancelHandler);
                        });
                        return OK;

                    default:
                        return Error($"unknown command: {cmd.command}");
                }
            }
            catch (Exception e)
            {
                return Error(e.Message);
            }
        }

        // ---- Helpers ----

        const string OK = "{\"ok\":true}";

        static string Error(string msg) =>
            "{\"error\":\"" + msg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";

        void OnDestroy()
        {
            cts?.Cancel();
            listener?.Stop();
        }

        // ---- Data types ----

        [Serializable]
        class McpCommand
        {
            public string command;
            public float x;
            public float y;
        }

        class GameStateSnapshot
        {
            public bool hasPlayer;
            public int playerHp, playerMaxHp;
            public float playerPosX, playerPosY;
            public bool playerIsAlive, playerIsSneaking, playerIsChargeComplete, playerIsInvincible;

            public bool hasBoss;
            public int bossHp, bossMaxHp;
            public bool bossIsAlive;

            public int enemyCount;
            public float screenMinX, screenMaxX, screenMinY, screenMaxY;

            public string ToJson()
            {
                var sb = new StringBuilder(256);
                sb.Append("{\"player\":");
                if (hasPlayer)
                {
                    sb.Append("{\"hp\":").Append(playerHp)
                      .Append(",\"maxHp\":").Append(playerMaxHp)
                      .Append(",\"x\":").Append(playerPosX.ToString("F3"))
                      .Append(",\"y\":").Append(playerPosY.ToString("F3"))
                      .Append(",\"isAlive\":").Append(B(playerIsAlive))
                      .Append(",\"isSneaking\":").Append(B(playerIsSneaking))
                      .Append(",\"isChargeComplete\":").Append(B(playerIsChargeComplete))
                      .Append(",\"isInvincible\":").Append(B(playerIsInvincible))
                      .Append("}");
                }
                else
                {
                    sb.Append("null");
                }

                sb.Append(",\"boss\":");
                if (hasBoss)
                {
                    sb.Append("{\"hp\":").Append(bossHp)
                      .Append(",\"maxHp\":").Append(bossMaxHp)
                      .Append(",\"isAlive\":").Append(B(bossIsAlive))
                      .Append("}");
                }
                else
                {
                    sb.Append("null");
                }

                sb.Append(",\"enemyCount\":").Append(enemyCount)
                  .Append(",\"screen\":{\"minX\":").Append(screenMinX.ToString("F2"))
                  .Append(",\"maxX\":").Append(screenMaxX.ToString("F2"))
                  .Append(",\"minY\":").Append(screenMinY.ToString("F2"))
                  .Append(",\"maxY\":").Append(screenMaxY.ToString("F2"))
                  .Append("}}");

                return sb.ToString();
            }

            static string B(bool v) => v ? "true" : "false";
        }
    }
}
#endif
