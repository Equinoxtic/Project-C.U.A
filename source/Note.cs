using Godot;
using System;
using System.Collections.Generic;

public partial class Note : Sprite2D
{
	public string noteType = "default";

	public int noteData;
    public float strumTime;
	public bool shouldHit;
    public bool isSustain;
    public bool isSustainEnd;

	Settings settings;

	public override void _Ready()
	{
		settings = GetNode<Settings>("/root/Settings");
	}

	public void resetNote()
	{
		FlipV = false;
		Modulate = new Color(1, 1, 1, 1);

		ZIndex = 2;

		string tail = "";
		if (isSustain)
		{
			tail = " hold piece";
			if (isSustainEnd)
			    tail = " hold end";
            ZIndex = 0;

            Modulate = new Color(1, 1, 1, 0.7f);

			if (settings.downScroll)
			    FlipV = true;
		}

		switch (noteData)
		{
			case 0:
			    Texture = ResourceLoader.Load<Texture2D>($"res://assets/images/noteSkins/{noteType}/purple" + tail + ".png");
			    break;
			case 1:
				Texture = ResourceLoader.Load<Texture2D>($"res://assets/images/noteSkins/{noteType}/blue" + tail + ".png");
			    break;
			case 2:
			    Texture = ResourceLoader.Load<Texture2D>($"res://assets/images/noteSkins/{noteType}/green" + tail + ".png");
			    break;
			case 3:
			    Texture = ResourceLoader.Load<Texture2D>($"res://assets/images/noteSkins/{noteType}/red" + tail + ".png");
			    break;
		}
	}
}