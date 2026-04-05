using Godot;

public partial class SmallBox : Node3D
{
	private Area3D _triggerArea;

	public override void _Ready()
	{
		_triggerArea = GetNode<Area3D>("Area3D2");
		_triggerArea.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		GD.Print("Body entered box area: " + body.Name);

		if (body is Player)
		{
			QueueFree();
		}
	}
}
