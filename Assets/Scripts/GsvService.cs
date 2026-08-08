using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GsvService : SingletonMono<GsvService>
{

    private string getReferWavPathByEmo(EmotionType emo)
    {
        switch (emo)
        {
            case EmotionType.中性:
                return "refer_wav/001.私としては奉仕部が協力してくれるのが一番　面倒がないんです.wav";
            case EmotionType.高兴:
                return "refer_wav/002.そうなんですよね　受け取らないって言ってる以上　難しいんですよね.wav";
            case EmotionType.赞扬:
                return "refer_wav/003.だから先輩も　雪乃先輩も　結衣先輩も　葉山先輩も.wav";
            case EmotionType.相信:
                return "refer_wav/004.大体そういう人って　3回くらい話すと急にこっちを名前呼びし始めて.wav";
            case EmotionType.祝愿:
                return "refer_wav/005.わあ頼れる　そういうことなら次もお願いしますね.wav";
            case EmotionType.愤怒:
                return "refer_wav/006.誰も助けてくれないんですもん　もう先輩達しか頼れないんですよ.wav";
            case EmotionType.悲伤:
                return "refer_wav/007.今　つきあってる人って　いますか.wav";
            case EmotionType.失望:
                return "refer_wav/008.あッ　そうだ　雪ノ下先輩って葉山先輩とつきあってたんですか？.wav";
            case EmotionType.恐惧:
                return "refer_wav/009.ていうかあざとくないですし　むしろ先輩の方があざといですし.wav";
            case EmotionType.害羞:
                return "refer_wav/010.あと　戸部先輩達に頼んでたのとごっちゃになりました.wav";
        }
        return "refer_wav/001.私としては奉仕部が協力してくれるのが一番　面倒がないんです.wav";
    }

    private string getPromptTextByEmo(EmotionType emo)
    {
        switch (emo)
        {
            case EmotionType.中性:
                return "私としては奉仕部が協力してくれるのが一番　面倒がないんです";
            case EmotionType.高兴:
                return "そうなんですよね　受け取らないって言ってる以上　難しいんですよね";
            case EmotionType.赞扬:
                return "だから先輩も　雪乃先輩も　結衣先輩も　葉山先輩も";
            case EmotionType.相信:
                return "大体そういう人って　3回くらい話すと急にこっちを名前呼びし始めて";
            case EmotionType.祝愿:
                return "わあ頼れる　そういうことなら次もお願いしますね";
            case EmotionType.愤怒:
                return "誰も助けてくれないんですもん　もう先輩達しか頼れないんですよ";
            case EmotionType.悲伤:
                return "今　つきあってる人って　いますか";
            case EmotionType.失望:
                return "あッ　そうだ　雪ノ下先輩って葉山先輩とつきあってたんですか？";
            case EmotionType.恐惧:
                return "ていうかあざとくないですし　むしろ先輩の方があざといですし";
            case EmotionType.害羞:
                return "あと　戸部先輩達に頼んでたのとごっちゃになりました";
        }
        return "私としては奉仕部が協力してくれるのが一番　面倒がないんです";
    }

    /// <summary>
    ///  通过asynv/await请求GSV服务
    /// </summary>
    /// <param name="text"> 文本内容 </param>
    /// <returns> 返回音频Clip </returns>
    public async Task<AudioClip> GenerateVoiceAsync(string text, EmotionType emotion = EmotionType.中性)
    {
        string baseUrl = "http://localhost:9880/";
        string referWavPath = Path.Combine(Application.streamingAssetsPath, getReferWavPathByEmo(emotion));
        string promptText = getPromptTextByEmo(emotion);

        string url = baseUrl + "?refer_wav_path=" + referWavPath +
                     "&prompt_text=" + promptText +
                     "&prompt_language=日文" +
                     "&text=" + text +
                     "&text_language=中文" +
                     "&top_k=15&top_p=1&temperature=1&speed=1&sample_steps=32&if_sr=false";

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage responseMessage = await client.GetAsync(url);
            if (!responseMessage.IsSuccessStatusCode)
            {
                Debug.LogError($"GSV请求失败: {responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                return null;
            }

            byte[] audioData = await responseMessage.Content.ReadAsByteArrayAsync();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            AudioClip clip = WavUtility.ToAudioClip(audioData, 0, timestamp.ToString());
            return clip;
        }
    }

    /// <summary>
    /// 通过协程请求GSV服务
    /// </summary>
    /// <param name="text"> 文本内容 </param>
    /// <param name="callback"> 回调函数,函数的参数接收GSV返回的音频</param>
    /// <returns></returns>
    public IEnumerator GenerateVoice(string text, Action<AudioClip> callback)
    {
        string baseUrl = "http://localhost:9880/";
        string referWavPath = Path.Combine(Application.streamingAssetsPath, "refer_wav/001.私としては奉仕部が協力してくれるのが一番　面倒がないんです.wav");
        string promptText = "私としては奉仕部が協力してくれるのが一番　面倒がないんです";

        string url = baseUrl + "?refer_wav_path=" + referWavPath +
                     "&prompt_text=" + promptText +
                     "&prompt_language=日文" +
                     "&text=" + text +
                     "&text_language=中文" +
                     "&top_k=15&top_p=1&temperature=1&speed=1&sample_steps=32&if_sr=false";

        using (UnityWebRequest request = new UnityWebRequest(url, "GET"))
        {
            // 设置下载处理程序
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
            else
            {
                byte[] audioData = request.downloadHandler.data;
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                AudioClip clip = WavUtility.ToAudioClip(audioData, 0, timestamp.ToString());
                callback(clip);
            }
        }
    }
}
