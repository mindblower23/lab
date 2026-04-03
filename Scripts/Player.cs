using Godot;
using System;

public partial class Player : Node3D
{
	private bool wasLeftPressed = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		bool isPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		if (isPressed && !wasLeftPressed)
		{
			GD.Print(GetViewport().GetMousePosition());
			Vector3 hitPos = ShootRay(GetViewport().GetMousePosition());
			if (hitPos != Vector3.Zero)
			{
				GlobalPosition = hitPos;
			}
		}
		wasLeftPressed = isPressed;
	}

	public Vector3 ShootRay(Vector2 mousePosition)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			GD.PrintErr("No camera found!");
			return Vector3.Zero;
		}

		Vector3 from = camera.ProjectRayOrigin(mousePosition);
		Vector3 dir = camera.ProjectRayNormal(mousePosition);
		Vector3 to = from + dir * 1000.0f;

		GD.Print("Ray from: " + from + " to: " + to);

		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);

		var result = space.IntersectRay(query);
		if (result.Count > 0)
		{
			return (Vector3)result["position"];
		}
		else
		{
			GD.Print("No hit detected");
			return Vector3.Zero;
		}
	}
}
