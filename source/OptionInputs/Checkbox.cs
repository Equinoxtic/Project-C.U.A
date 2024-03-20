using Godot;
using System;

public partial class Checkbox : Node //could've just make this script for the child but it's easier for ppl to understand if i make it the parent
{
	[Export]
	string settingName;
	[Export]
	string description;

	AnimatedSprite2D checkbox;

	bool selected;

	Settings settings;

    public override void _Ready()
    {
		settings = GetNode<Settings>("/root/Settings");
		checkbox = GetNode<AnimatedSprite2D>("Checkbox");

		if (selected != (bool)settings.Get(settingName))
			accept();
    }

    public void accept()
	{
		selected = !selected;

		if (selected)
		{
			checkbox.Play("selected");
			checkbox.Offset = new Vector2(-5.75f, -39.095f);
		}
		else
		{
			checkbox.Play("unselected");
			checkbox.Offset = new Vector2();
		}

		settings.Set(settingName, selected);
		modifyConfig();
	}

    public void modifyConfig()
    {
        settings.config.SetValue("PlayerSettings", settingName, selected);
		settings.config.Save("user://Settings.cfg");
    }

	public void specialSelection()
	{
		switch (settingName)
		{
			case "vSync":
				if (selected) DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
				else DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

				break;
		}
	}
}
