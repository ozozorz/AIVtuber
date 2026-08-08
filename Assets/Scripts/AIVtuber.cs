using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MotionController))]
[RequireComponent(typeof(AudioSource))]
public class AIVtuber : MonoBehaviour
{
    private AudioSource _audioSource;
    private MotionController _motionController;
    
    private bool _isTalking = false;
    private BliveDmService _bliveDmService;
    private DeepseekService _deepseekService;
    private GsvService _gsvService;
    private EmoService _emoService;
    private EmotionType _currentEmo;
    private bool _canTalk = false;
    private AudioClip _pendingClip = null;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _motionController = GetComponent<MotionController>();
        _bliveDmService = BliveDmService.Instance;
        _deepseekService = DeepseekService.Instance;
        _gsvService = GsvService.Instance;
        _emoService = EmoService.Instance;
        _currentEmo = EmotionType.中性;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isTalking)
        {
            TryTalk();
        }
    }

    void TryTalk()
    {
        string dm = _bliveDmService.GetDm();
        if (string.IsNullOrEmpty(dm)) return;
        // Talk(dm);
        _ = TalkAsync(dm);
    }

    private async Task TalkAsync(string dm)
    {
        _isTalking = true;
        Debug.Log("回复弹幕: " + dm);

        try
        {
            // 1. 调用Deepseek
            string responseText = await _deepseekService.AccessApiAsync(dm);
            Debug.Log("Deepseek返回: " + responseText);
            
            // 2. 并行调用情绪识别和语音合成
            Task<string> emoTask = _emoService.EmotextAsync(responseText);
            Task<AudioClip> gsvTask = _gsvService.GenerateVoiceAsync(responseText);
            await Task.WhenAll(emoTask, gsvTask);
            
            // 3. 接收情绪结果字符串然后转成Enum
            string emotext = emoTask.Result;
            EmotionType emotion = EmotionType.中性;
            if (Enum.TryParse<EmotionType>(emotext, true, out EmotionType emotionOut))
            {
                emotion = emotionOut;
            }
            
            // 4. 接收合成音频
            AudioClip clip = gsvTask.Result;
            
            // 5. 开始播放音频和动画
            Debug.Log("开始播放语音，情绪: " + emotion);
            _audioSource.clip = clip;
            _audioSource.Play();
            _motionController.PlayEmotionMotion(emotion, clip.length);
            StartCoroutine(WaitForClipEnd(clip.length));


        }
        catch (Exception e)
        {
            Debug.LogError("处理弹幕时出错: " + e.Message);
            _isTalking = false;
        }
    }

    void Talk(string dm)
    {
        _isTalking = true;
        Debug.Log("回复弹幕: " + dm);
        // 将弹幕传入Deepseek API
        StartCoroutine(_deepseekService.AccessApi(dm, OnDeepseekResponse));
    }

    void OnDeepseekResponse(string responseText)
    {
        Debug.Log("Deepseek返回: " + responseText);
        // 将Deepseek返回的文本传入Emo API，返回后得到当前情绪分类
        StartCoroutine(_emoService.Emotext(responseText, OnEmotextResponse));
        // 将Deepseek返回的文本传入语音合成，返回后得到音频文件
        StartCoroutine(_gsvService.GenerateVoice(responseText, OnGsvComplete));
        
    }
    
    void OnEmotextResponse(string emotext)
    {
        if (Enum.TryParse<EmotionType>(emotext, true, out EmotionType emotionOut))
        {
            _currentEmo = emotionOut;
        }
        else
        {
            _currentEmo = EmotionType.中性;
        }
        _canTalk = true;
        TryStartPlayback();
    }

    void OnGsvComplete(AudioClip clip)
    {
        _pendingClip = clip;
        TryStartPlayback();
    }
    
    void TryStartPlayback()
    {
        // 只有情绪分类完成（_canTalk = true）并且语音合成完成（_pendingClip != null）才开始播
        if (_canTalk && _pendingClip != null)
        {
            Debug.Log("开始播放语音，情绪: " + _currentEmo);
            
            // 播放音频
            _audioSource.clip = _pendingClip;
            _audioSource.Play();

            _motionController.PlayEmotionMotion(_currentEmo, _audioSource.clip.length);

            // 播放结束后恢复状态
            StartCoroutine(WaitForClipEnd(_audioSource.clip.length));
            
            // 清理
            _pendingClip = null;
            _canTalk = false;
        }
    }

    IEnumerator WaitForClipEnd(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isTalking = false;
        Debug.Log("语音播放完毕，可以接收下一条弹幕");
    }
}
