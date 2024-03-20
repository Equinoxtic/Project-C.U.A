using Godot;
using System;

public partial class PersistentMusic : Node
{
	public AudioStreamPlayer mainMenuMusic;
	public AudioStreamPlayer confirmNoise;
	public AudioStreamPlayer scrollNoise;
	public AudioStreamPlayer cancelNoise;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mainMenuMusic = GetNode<AudioStreamPlayer>("MainMenuMusic");
		confirmNoise = GetNode<AudioStreamPlayer>("Confirm");
		scrollNoise = GetNode<AudioStreamPlayer>("Scroll");
		cancelNoise = GetNode<AudioStreamPlayer>("Cancel");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
