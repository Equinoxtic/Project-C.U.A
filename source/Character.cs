using Godot;
using System;
using System.Collections.Generic;

public partial class Character : AnimatedSprite2D
{
	[Export]
	bool noMisses;

	public CharacterData charData;

	public float idleTimer;

	bool left;

	public void setCharacter(string character)
	{
		charData = Conductor.loadCharFromJson(character);

		SpriteFrames = ResourceLoader.Load<SpriteFrames>("res://assets/images/characters/" + character + "/anims.tres");
	}

	public void playAnim(string name, bool reversed = false, float idleTimer = 0.2f)
	{
		if (noMisses && name.Contains("miss"))
			return;
			
		this.idleTimer = idleTimer;

		Stop(); 

		if (!reversed)
		    Play(name);
		else
			PlayBackwards(name);

		Offset = charData.animationData[name].animationOffsets;
	}

	public void dance()
	{
		if (!IsPlaying() && idleTimer <= 0)
		{
			if (charData.animationData.ContainsKey("danceLeft") && charData.animationData.ContainsKey("danceRight"))
			{
				if (!left) playAnim("danceLeft");
				else playAnim("danceRight");

				left = !left;
			}
			else playAnim("idle");
		}
	}

    public override void _Process(double delta)
	{
		if (!IsPlaying())
			idleTimer -= (float)delta;
	}
}
