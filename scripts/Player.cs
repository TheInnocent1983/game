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
	
	[ExportGroup("Jump Force")]
	[Export] public float JumpVelocity = 4.5f;
	
	[ExportGroup("Mouse Sensitivity")]
	[Export] public float Sensitivity = 0.003f;
	
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
		if (Input.IsActionJustPressed("ui_cancel"))
			GetTree().Quit();
		
		Vector3 velocity = Velocity;
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

		// 3. Get Input and calculate direction
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		bool isMovingForward = inputDir.Y < 0;
		
		if (wantsToCrouch)
		{
			currentSpeed = CrouchSpeed;
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY - CrouchHeight, (float)delta * CrouchTransitionSpeed);
			
			if (capsule != null)
				capsule.Height = Mathf.Lerp(capsule.Height, 1.0f, (float)delta * CrouchTransitionSpeed);	
		}
		else 
		{
			if (isMovingForward && (Input.IsActionPressed("sprint") || AutoRunByDefault))
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
			bool isActuallySprinting = currentSpeed == SprintSpeed && direction != Vector3.Zero;
			currentFov = isActuallySprinting ? SprintFov : DefaultFov;
		}
		_camera.Fov = Mathf.Lerp(_camera.Fov, currentFov, (float)delta * FovChangeSpeed);
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
