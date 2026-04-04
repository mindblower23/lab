using Godot;
using System;

public partial class Player : CharacterBody3D
{
	private const float MoveSpeed = 2.0f;
	private const float StopDistance = 0.15f;
	private bool isWalking = false;
	private bool wasLeftPressed = false;
	private bool _hasMoveTarget = false;
	private NavigationAgent3D _navAgent;
	private Vector3 _hitPos = Vector3.Zero;
	private AnimationTree _animationTree;
	private AnimationNodeStateMachinePlayback _animationStateMachine;
	private float _rotationSpeed = 7.0f;
	private float _gravity;
	
	// Called when the node enters the scene tree for the first time. 
	public override void _Ready()
	{
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_animationStateMachine = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
		_gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
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
				_hasMoveTarget = true;
			}
		}

		wasLeftPressed = isPressed;

		isWalking = HandleNavigation(delta);
		if (!isWalking)
		{
			HandleMovement(Vector3.Zero, delta);
		}

		if (_animationStateMachine != null)
		{
			_animationStateMachine.Travel(isWalking ? "PlayerMotions_Walking" : "PlayerMotions_Idle1 2");
		}
	}

	private void HandleMovement(Vector3 horizontalVelocity, double delta)
	{
		float verticalVelocity = IsOnFloor() ? 0.0f : Velocity.Y - (_gravity * (float)delta);
		Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
		MoveAndSlide();
	}

	private bool HandleNavigation(double delta)
	{
		if (!_hasMoveTarget)
		{
			return false;
		}

		Vector3 toTarget = _hitPos - GlobalPosition;
		toTarget.Y = 0.0f;
		if (toTarget.LengthSquared() <= StopDistance * StopDistance)
		{
			_hasMoveTarget = false;
			return false;
		}

		Vector3 moveTarget = _hitPos;
		Vector3[] currentPath = _navAgent.GetCurrentNavigationPath();
		foreach (Vector3 pathPoint in currentPath)
		{
			Vector3 pathOffset = pathPoint - GlobalPosition;
			pathOffset.Y = 0.0f;
			if (pathOffset.LengthSquared() > StopDistance * StopDistance)
			{
				moveTarget = pathPoint;
				break;
			}
		}

		Vector3 flatDirection = moveTarget - GlobalPosition;
		flatDirection.Y = 0.0f;
		if (flatDirection.LengthSquared() <= 0.0001f)
		{
			return false;
		}

		Vector3 direction = flatDirection.Normalized();

		Vector3 faceDirection = new Vector3(direction.X, 0, direction.Z);
		if (faceDirection != Vector3.Zero)
		{
			float targetYaw = Mathf.Atan2(faceDirection.X, faceDirection.Z);
			float currentYaw = GlobalRotation.Y;
			float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, _rotationSpeed * (float)delta);
			GlobalRotation = new Vector3(GlobalRotation.X, newYaw, GlobalRotation.Z);
		}

		Vector3 velocity = direction * MoveSpeed;

		HandleMovement(velocity, delta);
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
