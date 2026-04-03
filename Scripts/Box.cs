using Godot;
using System;

public partial class Box : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// move right at 1 meter per second
		Translate(new Vector3(1f, 0f, 0f) * (float)delta);
	}
}
