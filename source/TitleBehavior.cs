using Discord;
using Godot;
using System;

public partial class TitleBehavior : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _Notification(int what)
    {
        base._Notification(what);

		switch (what)
		{
			case (int)MainLoop.NotificationApplicationFocusOut:
				GetTree().Paused = true;
				break;

			case (int)MainLoop.NotificationApplicationFocusIn:
				GetTree().Paused = false;
				break;
		}
    }
}
