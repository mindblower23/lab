using System.Collections.Generic;
using Godot;

public static class PlayerAnimationEngine
{
    private const string AnimationLibraryPrefix = "PlayerMotions/";
    public enum Emotion
	{
		Neutral,
		Happy,
		Sad,
		Angry,
		Surprised
	}

    public readonly record struct EmotionSettings(
        Emotion Emotion,
        float Speed,
        float XFade
    );

    private static readonly Dictionary<Emotion, EmotionSettings> EmotionSettingsMap = new()
{
    [Emotion.Neutral] = new(Emotion.Neutral, 2.0f, 0.2f),
    [Emotion.Happy] = new(Emotion.Happy, 1.2f, 0.15f),
    [Emotion.Sad] = new(Emotion.Sad, 0.8f, 0.3f),
    [Emotion.Angry] = new(Emotion.Angry, 1.4f, 0.1f),
    [Emotion.Surprised] = new(Emotion.Surprised, 1.1f, 0.25f)
};

    public static (string animationName, float speed, float xFade) GetAnimation(Emotion emotion, string baseAnimation, AnimationNodeStateMachine animationNodeStateMachine)
    {
        ChangeAnimationResource(animationNodeStateMachine, GetAnimationKey(emotion, baseAnimation), baseAnimation);
        var emotionSetting = EmotionSettingsMap.GetValueOrDefault(emotion);
        return (baseAnimation, emotionSetting.Speed, emotionSetting.XFade);
    }
    private static string GetAnimationKey(Emotion emotion, string baseAnimation)
    {
        return $"{AnimationLibraryPrefix}{GetAnimationName(emotion, baseAnimation)}";
    }
    private static string GetAnimationName(Emotion emotion, string baseAnimation)
    {
        switch (emotion)
        {
            case Emotion.Happy:
                return $"{baseAnimation}_Happy";
            case Emotion.Sad:
                return $"{baseAnimation}_Sad";
            case Emotion.Angry:
                return $"{baseAnimation}_Angry";
            case Emotion.Surprised:
                return $"{baseAnimation}_Surprised";
            default:
                return $"{baseAnimation}_Neutral";
        }
    }
    private static void ChangeAnimationResource(AnimationNodeStateMachine animationNodeStateMachine, string animationName, string nodeName)
    {
        var animNode = (AnimationNodeAnimation)animationNodeStateMachine.GetNode(nodeName);
        animNode.Animation = animationName;
    }
}
