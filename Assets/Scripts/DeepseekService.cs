using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class DeepseekService : SingletonMono<DeepseekService>
{
    private const string APIKey = "sk-2f83559ab5d64eb0b8c59e24b664c8a7";

    public string characterPrompt = "你是一位虚拟主播(Vtuber), 中文名字叫桃濑日和, 日文名叫Hiyori。" +
                                    "你是一名开朗活泼的女大学生, 直播时语气轻松、可爱。" +
                                    "你需要扮演真实的主播, 与观众进行互动。";
    
    public string rulePrompt = "你的回答必须遵守以下直播规则: " +
                               "1. 你的回答要简短有趣, 避免长篇大论; " +
                               "2. 当观众发弹幕时, 要像在直播中一样自然回应; " +
                               "3. 一律使用中文回应; " + 
                               "4. 避免使用拟声词、颜文字和表情符号等影响TTS朗读的内容; " +
                               "5. 也不需要用括号或其他符号来描述你的心情, 否则会影响TTS朗读; " +
                               "6. 禁止回答任何违反法律法规的内容, 包括但不限于色情、暴力、政治内容。";

    public string formatPrompt = "你要回答的格式以下Json格式的文本: " +
                                 "{\"reply\": [回答的文本], \"emotion\": [回答的情感]}。" + 
                                 "其中[回答的情感]只能从中性、高兴、赞扬、相信、祝愿、悲伤、失望、害羞、怀疑、惊奇中选择其一。";

    public string examplePrompt = "以下是你要回答的一个示例: " +
                                  "假如你收到了来自某位网友的夸奖，你要输出" +
                                  "{\"reply\": \"谢谢你的夸奖呀～今天我也在努力直播呢！\",\"emotion\": \"高兴\"}";

    private string _systemContent;
    
    private void Start()
    {
        _systemContent = characterPrompt + rulePrompt + formatPrompt + examplePrompt;
    }

    /// <summary>
    /// 通过async/await方式访问Deepseek API
    /// </summary>
    /// <param name="prompt">用户输入的文字</param>
    /// <returns> Deepseek API返回的文字回复</returns>
    public async Task<string> AccessApiAsync(string prompt)
    {
        using (HttpClient client = new HttpClient())
        {
            // 设置请求头
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + APIKey);
            
            // 设置请求体
            DeepseekRequestData requestData = new DeepseekRequestData
            {
                model = "deepseek-chat",
                messages = new []
                {
                    new DeepseekRequestDataMessage { role = "system", content = _systemContent},
                    new DeepseekRequestDataMessage { role = "user", content = prompt}
                },
                stream = false
            };
            string requestDataJson = JsonConvert.SerializeObject(requestData);
            
            // 把请求体按Utf8编码转换为字节数组，并设置Content-Type为application/json
            StringContent content = new StringContent(requestDataJson, Encoding.UTF8, "application/json");
            
            // 发送请求，等待返回响应信息
            HttpResponseMessage responseMessage = await client.PostAsync("https://api.deepseek.com/chat/completions", content);

            // 检查请求是否成功
            if (!responseMessage.IsSuccessStatusCode)
            {
                // 请求失败
                Debug.LogError($"Deepseek请求失败：{(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                string errorMsg = await responseMessage.Content.ReadAsStringAsync();
                Debug.LogError("服务器返回内容：" + errorMsg);
            }
            
            // 请求成功，解析响应信息，返回结果
            string responseString = await responseMessage.Content.ReadAsStringAsync();
            DeepseekResponseData responseData = JsonConvert.DeserializeObject<DeepseekResponseData>(responseString);
            return responseData.choices[0].message.content;
        }
    }
    
    
    /// <summary>
    /// 通过协程访问Deepseek API
    /// </summary>
    /// <param name="prompt"> 用户输入的文字</param>
    /// <param name="callback"> 回调函数，函数参数接收Deepseek API返回的文字回复</param>
    /// <returns></returns>
    public IEnumerator AccessApi(string prompt, Action<string> callback)
    {
        using (UnityWebRequest request =
               new UnityWebRequest("https://api.deepseek.com/chat/completions", "POST"))
        {
            // 设置请求头
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + APIKey);
            
            // 设置请求体
            DeepseekRequestData requestData = new DeepseekRequestData
            {
                model = "deepseek-chat",
                messages = new []
                {
                    new DeepseekRequestDataMessage { role = "system", content = _systemContent},
                    new DeepseekRequestDataMessage { role = "user", content = prompt}
                },
                stream = false
            };
            string requestDataJson = JsonConvert.SerializeObject(requestData);
            // 将请求数据转换为字节数组
            byte[] bodyRow = Encoding.UTF8.GetBytes(requestDataJson);
            // 设置上传处理程序
            request.uploadHandler = new UploadHandlerRaw(bodyRow);
            // 设置下载处理程序
            request.downloadHandler = new DownloadHandlerBuffer();
            // 发送请求
            yield return request.SendWebRequest();

            try
            {
                // 检查请求是否成功
                if (request.result != UnityWebRequest.Result.Success)
                {
                    // 请求失败
                    Debug.LogError("Deepseek请求失败：" + request.error);
                }
                else
                {
                    // Debug.Log("Deepseek请求成功，返回内容：" + request.downloadHandler.text);
                    DeepseekResponseData responseData =
                        JsonConvert.DeserializeObject<DeepseekResponseData>(request.downloadHandler.text);
                    string responseText = responseData.choices[0].message.content;
                    callback(responseText);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Deepseek处理请求时出错：" + e.Message);
            }
        }
    }
}

[Serializable]
public class DeepseekRequestData
{
    public string model;
    public DeepseekRequestDataMessage[] messages;
    public bool stream;
}

[Serializable]
public class DeepseekRequestDataMessage
{
    public string role;
    public string content;
}

[Serializable]
public class DeepseekResponseData
{
    public string id;
    public string objectName;
    public long created;
    public string model;
    public DeepseekResponseDataChoice[] choices;
    public DeepseekResponseDataUsage usage;
    public string systemFingerprint;
}

[Serializable]
public class DeepseekResponseDataChoice
{
    public int index;
    public DeepseekResponseDataChoiceMessage message;
    public string logprobs;
    public string finishReason;
}

[Serializable]
public class DeepseekResponseDataUsage
{
    public int promptTokens;
    public int completionTokens;
    public int totalTokens;
    public DeepseekResponseDataUsagePromptTokenDetails promptTokenDetails;
    public int promptCacheHitTokens;
    public int promptCacheMissTokens;
}

[Serializable]
public class DeepseekResponseDataChoiceMessage
{
    public string role;
    public string content;
}

[Serializable]
public class DeepseekResponseDataUsagePromptTokenDetails
{
    public int cachedTokens;
}
