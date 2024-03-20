using Godot;
using System;

public partial class Numbers : Node
{
	[Export]
	string settingName;
	[Export]
	string description;

	[Export]
	float min;
	[Export]
	float max;

	[Export]
	float steps;

	float value;
	Label valueText;
	Label label;

	Settings settings;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		settings = GetNode<Settings>("/root/Settings");

		valueText = GetNode<Label>("CurValue");
		label = GetNode<Label>("Label");

		value = (float)settings.Get(settingName);
		valueText.Text = "< " + value.ToString() + " >";
		valueText.ResetSize();
		label.Position = new Vector2(valueText.Size.X + 75f, label.Position.Y);
	}

	public void switchValue(bool left)
	{
		if (left)
			value -= steps;
		else
			value += steps;

		if (value < min)
			value = min;
		if (value > max)
			value = max;

		valueText.Text = "< " + value.ToString() + " >";
		valueText.ResetSize();

		settings.Set(settingName, value);

		label.Position = new Vector2(valueText.Size.X + 75f, label.Position.Y);

		modifyConfig();
	}

	public void modifyConfig()
    {
        settings.config.SetValue("PlayerSettings", settingName, value);
		settings.config.Save("user://Settings.cfg");
    }

	public void specialSwitch()
	{
		switch (settingName)
		{
			case "Fps":
				Engine.MaxFps = (int)value;

				break;
		}
	}
}
