using Godot;
using System;

public partial class UIPlayerEmotions : OptionButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Clear();
		string[] names = Enum.GetNames(typeof(PlayerAnimationEngine.Emotion));
		foreach (var name in names)
		{
			AddItem(name);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void _on_item_selected(int index)
	{
		EventBus.Instance.EmitSignal(EventBus.SignalName.EmotionChanged, index);
	}
}
