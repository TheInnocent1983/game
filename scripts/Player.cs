using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// 'Export' let's you change these values in the Godot Inspector
	[ExportGroup("Speed Boost")]
	[Export] public float Speed = 5.0f;
	[Export] public float SprintSpeed = 8.0f;
	[Export] public float CrouchSpeed = 2.5f;
	
	[ExportGroup("Jump Force")]
	[Export] public float JumpVelocity = 4.5f;
	
	[ExportGroup("Mouse Sensitivity")]
	[Export] public float Sensitivity = 0.003f;

	[ExportGroup("Climbing Settings")]
	[Export] public float WallClimbSpeed = 4.0f;
	[Export] public float MaxClimbTime = 3.0f;
	
	[ExportGroup("FOV Settings")]
	[Export] public bool UseDynamicFov = false;
	[Export] public float DefaultFov = 90.0f;
	[Export] public float SprintFov = 120.0f;
	[Export] public float ConstantFov = 100.0f;
	
	// These represents the "Head" and "Camera" nodes
	private Node3D _head;
	private Camera3D _camera;
	private bool _isCrouching = false;
	private float _defaultHeadY;
	private CollisionShape3D _collisionShape;
	private RayCast3D _ceilingChecker;
	private float CrouchHeight = 0.5f;
	private float CrouchTransitionSpeed = 10.0f;
	private float FovChangeSpeed = 8.0f;

	private float _climbTimer = 0.0f;
	private bool _isClimbing = false;
	
	// Gravity pulled from project settings
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	public override void _Ready()
	{
		// Finding our nodes. '$' in GDScript becomes 'GetNode' in C#
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		
		_ceilingChecker = GetNode<RayCast3D>("CeilingChecker");
		
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_defaultHeadY = _head.Position.Y;
	}
	
		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event is InputEventMouseMotion mouseMotion)
			{
				// Rotate the whole body left/right
				RotateY(-mouseMotion.Relative.X * Sensitivity);
				
				// Rotate ONLY the head up/down
				Vector3 headRotation = _head.Rotation;
				headRotation.X -= mouseMotion.Relative.Y * Sensitivity;
				
				// Limit looking up/down so you don't flip upside down
				headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
				_head.Rotation = headRotation;
			}
		}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
    	float fDelta = (float)delta;

    // 1. Check if we can climb
    // Requirements: Touching a wall, pressing "Forward", and has time left
    bool wantsToClimb = IsOnWall() && Input.IsActionPressed("move_forward") && _climbTimer < MaxClimbTime;

    if (wantsToClimb)
    {
        _isClimbing = true;
        _climbTimer += fDelta;
        velocity.Y = WallClimbSpeed; // Move up
    }
    else
    {
        _isClimbing = false;
        // Reset timer only when touching the floor
        if (IsOnFloor())
        {
            _climbTimer = 0.0f;
        }

        // Apply normal gravity if not climbing
        if (!IsOnFloor())
        {
            velocity += GetGravity() * fDelta;
        }
    }

		if (Input.IsActionJustPressed("ui_cancel"))
			GetTree().Quit();
		
		float currentSpeed = Speed;
		
		// 1. Check for Toggle Input
		if (Input.IsActionJustPressed("crouch_toggle"))
			_isCrouching = !_isCrouching;
		
		if (_isCrouching && Input.IsActionJustPressed("crouch_hold"))
		{
			_isCrouching = false;
		}
		
		bool holdingCrouch = Input.IsActionPressed("crouch_hold");
		bool underCeiling = _ceilingChecker.IsColliding();	
		bool wantsToCrouch = _isCrouching || holdingCrouch || underCeiling;
		Vector3 headPos = _head.Position;

		var capsule = _collisionShape.Shape as CapsuleShape3D;
		
		if (wantsToCrouch)
		{
			currentSpeed = CrouchSpeed;
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY - CrouchHeight, (float)delta * CrouchTransitionSpeed);
			
			if (capsule != null)
				capsule.Height = Mathf.Lerp(capsule.Height, 1.0f, (float)delta * CrouchTransitionSpeed);	
		}
		else 
		{
			if (Input.IsActionPressed("sprint"))
				currentSpeed = SprintSpeed;
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY, (float)delta * CrouchTransitionSpeed);
			if (capsule != null)
				capsule.Height = Mathf.Lerp(capsule.Height, 2.0f, (float)delta * CrouchTransitionSpeed);
		}
		_head.Position = headPos;
		
		// 1. Add Gravity if not on the floor
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;
			
		// 2. Handle Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;
			
		// 3. Get Input and calculate direction
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		
		// This math aligns your movement with where you are looking
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			// Smoothly slow down to a stop
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}
		
		float currentFov;
		
		if (!UseDynamicFov) 
			currentFov = ConstantFov;
		else
		{
			bool isSprinting = Input.IsActionPressed("sprint") && direction != Vector3.Zero && !wantsToCrouch;
			currentFov = isSprinting ? SprintFov : DefaultFov;
		}
		_camera.Fov = Mathf.Lerp(_camera.Fov, currentFov, (float)delta * FovChangeSpeed);
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
