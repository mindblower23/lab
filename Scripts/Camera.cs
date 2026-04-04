using Godot;
using System;

public partial class Camera : Camera3D
{
	private const float MoveSpeed = 5.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector3 moveDirection = Vector3.Zero;

		if (Input.IsKeyPressed(Key.A))
		{
			moveDirection -= GlobalTransform.Basis.X;
		}

		if (Input.IsKeyPressed(Key.D))
		{
			moveDirection += GlobalTransform.Basis.X;
		}

		if (moveDirection != Vector3.Zero)
		{
			GlobalPosition += moveDirection.Normalized() * MoveSpeed * (float)delta;
		}
	}
}
