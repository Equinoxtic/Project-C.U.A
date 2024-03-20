using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public partial class MainMenuBehavior : MusicBeatBehavior
{
	Sprite2D background;
	List<Texture2D> flashTextures = new List<Texture2D>() {
		ResourceLoader.Load<Texture2D>("res://assets/images/menus/menuBG.png"),
		ResourceLoader.Load<Texture2D>("res://assets/images/menus/menuBGMagenta.png")
	};
	Sprite2D checkerboard;

	[Export]
	Array<Label> labels;
	List<string> optionTexts = new List<string>();

	Sprite2D logo;

	int curSelection;

	bool selected = false;
	float flickerDelay = 0.1f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		
		discordSDK.changePresence("In The Main Menu.");

		if (!persistentAudio.mainMenuMusic.Playing)
			persistentAudio.mainMenuMusic.Play();

		for (int i = 0; i < labels.Count; i++)
			optionTexts.Add(labels[i].Text);

		Conductor.changeBPM(102);

		background = GetNode<Sprite2D>("Bg");
		checkerboard = GetNode<Sprite2D>("Checkerboard");

		logo = GetNode<Sprite2D>("Logo");

		switchSelection(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		checkerboard.Position -= new Vector2(50f * (float)delta, 0);
		if (checkerboard.Position.X == 0)
			checkerboard.Position = new Vector2(1280, 0);

		Conductor.songPosition = persistentAudio.mainMenuMusic.GetPlaybackPosition() * 1000f;

		background.Position = background.Position.Lerp(new Vector2(640, 360 - curSelection * 10), (float)delta / (1f/60f) * 0.08f);
		logo.Scale = logo.Scale.Lerp(new Vector2(1, 1), (float)delta / (1f/60f) * 0.05f);

		if (!selected)
		{
			if (Input.IsActionJustPressed("uiDown"))
				switchSelection(1);
			if (Input.IsActionJustPressed("uiUp"))
				switchSelection(-1);
			if (Input.IsActionJustPressed("uiAccept"))
				flickerAndGo();
		}
		else
		{
			flickerDelay -= (float)delta;
			if (flickerDelay <= 0)
			{
				labels[curSelection].Visible = !labels[curSelection].Visible;

				if (labels[curSelection].Visible) //kms
					background.Texture = flashTextures[0];
				else
					background.Texture = flashTextures[1];

				flickerDelay = 0.1f;
			}
		}
			
	}

	void flickerAndGo()
	{
		selected = true;
		persistentAudio.confirmNoise.Play();

		Timer switchTime = new Timer();
		switchTime.WaitTime = 1f;
		switchTime.OneShot = true;

		switch (optionTexts[curSelection])
		{
			case "STORY MODE":
				break;

			case "FREEPLAY":
				switchTime.Timeout += () => {
					persistentAudio.mainMenuMusic.Stop();

					Conductor.difficulty = 1;
					switchState("GameSongs/Brine");
				};
				break;

			case "OPTIONS":
				switchTime.Timeout += () => switchState("OptionMenus/OptionCategoriesMenu");
				break;
		}

		AddChild(switchTime);
		switchTime.Start();
	}

	void switchSelection(int hit)
	{
		persistentAudio.scrollNoise.Play();

		curSelection += hit;

		if (curSelection >= labels.Count)
			curSelection = 0;
		if (curSelection < 0)
			curSelection = labels.Count - 1;

		for (int i = 0; i < labels.Count; i++)
		{
			if (i == curSelection)
			{
				labels[i].Text = optionTexts[i] + " <";
				continue;
			}
			labels[i].Text = optionTexts[i];
		}
	}

    public override void beatHit()
    {
        base.beatHit();
		logo.Scale = new Vector2(1.05f, 1.05f);
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
