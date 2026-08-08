using System.Collections;
using System.Collections.Generic;
using Live2D.Cubism.Framework.Motion;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(CubismMotionController))]
public class MotionController : MonoBehaviour
{
    private CubismMotionController _cubismMotionController;
    
    [Header("动作数据")]
    public MotionData motionData;

    private Dictionary<EmotionType, List<int>> _emotionClipDict;
    
    public Text debugText;
    
    private Coroutine _idleCoroutine;
    private Coroutine _emotionCoroutine;
    
    void Start()
    {
        _cubismMotionController = GetComponent<CubismMotionController>();
        if (motionData == null || motionData.animationClips == null || motionData.animationClips.Length == 0)
        {
            Debug.LogError("MotionData Error");
            return;
        }
        
        // 初始化情绪字典
        _emotionClipDict = motionData.BuildEmotionDict();
        
        // 播放待机动画
        PlayIdleMotion();

    }
    
    private void PlayIdleMotion()
    {
        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
        }
        _idleCoroutine = StartCoroutine(IdleMotionLoop());
    }
    
    private IEnumerator IdleMotionLoop()
    {
        var idleIndexes = motionData.idleAnimationIndexes;

        if (idleIndexes == null || idleIndexes.Length == 0)
        {
            Debug.LogWarning("没有待机动画");
            yield break;
        }

        float fadeBuffer = 0.1f;
        while (true)
        {
            int randomIndex = idleIndexes[Random.Range(0, idleIndexes.Length)];
            AnimationClip clip = motionData.animationClips[randomIndex];
            float clipLength = clip.length;
            _cubismMotionController.PlayAnimation(clip, 0, CubismMotionPriority.PriorityForce, false, 1f);
            debugText.text =
                $"Current Emotion: Idle\nCurrent Motion Index: {randomIndex}\nCurrent Motion Clip: {clip.name}";
            yield return new WaitForSeconds(clipLength - fadeBuffer);
        
        }
    }
    
    /// <summary>
    /// 根据情绪类别播放动画
    /// </summary>
    /// <param name="emotion">情绪类别</param>
    /// <param name="duration">播放时长</param>
    public void PlayEmotionMotion(EmotionType emotion, float duration)
    {
        if (!_emotionClipDict.ContainsKey(emotion) || _emotionClipDict[emotion].Count == 0)
        {
            Debug.LogWarning($"没有找到 {emotion} 对应的动画");
            return;
        }

        // 如果有情绪动画正在播放，先停止
        if (_emotionCoroutine != null)
        {
            StopCoroutine(_emotionCoroutine);
        }

        // 停止待机动画
        if (_idleCoroutine != null)
        {
            StopCoroutine(_idleCoroutine);
        }

        _emotionCoroutine = StartCoroutine(EmotionMotionLoop(emotion, duration));
    }
    
    private IEnumerator EmotionMotionLoop(EmotionType emotion, float duration)
    {
        List<int> emotionIndexes = _emotionClipDict[emotion];
        float endTime = Time.time + duration;

        float fadeBuffer = 0.1f;
        while (Time.time < endTime)
        {
            int randomIndex = emotionIndexes[Random.Range(0, emotionIndexes.Count)];
            var clip = motionData.animationClips[randomIndex];
            float clipLength = clip.length;
            if (Time.time + clipLength < endTime)
            {
                _cubismMotionController.PlayAnimation(clip, 0, CubismMotionPriority.PriorityForce, false, 1f);
                debugText.text =
                    $"Current Emotion: {emotion}\nCurrent Motion Index: {randomIndex}\nCurrent Motion Clip: {clip.name}";
                yield return new WaitForSeconds(clipLength - fadeBuffer);
            }

            if (Time.time + clipLength > endTime)
            {
                _cubismMotionController.PlayAnimation(clip, 0, CubismMotionPriority.PriorityForce, false, 1f);
                debugText.text =
                    $"Current Emotion: {emotion}\nCurrent Motion Index: {randomIndex}\nCurrent Motion Clip: {clip.name}";
                yield return new WaitForSeconds(endTime - Time.time - fadeBuffer);
                // 情绪动画结束后 → 回到待机
                PlayIdleMotion();
            }
        }

        
    }
    
}

