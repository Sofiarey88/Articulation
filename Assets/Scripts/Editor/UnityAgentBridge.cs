#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Servidor HTTP local dentro de Unity Editor para comunicar Antigravity en tiempo real.
/// Permite consultar el estado, la jerarquía, la consola y ejecutar acciones en vivo.
/// </summary>
[InitializeOnLoad]
public static class UnityAgentBridge
{
    private const int Port = 8085;
    private static HttpListener _listener;
    private static CancellationTokenSource _cts;

    private static readonly List<LogItem> _recentLogs = new List<LogItem>();
    private const int MaxLogs = 100;
    private static readonly object _logsLock = new object();

    [System.Serializable]
    public class LogItem
    {
        public string type;
        public string message;
        public string stackTrace;
        public string timestamp;
    }

    [System.Serializable]
    public class StatusResponse
    {
        public string status;
        public bool isPlaying;
        public bool isCompiling;
        public bool isPaused;
        public string activeScene;
        public int logCount;
    }

    [System.Serializable]
    public class HierarchyItem
    {
        public string name;
        public bool active;
        public string tag;
        public List<string> components = new List<string>();
        public List<HierarchyItem> children = new List<HierarchyItem>();
    }

    [System.Serializable]
    public class HierarchyResponse
    {
        public string scene;
        public List<HierarchyItem> roots = new List<HierarchyItem>();
    }

    private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _mainThreadQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();

    static UnityAgentBridge()
    {
        Application.runInBackground = true;
        EditorApplication.update += ProcessMainThreadQueue;
        Application.logMessageReceivedThreaded += OnLogReceived;
        AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        EditorApplication.quitting += StopServer;

        StartServer();
    }

    private static void ProcessMainThreadQueue()
    {
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        lock (_logsLock)
        {
            if (_recentLogs.Count >= MaxLogs)
            {
                _recentLogs.RemoveAt(0);
            }
            _recentLogs.Add(new LogItem
            {
                type = type.ToString(),
                message = condition,
                stackTrace = stackTrace,
                timestamp = DateTime.Now.ToString("HH:mm:ss.fff")
            });
        }
    }

    [MenuItem("Tools/Antigravity Bridge/Reiniciar Servidor")]
    public static void StartServer()
    {
        StopServer();
        try
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();

            Task.Run(() => ListenLoop(_cts.Token));
            Debug.Log($"<color=#38bdf8>[UnityAgentBridge]</color> Servidor activo en http://127.0.0.1:{Port}/");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[UnityAgentBridge] Error al iniciar listener: {ex.Message}");
        }
    }

    [MenuItem("Tools/Antigravity Bridge/Detener Servidor")]
    public static void StopServer()
    {
        try
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }
        }
        catch { }
    }

    private static async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch
            {
                if (token.IsCancellationRequested) break;
            }
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        string path = req.Url.AbsolutePath.ToLowerInvariant();
        try
        {
            if (path == "/ping")
            {
                SendJson(res, "{\"status\":\"pong\"}");
            }
            else if (path == "/status")
            {
                var status = await RunOnMainThread(() => new StatusResponse
                {
                    status = "online",
                    isPlaying = EditorApplication.isPlaying,
                    isCompiling = EditorApplication.isCompiling,
                    isPaused = EditorApplication.isPaused,
                    activeScene = SceneManager.GetActiveScene().name,
                    logCount = _recentLogs.Count
                });
                SendJson(res, JsonUtility.ToJson(status));
            }
            else if (path == "/console")
            {
                List<LogItem> logsCopy;
                lock (_logsLock)
                {
                    logsCopy = new List<LogItem>(_recentLogs);
                }
                var sb = new StringBuilder();
                sb.Append("{\"logs\":[");
                for (int i = 0; i < logsCopy.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append(JsonUtility.ToJson(logsCopy[i]));
                }
                sb.Append("]}");
                SendJson(res, sb.ToString());
            }
            else if (path == "/clear_console")
            {
                lock (_logsLock)
                {
                    _recentLogs.Clear();
                }
                SendJson(res, "{\"success\":true}");
            }
            else if (path == "/hierarchy")
            {
                var hierarchy = await RunOnMainThread(() =>
                {
                    var scene = SceneManager.GetActiveScene();
                    var resp = new HierarchyResponse { scene = scene.name };
                    var roots = scene.GetRootGameObjects();
                    foreach (var root in roots)
                    {
                        resp.roots.Add(SerializeHierarchy(root));
                    }
                    return resp;
                });
                SendJson(res, JsonUtility.ToJson(hierarchy));
            }
            else if (path == "/play")
            {
                bool result = await RunOnMainThread(() =>
                {
                    EditorApplication.isPlaying = !EditorApplication.isPlaying;
                    return EditorApplication.isPlaying;
                });
                SendJson(res, $"{{\"isPlaying\":{result.ToString().ToLower()}}}");
            }
            else if (path == "/refresh")
            {
                await RunOnMainThread(() =>
                {
                    AssetDatabase.Refresh();
                    return true;
                });
                SendJson(res, "{\"refreshed\":true}");
            }
            else if (path == "/build_grua")
            {
                await RunOnMainThread(() =>
                {
                    CreadorGruaTorre.ConstruirEscenarioCompleto();
                    return true;
                });
                SendJson(res, "{\"built\":true}");
            }
            else if (path == "/check_targets")
            {
                var info = await RunOnMainThread(() =>
                {
                    bool webgl = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                    bool win = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    return $"{{\"webgl\":{webgl.ToString().ToLower()},\"windows\":{win.ToString().ToLower()},\"activeTarget\":\"{EditorUserBuildSettings.activeBuildTarget}\"}}";
                });
                SendJson(res, info);
            }
            else if (path == "/execute")
            {
                string menu = req.QueryString["menu"];
                if (string.IsNullOrEmpty(menu))
                {
                    SendJson(res, "{\"error\":\"Parametro 'menu' requerido\"}", 400);
                    return;
                }
                bool executed = await RunOnMainThread(() => EditorApplication.ExecuteMenuItem(menu));
                SendJson(res, $"{{\"executed\":{executed.ToString().ToLower()},\"menu\":\"{menu}\"}}");
            }
            else
            {
                SendJson(res, "{\"error\":\"Ruta no encontrada\"}", 404);
            }
        }
        catch (Exception ex)
        {
            SendJson(res, $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}", 500);
        }
    }

    private static HierarchyItem SerializeHierarchy(GameObject go)
    {
        var item = new HierarchyItem
        {
            name = go.name,
            active = go.activeSelf,
            tag = go.tag
        };
        foreach (var c in go.GetComponents<Component>())
        {
            if (c != null) item.components.Add(c.GetType().Name);
        }
        for (int i = 0; i < go.transform.childCount; i++)
        {
            item.children.Add(SerializeHierarchy(go.transform.GetChild(i).gameObject));
        }
        return item;
    }

    private static async Task<T> RunOnMainThread<T>(Func<T> action, int timeoutMs = 4000)
    {
        var tcs = new TaskCompletionSource<T>();
        _mainThreadQueue.Enqueue(() =>
        {
            try
            {
                tcs.TrySetResult(action());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
        if (completedTask == tcs.Task)
        {
            return await tcs.Task;
        }
        throw new TimeoutException("Operacion en hilo principal demoro demasiado o Unity esta en segundo plano.");
    }

    private static void SendJson(HttpListenerResponse res, string json, int statusCode = 200)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        res.StatusCode = statusCode;
        res.ContentType = "application/json";
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.Close();
    }

    private static string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }
}
#endif
