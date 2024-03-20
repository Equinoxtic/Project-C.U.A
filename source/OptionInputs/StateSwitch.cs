using Godot;
using System;

public partial class StateSwitch : Node
{
	[Export]
	string state;
	[Export]
	string description;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void accept()
	{
		GetTree().ChangeSceneToFile("res://Scenes/OptionMenus/" + state + ".tscn");
	}
}
