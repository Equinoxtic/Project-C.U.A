using Godot;
using System;
using System.Collections.Generic;

public partial class Settings : Node
{
	public ConfigFile config; //saves

	// Gameplay
	public bool downScroll;
	public bool middleScroll;
	public bool ghostTapping = true;
	public bool vSync;
	public float Fps = 30;
	//

	// Appearance
	public bool hideHud;
	public Vector2 ratingPos = new Vector2(536.73f, 325.71f);
	public Vector2 comboPos = new Vector2(436.73f, 385.71f);
	//

    public override void _Ready()
    {
		config = new ConfigFile();
		config.Load("user://Settings.cfg");

		foreach (string section in config.GetSections())
		{
			switch (section)
			{
				case "PlayerSettings":
					foreach (string key in config.GetSectionKeys(section))
					{
						try{
							Set(key, config.GetValue(section, key));
						}
						catch{};
					}
					break;

				case "PlayerControls":
					foreach (string key in config.GetSectionKeys(section))
					{
						try{
							InputMap.ActionEraseEvents(key);
							InputMap.ActionAddEvent(key, (InputEvent)config.GetValue(section, key));
						}
						catch{};
					}
					break;
			}
		}

		Engine.MaxFps = (int)Fps;

		if (vSync) DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
		else DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
    }
}
