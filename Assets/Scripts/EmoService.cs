using System;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class EmoService : SingletonMono<EmoService>
{
    private const string APIKey = "sk-2f83559ab5d64eb0b8c59e24b664c8a7";

    private const string SystemContent =
        "你是一个文本情绪分类器。你的任务是: 接收一句话，只能从以下情感类别中选择最符合的一项作为输出: 中性、高兴、赞扬、相信、祝愿、悲伤、失望、害羞、怀疑、惊奇。" +
        "输出时只返回类别词，不要解释，不要输出概率，不要输出其它文字。";

    
    /// <summary>
    /// 通过async/await方式访问Deepseek API
    /// </summary>
    /// <param name="text">用户输入的文字</param>
    /// <returns> Deepseek API返回的文字回复</returns>
    public async Task<string> EmotextAsync(string text)
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
                    new DeepseekRequestDataMessage { role = "system", content = SystemContent},
                    new DeepseekRequestDataMessage { role = "user", content = text}
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
    /// <param name="text"> 用户输入的文字</param>
    /// <param name="callback"> 回调函数，函数参数接收Deepseek API返回的文字回复</param>
    /// <returns></returns>
    public IEnumerator Emotext(string text, Action<string> callback)
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
                    new DeepseekRequestDataMessage { role = "system", content = SystemContent},
                    new DeepseekRequestDataMessage { role = "user", content = text}
                },
                stream = false
            };
            // 将请求数据转换为字节数组
            byte[] bodyRow = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestData));
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
