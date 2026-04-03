using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private bool isWalking = false;
	private bool wasLeftPressed = false;
	private NavigationAgent3D _navAgent;
	private Vector3 _hitPos = Vector3.Zero;
	private AnimationTree _animationTree;
	private AnimationNodeStateMachinePlayback _animationStateMachine;
	private float _rotationSpeed = 7.0f;
	
	// Called when the node enters the scene tree for the first time. 
	public override void _Ready()
	{
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_animationStateMachine = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		bool isPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		if (isPressed && !wasLeftPressed)
		{
			GD.Print(GetViewport().GetMousePosition());
			Vector3 hitPos = ShootRay(GetViewport().GetMousePosition());
			if (hitPos != Vector3.Zero)
			{
				_navAgent.TargetPosition = hitPos;
				_hitPos = hitPos;
			}
		}

		wasLeftPressed = isPressed;

		isWalking = HandleNavigation(delta);
		if (_animationStateMachine != null)
		{
			_animationStateMachine.Travel(isWalking ? "PlayerMotions_Walking" : "PlayerMotions_Idle1 2");
		}
	}

	private void HandleMovement(Vector3 velocity)
	{
		Velocity = velocity;
		MoveAndSlide();
	}

	private bool HandleNavigation(double delta)
	{
		if (_navAgent.IsNavigationFinished())
		{
			return false;
		}

		Vector3 nextPathPosition = _navAgent.GetNextPathPosition();
		Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();
		if (direction == Vector3.Zero)
		{
			return false;
		}

		Vector3 faceDirection = new Vector3(direction.X, 0, direction.Z);
		if (faceDirection != Vector3.Zero)
		{
			float targetYaw = Mathf.Atan2(faceDirection.X, faceDirection.Z);
			float currentYaw = GlobalRotation.Y;
			float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, _rotationSpeed * (float)delta);
			GlobalRotation = new Vector3(GlobalRotation.X, newYaw, GlobalRotation.Z);
		}

		float speed = 2.0f; // Adjust speed as needed
		Vector3 velocity = direction * speed;

		_navAgent.SetVelocity(velocity);
		HandleMovement(velocity);
		return true;
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
