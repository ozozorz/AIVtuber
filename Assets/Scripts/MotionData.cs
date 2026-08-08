using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionData", menuName = "Scriptable Objects/MotionData")]
public class MotionData : ScriptableObject
{
    [Header("所有动作列表")]
    public AnimationClip[] animationClips;

    [Header("待机动作索引")]
    public int[] idleAnimationIndexes;

    [Header("情绪动作映射")]
    public EmotionMapping[] emotionMappings;

    /// <summary>
    /// 生成字典：EmotionType -> 动画索引列表
    /// </summary>
    public Dictionary<EmotionType, List<int>> BuildEmotionDict()
    {
        var dict = new Dictionary<EmotionType, List<int>>();

        foreach (var mapping in emotionMappings)
        {
            if (!dict.ContainsKey(mapping.emotion))
            {
                dict[mapping.emotion] = new List<int>();
            }

            foreach (var index in mapping.animationIndexes)
            {
                if (index >= 0 && index < animationClips.Length)
                {
                    dict[mapping.emotion].Add(index);
                }
                else
                {
                    Debug.LogWarning($"Invalid animation index {index} for emotion {mapping.emotion}");
                }
            }
        }

        return dict;
    }
}
