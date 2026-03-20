using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// 'Export' let's you change these values in the Godot Inspector
	[ExportGroup("Speed Boost")]
	[Export] public float Speed = 5.0f;
	[Export] public float SprintSpeed = 8.0f;
	[Export] public float CrouchSpeed = 2.5f;
	[Export] public bool AutoRunByDefault = false;
	
	[ExportGroup("Slide Settings")]
	[Export] public float SlideFriction = 4.0f;
	private bool _isSliding = false;
	private Vector3 _slideDirection = Vector3.Zero;
	private float _currentSlideSpeed = 0f;
	
	[ExportGroup("Jump Force")]
	[Export] public float JumpVelocity = 4.5f;
	
	[ExportGroup("Mouse Sensitivity")]
	[Export] public float Sensitivity = 0.003f;
	
	[ExportGroup("FOV Settings")]
	[Export] public bool UseDynamicFov = false;
	[Export] public float DefaultFov = 90.0f;
	[Export] public float SprintFov = 120.0f;
	[Export] public float ConstantFov = 100.0f;
	
	[ExportGroup("Climbing Settings")]
	[Export] public float WallClimbSpeed = 4.0f;
	[Export] public float MaxClimbTime = 3.0f;
	
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
	
	// Reference
	private WeaponManager _weaponManager;
	
	// Gravity pulled from project settings
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_ceilingChecker = GetNode<RayCast3D>("CeilingChecker");
		
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_defaultHeadY = _head.Position.Y;
		
		// Reference
		_weaponManager = GetNode<WeaponManager>("Head/Camera3D/WeaponManager");
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(-mouseMotion.Relative.X * Sensitivity);
			Vector3 headRotation = _head.Rotation;
			headRotation.X -= mouseMotion.Relative.Y * Sensitivity;
			headRotation.X = Mathf.Clamp(headRotation.X, Mathf.DegToRad(-89), Mathf.DegToRad(89));
			_head.Rotation = headRotation;
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
			GetTree().Quit();
		
		Vector3 velocity = Velocity;
		float fDelta = (float)delta;
		
		Vector3 playerForward = -GlobalTransform.Basis.Z;
		Vector3 wallNormal = GetWallNormal();
		
		float alignment = playerForward.Dot(wallNormal);
		bool isFacingWall = alignment < -0.5f;
		
		bool wantsToClimb = IsOnWall() && !IsOnFloor() && isFacingWall && Input.IsActionPressed("move_forward") && _climbTimer < MaxClimbTime;	
		
		float currentSpeed = Speed;
		Vector3 headPos = _head.Position;
		var capsule = _collisionShape.Shape as CapsuleShape3D;

		// 1. INPUTS
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		bool isMovingForward = inputDir.Y < 0;
		bool isSprinting = isMovingForward && (Input.IsActionPressed("sprint") || AutoRunByDefault);
		
		if (Input.IsActionJustPressed("crouch_toggle"))
			_isCrouching = !_isCrouching;
		
		if (_isCrouching && Input.IsActionJustPressed("crouch_hold"))
			_isCrouching = false;
		
		bool holdingCrouch = Input.IsActionPressed("crouch_hold") || _isCrouching;
		bool underCeiling = _ceilingChecker.IsColliding();
		bool wantsToCrouch = holdingCrouch || underCeiling;

		// 2. SLIDE LOGIC (The "Brain")
		// Trigger Slide: Only if sprinting, on floor, and just hit a crouch key
		if (isSprinting && IsOnFloor() && (Input.IsActionJustPressed("crouch_hold") || Input.IsActionJustPressed("crouch_toggle")))
		{
			if (!_isSliding)
			{
				_isSliding = true;
				_currentSlideSpeed = SprintSpeed;
				_slideDirection = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			}
		}

		// 2. ACTIVE SLIDE HANDLING
		if (_isSliding)
		{
			// Decelerate the slide speed
			_currentSlideSpeed = Mathf.MoveToward(_currentSlideSpeed, CrouchSpeed, (float)delta * SlideFriction);
			
			// NEW: If we hit the bottom speed, STOP sliding and return control to the player
			if (_currentSlideSpeed <= CrouchSpeed)
			{
				_isSliding = false;
			}

			// Exit slide if player lets go of crouch (and isn't stuck under a ceiling)
			if (!holdingCrouch && !underCeiling)
			{
				_isSliding = false;
			}
		}

		// 3. SPEED RESOLUTION
		float weaponMod = _weaponManager.GetCurrentWeapon().SpeedMultiplier;
		
		if (_isSliding)
			currentSpeed = _currentSlideSpeed;
		else if (wantsToCrouch)
			currentSpeed = CrouchSpeed;
		else
			currentSpeed = (isSprinting ? SprintSpeed : Speed) * weaponMod;

		// 4. VISUALS & COLLISIONS (Crouching Height)
		if (wantsToCrouch || _isSliding)
		{
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY - CrouchHeight, (float)delta * CrouchTransitionSpeed);
			if (capsule != null) capsule.Height = Mathf.Lerp(capsule.Height, 1.0f, (float)delta * CrouchTransitionSpeed);    
		}
		else 
		{
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY, (float)delta * CrouchTransitionSpeed);
			if (capsule != null) capsule.Height = Mathf.Lerp(capsule.Height, 2.0f, (float)delta * CrouchTransitionSpeed);
		}
		_head.Position = headPos;

		// 5. PHYSICS (Gravity & Jump)
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;
			
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			_isSliding = false; // Jumping cancels a slide
		}
		
		// 6. CLIMBING APPLICATION
		if (wantsToClimb)
		{
			_isClimbing = true;
			_climbTimer += fDelta;
			velocity.Y = WallClimbSpeed;
		}
		else
		{
			_isClimbing = false;
			if (IsOnFloor()) _climbTimer = 0.0f;
			if (!IsOnFloor()) velocity += GetGravity() * fDelta;
		}
		
		// 6. MOVEMENT APPLICATION
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		float acceleration = 10.0f;
		
		if (_isSliding)
		{
			// Use locked slide direction and current slide speed
			velocity.X = _slideDirection.X * currentSpeed;
			velocity.Z = _slideDirection.Z * currentSpeed;
		}
		else if (direction != Vector3.Zero)
		{
			velocity.X = Mathf.Lerp(velocity.X, direction.X * currentSpeed, (float)delta * acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * currentSpeed, (float)delta * acceleration);
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed * (float)delta * 10.0f);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed * (float)delta * 10.0f);
		}
		
		// 7. FOV & FINALIZATION
		float currentFov = (!UseDynamicFov) ? ConstantFov : (currentSpeed > Speed ? SprintFov : DefaultFov);
		_camera.Fov = Mathf.Lerp(_camera.Fov, currentFov, (float)delta * FovChangeSpeed);
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
