using Godot;
using System.Collections.Generic;

public partial class WeaponManager : Node3D
{
	[Export] public WeaponResource[] WeaponList;
	private int _currentWeaponIndex = 0;
	
	public WeaponResource GetCurrentWeapon() => WeaponList[_currentWeaponIndex];

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("next_weapon")) // Map this in Input Map
		{
			ChangeWeapon((_currentWeaponIndex + 1) % WeaponList.Length);
		}
	}

	private void ChangeWeapon(int index)
	{
		_currentWeaponIndex = index;
		GD.Print("Equipped: " + GetCurrentWeapon().WeaponName);
		// Here is where you would trigger your "Swap" animation later!
	}
}
