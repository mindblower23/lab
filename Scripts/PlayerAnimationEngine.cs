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
    public static string GetAnimation(Emotion emotion, string baseAnimation, AnimationNodeStateMachine animationNodeStateMachine)
    {
        ChangeAnimationResource(animationNodeStateMachine, GetAnimationKey(emotion, baseAnimation), baseAnimation);
        return baseAnimation;
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
