using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// 'Export' let's you change these values in the Godot Inspector
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float Sensitivity = 0.003f;
	
	// These represents the "Head" and "Camera" nodes
	private Node3D _head;
	private Camera3D _camera;
	
	// Gravity pulled from project settings
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	public override void _Ready()
	{
		// Finding our nodes. '$' in GDScript becomes 'GetNode' in C#
		_head = GetNode<Node3D>("Head");
		_camera = GetNode<Camera3D>("Head/Camera3D");
		
		// Lock the mouse to the center of the screen
		Input.MouseMode = Input.MouseModeEnum.Captured;
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
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			// Smoothly slow down to a stop
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}
		
		Velocity = velocity;
		MoveAndSlide();
	}
}
