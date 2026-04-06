using Godot;
using System;

public partial class Camera : Camera3D
{
	[Export] public Node3D Target;
	private float _fixedY;
	private float _fixedZ;
	private const float MoveSpeed = 5.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_fixedY = GlobalPosition.Y;
		_fixedZ = GlobalPosition.Z;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Target != null)
		{
			GlobalPosition = new Vector3(Target.GlobalPosition.X, _fixedY, _fixedZ);
		}
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
