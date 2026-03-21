using Godot;
using System;

public partial class Bullet : Node3D
{
	// 1. Move variables inside the class
	[Export] public float Speed = 40.0f;
	
	private RayCast3D _ray;
	private MeshInstance3D _mesh;

	public override void _Ready()
	{
		// 2. Initialize your nodes (make sure the names match your Bullet scene)
		_ray = GetNode<RayCast3D>("RayCast3D");
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
	}

	public override void _Process(double delta)
	{
		// 3. Use Capitalized properties and 'new Vector3'
		// 4. Use (float)delta because Speed and Vector3 use floats, but delta is a double
		Position += Transform.Basis * new Vector3(0, 0, -Speed) * (float)delta;
		
		// 5. Check for hit (Simple Raycast logic)
		if (_ray.IsColliding())
		{
			GD.Print("Hit something!");
			QueueFree(); // Destroy the bullet
		}
	}
}
