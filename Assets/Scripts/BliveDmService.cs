using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BliveDmService : SingletonMono<BliveDmService>
{
    private WebSocket _webSocket;
    private float _heartbeatInterval = 10f; // 10 秒心跳
    private float _heartbeatTimer = 0f;
    private bool _isClosing = false;
    private readonly string _url = "ws://localhost:12450/api/chat";
    
    private Queue<string> _dmQueue;

    public string GetDm()
    {
        if (_dmQueue.Count <= 0)
        {
            return null;
        }
        return _dmQueue.Dequeue();
    }

    public void AddDm(string dm)
    {
        _dmQueue.Enqueue(dm);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        _dmQueue = new Queue<string>();
        await Connect();
    }

    // Update is called once per frame
    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        _webSocket?.DispatchMessageQueue();
        #endif
        
        // 心跳定时器
        _heartbeatTimer += Time.deltaTime;
        if (_heartbeatTimer >= _heartbeatInterval)
        {
            _heartbeatTimer = 0f;
            SendJson(@"{""cmd"":0,""data"":{}}");
        }
    }
    
    private async Task Connect()
    {
        _webSocket = new WebSocket(_url);

        _webSocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket 已连接到弹幕");
            _isClosing = false;
            // 订阅直播间
            SendJson(@"{""cmd"":1,""data"":{""roomId"":657372}}");
        };
        
        _webSocket.OnMessage += (bytes) =>
        {
            // getting the message as a string
            // var message = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log("OnMessage! " + message);
            string msg = Encoding.UTF8.GetString(bytes);
            // Debug.Log("📩 收到消息: " + msg);
            BliveDmGetterData data = JsonConvert.DeserializeObject<BliveDmGetterData>(msg);
            if (data.cmd == 2 && data.data is JArray arr)
            {
                string dmtext = arr[4].ToString();
                // 弹幕添加到队列
                AddDm(dmtext);
            }
        };
        
        _webSocket.OnError += (e) =>
        {
            Debug.Log("❌ WebSocket 错误: " + e);
        };
        
        _webSocket.OnClose += (e) =>
        {
            Debug.Log("⚠️ WebSocket 已关闭, 尝试重连...");
            if (!_isClosing) _ = TryReconnect();
        };

        // waiting for messages
        await _webSocket.Connect();
    }
    
    private async Task TryReconnect()
    {
        _isClosing = true;
        int delay = 3000; // 3 秒后重连，可递增
        await Task.Delay(delay);

        Debug.Log("🔄 正在重连...");
        await Connect();
    }
    
    // 发送 JSON 数据
    async void SendJson(string json)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.SendText(json);
        }
    }

    private async void OnApplicationQuit()
    {
        _isClosing = true;
        await _webSocket.Close();
    }
}

[System.Serializable]
public class BliveDmGetterData
{
    public int cmd;
    public JToken data;
}
