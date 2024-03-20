using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class OptionCategoriesBehavior : MusicBeatBehavior
{
	Sprite2D background;
	Sprite2D checkerboard;

	[Export]
	Array<Label> options;
	List<string> originalTexts = new List<string>();

	int curSelection;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		discordSDK.changePresence("In The Options Menu.");

		for (int i = 0; i < options.Count; i++)
			originalTexts.Add(options[i].Text);

		background = GetNode<Sprite2D>("Bg");
		checkerboard = GetNode<Sprite2D>("Checkerboard");

		switchSelection(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		checkerboard.Position -= new Vector2(50f * (float)delta, 0);
		if (checkerboard.Position.X == 0)
			checkerboard.Position = new Vector2(1280, 0);

		if (Input.IsActionJustPressed("uiDown"))
			switchSelection(1);
		if (Input.IsActionJustPressed("uiUp"))
			switchSelection(-1);
		if (Input.IsActionJustPressed("uiAccept"))
		{
			persistentAudio.scrollNoise.Play();
			GetTree().ChangeSceneToFile("res://Scenes/OptionMenus/" + originalTexts[curSelection] + ".tscn");
		}
		if (Input.IsActionJustPressed("uiEscape"))
		{
			persistentAudio.cancelNoise.Play();
			switchState("MainMenu");
		}
	}

	void switchSelection(int hit)
	{
		persistentAudio.scrollNoise.Play();
		
		curSelection += hit;

		if (curSelection >= options.Count)
			curSelection = 0;
		if (curSelection < 0)
			curSelection = options.Count - 1;

		for (int i = 0; i < options.Count; i++)
		{
			if (i == curSelection)
			{
				options[i].Text = "> " + originalTexts[i] + " <";
				options[i].ResetSize();
				options[i].Position = new Vector2((1280 - options[i].Size.X) / 2, options[i].Position.Y);
				continue;
			}
			options[i].Text = originalTexts[i];
			options[i].ResetSize();
			options[i].Position = new Vector2((1280 - options[i].Size.X) / 2, options[i].Position.Y);
		}
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
