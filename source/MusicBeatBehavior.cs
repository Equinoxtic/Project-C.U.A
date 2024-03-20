using Godot;
using System;

public partial class MusicBeatBehavior : Node
{
	public PersistentMusic persistentAudio;
	public DiscordSDK discordSDK;
	public Transition transition;

	public int curStep;
	public int curBeat;
	public int curSection;

	BPMChangeEvent lastEvent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Conductor._Ready();

		lastEvent = new BPMChangeEvent();

		if (persistentAudio == null)
			persistentAudio = GetNode<PersistentMusic>("/root/MusicNSounds");

		if (discordSDK == null)
			discordSDK = GetNode<DiscordSDK>("/root/DiscordSDK");

		if (transition == null)
			transition = GetNode<Transition>("/root/Transition");
		transition.Play(true);
	}

	int oldStep;
	int oldSection;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Conductor._Process(delta);

		curStep = calcCurStep(Conductor.songPosition);
        curBeat = Mathf.FloorToInt(curStep / 4);
		curSection = Mathf.FloorToInt(curStep / 16);

        if (curStep != oldStep && curStep >= 0)
        {
            oldStep = curStep;
            stepHit();
        }

		if (curSection != oldSection && curSection >= 0)
        {
            oldSection = curSection;
            sectionHit();
        }
	}

	public int calcCurStep(float songPos) // public so chart editor can use it
	{
		for (int i = 0; i < Conductor.bpmChangeMap.Count; i++)
		{
			if (songPos >= Conductor.bpmChangeMap[i].songTime)
				lastEvent = Conductor.bpmChangeMap[i];
		}

		return lastEvent.stepTime + Mathf.FloorToInt((songPos - lastEvent.songTime) / Conductor.stepCrochet);
	}

	public virtual void stepHit()
    {
        if (curStep % 4 == 0)
            beatHit();
    }
    public virtual void beatHit()
    {
        
    }
	public virtual void sectionHit()
    {
        
    }
	public void switchState(string state)
	{
		transition.Play();
		Timer switchTime = new Timer();
		switchTime.WaitTime = 0.5;
		switchTime.OneShot = true;
		switchTime.Timeout += () => GetTree().ChangeSceneToFile("res://Scenes/" + state + ".tscn");
		AddChild(switchTime);
		switchTime.Start();
	}
}
