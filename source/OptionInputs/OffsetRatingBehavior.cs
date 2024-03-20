using Godot;
using System;
public partial class OffsetRatingBehavior : MusicBeatBehavior
{
	Sprite2D background;
	Sprite2D checkerboard;

	Label details;

	Sprite2D rating;
	Label combo;

	Viewport viewport;

	bool isHoldingRating; // duhhhh fps stuff IDFKKK
	bool isHoldingCombo;

	Settings settings;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		viewport = GetViewport();

		background = GetNode<Sprite2D>("BgLayer/Bg");
		checkerboard = GetNode<Sprite2D>("BgLayer/Checkerboard");

		settings = GetNode<Settings>("/root/Settings");

		details = GetNode<Label>("Details");

		rating = GetNode<Sprite2D>("Rating");
		rating.Position = settings.ratingPos;

		combo = GetNode<Label>("Combo");
		combo.Position = settings.comboPos;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		checkerboard.Position -= new Vector2(50f * (float)delta, 0);
		if (checkerboard.Position.X == 0)
			checkerboard.Position = new Vector2(1280, 0);

		Vector2 mousePos = viewport.GetMousePosition();

		if (Input.IsActionJustPressed("leftClick"))
		{
			if (mousePos.X >= rating.Position.X - 120.9f && mousePos.X <= rating.Position.X + 120.9f && 
				mousePos.Y >= rating.Position.Y - 45.6f && mousePos.Y <= rating.Position.Y + 45.6f)
				isHoldingRating = true;
			else if (mousePos.X >= combo.Position.X && mousePos.X <= combo.Position.X + combo.Size.X && 
				mousePos.Y >= combo.Position.Y && mousePos.Y <= combo.Position.Y + combo.Size.Y)
				isHoldingCombo = true;
		}
		if (Input.IsActionJustPressed("leftClick"))
		{
			isHoldingRating = false;
			isHoldingCombo = false;
		}
		if (isHoldingRating)
		{
			rating.Position = mousePos;
			settings.ratingPos = rating.Position;
		}
		else if (isHoldingCombo)
		{
			combo.Position = mousePos - combo.Size / 2;
			settings.comboPos = combo.Position;
		}

		if (Input.IsActionJustPressed("uiEscape"))
		{
			settings.config.SetValue("PlayerSettings", "ratingPos", rating.Position);
			settings.config.SetValue("PlayerSettings", "comboPos", combo.Position);

			settings.config.Save("user://Settings.cfg");

			persistentAudio.cancelNoise.Play();
			GetTree().ChangeSceneToFile("res://Scenes/OptionMenus/APPEARANCE.tscn");
		}

		details.Text = $"X: {rating.Position.X}, {combo.Position.X}\nY: {rating.Position.Y}, {combo.Position.Y}";
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
