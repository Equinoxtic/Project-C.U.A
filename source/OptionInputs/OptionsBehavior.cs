using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using System;

public partial class OptionsBehavior : MusicBeatBehavior
{
	[Export]
	Array<Node> options;

	int curSelection;

	Sprite2D background;
	Sprite2D checkerboard;

	Label description;

	Camera2D camera;
	float expectedY;

	double holdDelay = 0.5;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		camera = GetNode<Camera2D>("Camera2D");

		background = GetNode<Sprite2D>("BgLayer/Bg");
		checkerboard = GetNode<Sprite2D>("BgLayer/Checkerboard");

		description = GetNode<Label>("DescriptionLayer/Description");

		switchSelection(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		camera.Position = camera.Position.Lerp(new Vector2(0, expectedY), (float)delta / (1f/60f) * 0.1f);

		checkerboard.Position -= new Vector2(50f * (float)delta, 0);
		if (checkerboard.Position.X == 0)
			checkerboard.Position = new Vector2(1280, 0);
		
		if (Input.IsActionJustPressed("uiDown"))
			switchSelection(1);
		if (Input.IsActionJustPressed("uiUp"))
			switchSelection(-1);

		// oh my god kill myself
		if (Input.IsActionJustPressed("uiLeft"))
		{
			Node currentOption = options[curSelection];

			if (currentOption.GetType().Equals(typeof(Numbers)))
				((Numbers)currentOption).switchValue(true);
			
			holdDelay = 0.5;
		}
		if (Input.IsActionJustPressed("uiRight"))
		{
			Node currentOption = options[curSelection];

			if (currentOption.GetType().Equals(typeof(Numbers)))
				((Numbers)currentOption).switchValue(false);

			holdDelay = 0.5;
		}
		if (Input.IsActionPressed("uiLeft"))
		{
			Node currentOption = options[curSelection];

			holdDelay -= delta;
			if (holdDelay <= 0)
			{
				if (currentOption.GetType().Equals(typeof(Numbers)))
					((Numbers)currentOption).switchValue(true);
			}
		}
		if (Input.IsActionPressed("uiRight"))
		{
			Node currentOption = options[curSelection];

			holdDelay -= delta;
			if (holdDelay <= 0)
			{
				if (currentOption.GetType().Equals(typeof(Numbers)))
					((Numbers)currentOption).switchValue(false);
			}
		}
		//

		if (Input.IsActionJustPressed("uiAccept"))
		{
			Node currentOption = options[curSelection];

			if (currentOption.GetType().Equals(typeof(Checkbox)))
				((Checkbox)currentOption).accept();
			if (currentOption.GetType().Equals(typeof(Controls)))
				((Controls)currentOption).accept();
			if (currentOption.GetType().Equals(typeof(StateSwitch)))
				((StateSwitch)currentOption).accept();
		}
		if (Input.IsActionJustPressed("uiEscape"))
		{
			persistentAudio.cancelNoise.Play();
			applySpecialChanges();
			GetTree().ChangeSceneToFile("res://Scenes/OptionMenus/OptionCategoriesMenu.tscn");
		}
	}

	void applySpecialChanges()
	{
		for (int i = 0; i < options.Count; i++)
		{
			Node currentOption = options[i];

			if (currentOption.GetType().Equals(typeof(Checkbox)))
				((Checkbox)currentOption).specialSelection();
			if (currentOption.GetType().Equals(typeof(Numbers)))
				((Numbers)currentOption).specialSwitch();
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
			Node selectedOption = options[i];

			if (i == curSelection)
			{
				description.Text = (string)selectedOption.Get("description");
				description.ResetSize();
				description.Position = new Vector2((1280 - description.Size.X) / 2, 673);

				Label selectedLabel = selectedOption.GetNode<Label>("Label");
				selectedLabel.Modulate = new Color("yellow");
				expectedY = selectedLabel.Position.Y - 312;
				continue;
			}
			
			selectedOption.GetNode<Label>("Label").Modulate = new Color("white");
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
