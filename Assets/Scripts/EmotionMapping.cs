using UnityEngine;

[System.Serializable]
public class EmotionMapping
{
    public EmotionType emotion;
    public int[] animationIndexes; // 指向 animationClips 的索引
}
