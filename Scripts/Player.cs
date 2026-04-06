using System;
using Godot;
/// <summary>
/// Controls a click-to-move character in a 3D scene.
/// The player clicks on the world to set a destination, the navigation agent
/// provides a path around obstacles, and this script moves the body toward
/// the next useful point on that path.
/// The current implementation intentionally keeps movement on the floor plane
/// by removing vertical path differences from the steering calculation, while
/// gravity still handles the body's up/down motion through CharacterBody3D physics.
/// </summary>
public partial class Player : CharacterBody3D
{
	/// <summary>
	/// Particle effect shown at the clicked destination to confirm where the player will move.
	/// </summary>
	[Export] public GpuParticles3D PlayerPositionMarker { get; private set; }

	/// <summary>
	/// Emotion currently used to resolve animation variants through <see cref="PlayerAnimationEngine"/>.
	/// </summary>
	[Export] public PlayerAnimationEngine.Emotion CurrentEmotion { get; private set; } = PlayerAnimationEngine.Emotion.Neutral;

	/// <summary>
	/// High-level locomotion and emote states used to drive the animation state machine.
	/// </summary>
	private enum AnimationState
	{
		/// <summary>
		/// The player is standing still and using its idle animation.
		/// </summary>
		Idle,

		/// <summary>
		/// The player is actively moving toward a navigation target.
		/// </summary>
		Walking,

		/// <summary>
		/// The player is performing the wave emote and temporarily suspends movement.
		/// </summary>
		Emote
	}

	/// <summary>
	/// Animation state selected for the current frame.
	/// </summary>
	private AnimationState _currentAnimationState = AnimationState.Idle;

	/// <summary>
	/// Previously applied animation state, used to avoid redundant state machine transitions.
	/// </summary>
	private AnimationState? _previousAnimationState = AnimationState.Idle;

	/// <summary>
	/// Horizontal movement speed resolved from the active animation data.
	/// </summary>
	private float MoveSpeed = 2.0f;

	/// <summary>
	/// Distance from the destination at which movement is considered complete.
	/// </summary>
	private const float StopDistance = 0.15f;

	/// <summary>
	/// Minimum squared horizontal direction magnitude required before steering is considered valid.
	/// </summary>
	private const float DirectionEpsilon = 0.0001f;

	/// <summary>
	/// Length of the camera ray used to project mouse clicks into the 3D world.
	/// </summary>
	private const float RayLength = 1000.0f;

	/// <summary>
	/// Normal playback speed for non-emote animations.
	/// </summary>
	private const float DefaultAnimationSpeedScale = 1.0f;

	/// <summary>
	/// Base animation key used when the player is walking.
	/// </summary>
	private const string WalkingAnimation = "Walking";

	/// <summary>
	/// Base animation key used when the player is idle.
	/// </summary>
	private const string IdleAnimation = "Idle";

	/// <summary>
	/// Base animation key used when the player is performing an emote.
	/// </summary>
	private const string EmoteAnimation = "Emote";

	/// <summary>
	/// Fallback animation resource used to determine wave timing during initialization.
	/// </summary>
	private const string DefaultEmoteAnimationResource = "PlayerMotions/Emote_Neutral";

	/// <summary>
	/// Navigation agent responsible for generating and exposing the current path.
	/// </summary>
	private NavigationAgent3D _navAgent;

	/// <summary>
	/// Animation player that owns the raw animation resources and playback speed.
	/// </summary>
	private Godot.AnimationPlayer _animationPlayer;

	/// <summary>
	/// Animation tree that blends and transitions between high-level animation states.
	/// </summary>
	private AnimationTree _animationTree;

	/// <summary>
	/// Runtime playback controller for the animation tree's state machine.
	/// </summary>
	private AnimationNodeStateMachinePlayback _animationStateMachinePlayback;

	/// <summary>
	/// Root animation state machine used to look up emotion-specific animation data.
	/// </summary>
	private AnimationNodeStateMachine _animationStateMachine;

	/// <summary>
	/// Last clicked world-space destination the player is moving toward.
	/// </summary>
	private Vector3 _targetPosition = Vector3.Zero;

	/// <summary>
	/// Interpolation speed used when turning the character toward a target direction.
	/// </summary>
	private float _rotationSpeed = 7.0f;

	/// <summary>
	/// Project gravity cached from settings so movement can apply vertical physics consistently.
	/// </summary>
	private float _gravity;

	/// <summary>
	/// Tracks the previous left mouse button state so clicks are only handled once per press.
	/// </summary>
	private bool _wasLeftPressed;

	/// <summary>
	/// Indicates whether a valid movement destination is currently active.
	/// </summary>
	private bool _hasMoveTarget;

	/// <summary>
	/// Lightweight raycast result that keeps the world hit point together with the hit object.
	/// </summary>
	private readonly struct RaycastHit
	{
		/// <summary>
		/// Creates a new raycast result wrapper.
		/// </summary>
		/// <param name="position">World-space hit position.</param>
		/// <param name="collider">Object intersected by the raycast.</param>
		public RaycastHit(Vector3 position, GodotObject collider)
		{
			Position = position;
			Collider = collider;
		}

		/// <summary>
		/// World-space position where the ray intersected the scene.
		/// </summary>
		public Vector3 Position { get; }

		/// <summary>
		/// Scene object hit by the raycast.
		/// </summary>
		public GodotObject Collider { get; }
	}

	/// <summary>
	/// Resolves and caches the child nodes and project settings used during gameplay.
	/// This avoids repeated node lookups every physics frame and stores the project's
	/// default gravity so the player can keep using CharacterBody3D movement naturally.
	/// </summary>
	public override void _Ready()
	{
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_animationPlayer = GetNode<Godot.AnimationPlayer>("AnimationPlayer");
		_animationTree = GetNode<AnimationTree>("AnimationTree");
		_animationTree.Active = true;
		_animationStateMachinePlayback = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
		_animationStateMachine = (AnimationNodeStateMachine)_animationTree.TreeRoot;
		_gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

		EventBus.Instance.EmotionChanged += (newEmotion) =>
		{
			var animationState = _currentAnimationState;
			_currentAnimationState = _currentAnimationState == AnimationState.Emote || _currentAnimationState == AnimationState.Walking
				? AnimationState.Idle
				: AnimationState.Walking;
			UpdateAnimationState();
			_currentAnimationState = animationState;
			CurrentEmotion = (PlayerAnimationEngine.Emotion)newEmotion;
			UpdateAnimationState();
		};

		UpdateAnimationState();

	}

	/// <summary>
	/// Runs once per physics tick to process the full player loop.
	/// The order here is important: first a new click target is captured,
	/// then movement is updated toward that target, then idle movement is applied
	/// when no pathing is active, and finally the animation state is synchronized
	/// with the resulting movement state.
	/// </summary>
	/// <param name="delta">Elapsed physics time since the previous frame.</param>
	public override void _PhysicsProcess(double delta)
	{
		HandleClickInput();

		if (_currentAnimationState == AnimationState.Emote)
		{
			RotateTowardsCamera(delta);
			ApplyMovement(Vector3.Zero, delta);
		}
		else
		{
			bool isWalking = UpdateNavigationMovement(delta);
			_currentAnimationState = isWalking ? AnimationState.Walking : AnimationState.Idle;
			if (!isWalking)
			{
				ApplyMovement(Vector3.Zero, delta);
			}
		}

		UpdateAnimationState();

		MoveAndSlide();
	}

	/// <summary>
	/// Detects a fresh left mouse click and converts the cursor position into a world target.
	/// A target is only updated on the transition from "not pressed" to "pressed"
	/// so the player does not constantly reset the path while the mouse button is held.
	/// When the raycast hits something, both the local target cache and the navigation
	/// agent target are updated so pathfinding and movement stay in sync.
	/// </summary>
	private void HandleClickInput()
	{
		bool isLeftPressed = Input.IsMouseButtonPressed(MouseButton.Left);
		if (isLeftPressed && !_wasLeftPressed)
		{
			RaycastHit? hit = TryShootRay(GetViewport().GetMousePosition());
			if (hit.HasValue)
			{
				if (DidClickPlayer(hit.Value.Collider))
				{
					var animationData = PlayerAnimationEngine.GetAnimation(CurrentEmotion, AnimationState.Emote.ToString(), _animationStateMachine);
					_animationStateMachinePlayback.Travel(animationData.animationName);
					_currentAnimationState = AnimationState.Emote;
					
				}
				else
				{
					ShowPositionMarker(hit.Value.Position);
					StartMovement(hit.Value.Position);
				}
			}
		}

		_wasLeftPressed = isLeftPressed;
	}

	/// <summary>
	/// Moves the click marker effect to the chosen destination and restarts its emission.
	/// </summary>
	/// <param name="position">World-space location where the marker should appear.</param>
	private void ShowPositionMarker(Vector3 position)
	{
		PlayerPositionMarker.GlobalPosition = position;
		PlayerPositionMarker.Restart();
		PlayerPositionMarker.Emitting = true;
	}
	/// <summary>
	/// Starts or resumes navigation toward a clicked world position and interrupts
	/// any currently playing wave animation.
	/// </summary>
	/// <param name="targetPosition">World-space destination chosen by the click.</param>
	private void StartMovement(Vector3 targetPosition)
	{
		_currentAnimationState = AnimationState.Walking;
		_targetPosition = targetPosition;
		_navAgent.TargetPosition = _targetPosition;
		_hasMoveTarget = true;
		_animationPlayer.SpeedScale = DefaultAnimationSpeedScale;
	}


	/// <summary>
	/// Checks whether the clicked collider belongs to this player body or one of its children.
	/// This allows future scene changes to keep working even if the raycast no longer reports
	/// the CharacterBody3D node directly.
	/// </summary>
	/// <param name="collider">Collider returned by the raycast.</param>
	/// <returns>
	/// <c>true</c> when the clicked collider is the player or a descendant of the player.
	/// </returns>
	private bool DidClickPlayer(GodotObject collider)
	{
		if (collider is not Node node)
		{
			return false;
		}

		return node == this || IsAncestorOf(node);
	}

	/// <summary>
	/// Advances the player toward the current destination using the navigation path.
	/// The method first checks whether an active target still exists, then stops movement
	/// once the player is close enough to the final clicked point.
	/// To avoid jitter from path points that are only vertically offset, it steers toward
	/// the first waypoint that has meaningful distance on the XZ plane.
	/// If a usable direction is found, the player rotates smoothly and receives horizontal velocity.
	/// </summary>
	/// <param name="delta">Elapsed physics time since the previous frame.</param>
	/// <returns>
	/// <c>true</c> when the player is currently walking toward a destination;
	/// otherwise <c>false</c>.
	/// </returns>
	private bool UpdateNavigationMovement(double delta)
	{
		if (!_hasMoveTarget)
		{
			return false;
		}

		Vector3 flatTargetOffset = FlattenToFloor(_targetPosition - GlobalPosition);
		if (flatTargetOffset.LengthSquared() <= StopDistance * StopDistance)
		{
			_hasMoveTarget = false;
			return false;
		}

		Vector3 moveTarget = GetNextMoveTarget();
		Vector3 flatDirection = FlattenToFloor(moveTarget - GlobalPosition);
		if (flatDirection.LengthSquared() <= DirectionEpsilon)
		{
			return false;
		}

		Vector3 direction = flatDirection.Normalized();
		RotateTowards(direction, delta);
		ApplyMovement(direction * MoveSpeed, delta);
		return true;
	}

	/// <summary>
	/// Selects the next point the body should steer toward.
	/// The navigation path can sometimes contain points that differ mostly in height,
	/// especially when navigation and body positions are not perfectly aligned.
	/// Those points are ignored here because they produce little or no horizontal movement.
	/// If no path point has meaningful floor-plane distance, the final clicked target
	/// is used as a fallback so the movement logic still has a stable destination.
	/// </summary>
	/// <returns>
	/// A world-space point that should be used as the current steering target.
	/// </returns>
	private Vector3 GetNextMoveTarget()
	{
		foreach (Vector3 pathPoint in _navAgent.GetCurrentNavigationPath())
		{
			if (FlattenToFloor(pathPoint - GlobalPosition).LengthSquared() > StopDistance * StopDistance)
			{
				return pathPoint;
			}
		}

		return _targetPosition;
	}

	/// <summary>
	/// Applies movement to the CharacterBody3D for the current frame.
	/// Horizontal velocity comes from the navigation logic, while vertical velocity
	/// is preserved from gravity unless the body is standing on the floor.
	/// This keeps the character grounded and allows slopes, falling, and floor collision
	/// behavior to continue working through Godot's built-in body movement.
	/// </summary>
	/// <param name="horizontalVelocity">
	/// Desired movement along the floor plane for this frame.
	/// </param>
	/// <param name="delta">Elapsed physics time since the previous frame.</param>
	private void ApplyMovement(Vector3 horizontalVelocity, double delta)
	{
		float verticalVelocity = IsOnFloor() ? 0.0f : Velocity.Y - (_gravity * (float)delta);
		Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
	}

	/// <summary>
	/// Smoothly rotates the player so the character faces its movement direction.
	/// Only yaw is changed here, so the body turns left and right without tilting.
	/// Godot's angle interpolation is used to avoid snapping when the target direction changes.
	/// </summary>
	/// <param name="direction">
	/// Normalized movement direction on the floor plane.
	/// </param>
	/// <param name="delta">Elapsed physics time since the previous frame.</param>
	private void RotateTowards(Vector3 direction, double delta)
	{
		if (direction == Vector3.Zero)
		{
			return;
		}

		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float currentYaw = GlobalRotation.Y;
		float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, _rotationSpeed * (float)delta);
		GlobalRotation = new Vector3(GlobalRotation.X, newYaw, GlobalRotation.Z);
	}

	/// <summary>
	/// Rotates the player toward the active camera while waving so the gesture is
	/// presented toward the viewer instead of keeping the previous move direction.
	/// </summary>
	/// <param name="delta">Elapsed physics time since the previous frame.</param>
	private void RotateTowardsCamera(double delta)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			return;
		}

		Vector3 faceDirection = FlattenToFloor(camera.GlobalPosition - GlobalPosition);
		if (faceDirection.LengthSquared() <= DirectionEpsilon)
		{
			return;
		}

		RotateTowards(faceDirection.Normalized(), delta);
	}

	/// <summary>
	/// Removes the vertical component from a vector so only floor-plane movement remains.
	/// This is used to prevent navigation points above or below the player from injecting
	/// artificial upward or downward steering into the movement direction.
	/// </summary>
	/// <param name="value">The original 3D vector.</param>
	/// <returns>
	/// A copy of the vector with its Y component set to zero.
	/// </returns>
	private static Vector3 FlattenToFloor(Vector3 value)
	{
		value.Y = 0.0f;
		return value;
	}

	/// <summary>
	/// Updates the animation state machine after movement has been processed.
	/// The script currently switches between a walking state and an idle state
	/// based on whether navigation movement is active for the frame.
	/// Keeping this logic in one place makes it easier to extend later with
	/// more movement states such as running, turning, or falling.
	/// </summary>
	private void UpdateAnimationState()
	{
		if (_animationStateMachinePlayback == null)
		{
			return;
		}

		if (_currentAnimationState != _previousAnimationState)
		{
			var animName = _currentAnimationState switch
			{
				AnimationState.Idle => IdleAnimation,
				AnimationState.Walking => WalkingAnimation,
				AnimationState.Emote => EmoteAnimation,
				_ => IdleAnimation
			};
			var animationData = PlayerAnimationEngine.GetAnimation(CurrentEmotion, animName, _animationStateMachine);
			MoveSpeed = animationData.speed;

			_animationStateMachinePlayback.Travel(animationData.animationName);
			_previousAnimationState = _currentAnimationState;
		}
	}
		

	/// <summary>
	/// Casts a ray from the active camera through the mouse cursor into the world.
	/// This is the bridge between a screen-space click and a world-space movement target.
	/// If the ray hits a collider, the hit position is returned; otherwise the method
	/// returns <c>null</c> so callers can distinguish "no hit" from a valid world origin point.
	/// </summary>
	/// <param name="mousePosition">Mouse position in viewport coordinates.</param>
	/// <returns>
	/// The world-space hit position under the mouse cursor, or <c>null</c> if nothing was hit.
	/// </returns>
	private RaycastHit? TryShootRay(Vector2 mousePosition)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			GD.PrintErr("No camera found!");
			return null;
		}

		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);
		Vector3 rayEnd = rayOrigin + (rayDirection * RayLength);

		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
		var result = space.IntersectRay(query);
		if (result.Count == 0 || !result.ContainsKey("position") || !result.ContainsKey("collider"))
		{
			return null;
		}

		if (result["collider"].Obj is not GodotObject collider)
		{
			return null;
		}

		return new RaycastHit((Vector3)result["position"], collider);
	}
	public void StopWaving()
	{
		var animationData = PlayerAnimationEngine.GetAnimation(CurrentEmotion, AnimationState.Idle.ToString(), _animationStateMachine);
		_animationStateMachinePlayback.Travel(animationData.animationName);
	}

	/// <summary>
	/// Handles the signal fired when another body enters the watched area.
	/// If the reported source is an <see cref="Area3D"/>, the area is removed after logging.
	/// </summary>
	/// <param name="body">The body reported by the signal.</param>
	/// <param name="source">The source object associated with the overlap event.</param>
	public void _on_area_3d_2_body_entered(GodotObject body, GodotObject source)
    {
		GD.Print("Body entered: " + body.ToString());
        if (source is Area3D area)
        {
            GD.Print("Entered area: " + area.Name);
			area.QueueFree();
        }
        else
        {
            GD.Print("Entered area: " + body.ToString());
        }

        

    }

	/// <summary>
	/// Logs the terrain type currently being reported by a terrain interaction.
	/// </summary>
	/// <param name="terrainType">The terrain category being announced.</param>
	public void ShoutOutTerrainType(TerrainArea.TerrainType terrainType)
	{
		GD.Print("Shouting out terrain type: " + terrainType);
	}
}
