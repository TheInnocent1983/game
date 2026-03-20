using Godot;

public partial class WeaponResource : Resource
{
	[Export] public string WeaponName;
	[Export] public float SpeedMultiplier = 1.0f; // 1.2 for Knife
	[Export] public float Damage = 10.0f;
	[Export] public float FireRate = 0.5f;
	[Export] public Mesh WeaponMesh; // The 3D model
}
