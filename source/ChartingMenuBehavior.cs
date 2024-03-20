using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ChartingMenuBehavior : MusicBeatBehavior
{

	SongPack songData;

	Camera2D camera;

	// top tab buttons
	Button songButton;
	Button sectionButton;
	Button noteButton;

	string curSong;
	LineEdit songLineEdit;

	CheckButton needVoicesToggle;

	float curBpm = 100;
	SpinBox bpmSpinBox;

	SpinBox speedSpinBox;

	OptionButton opponentSelection;
	OptionButton protagonistSelection;

	Button saveButton;
	Button clearButton;

	Button mustHitSectionToggle;

	CheckButton changeBpmToggle;
	SpinBox changeBpmSpinBox;

	SpinBox noteLengthSpinBox;

	Button copySectionButton;
	Button pasteSectionButton;
	Button deleteSectionButton;

	CheckButton protagonistNoteSoundToggle;
	CheckButton opponentNoteSoundToggle;
	//

	// bottom buttons
	Button play_pauseButton;

	Button stopButton;

	bool songIsPlaying;
	float curTime;
	Label curTimeLabel;
	Label totalTimeLabel;
	HSlider timeStamp;
	//

	// Necessaries
	AudioStreamPlayer inst;
	AudioStreamPlayer voices;

	float tickPlayedAtLast;
	AudioStreamPlayer noteTick;

	Sprite2D grid;

	NotePool notePool;
	List<Sprite2D> activeNotes;

	CanvasLayer noteLayer;
	List<float> selectedNote;

	int copiedSectionId;
	List<List<float>> copiedSection;
	//

	// Details
	Label stepLabel;
	Label beatLabel;
	Label sectionLabel;
	int totalNotes;
	Label noteCountLabel;
	//

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		initVars();
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		base._Process(delta);

		if (songIsPlaying)
			curTime = inst.GetPlaybackPosition();
		else if (inst.Stream != null && !songIsPlaying) // to prevent all these calculations from running every frame, idk if it does anything but ehhh
		{
			if (Input.IsActionJustReleased("wheelUp"))
			{
				curTime -= Conductor.stepCrochet * 0.4f / 1000f;
				if (curTime < 0)
					curTime = 0;
			}

			if (Input.IsActionJustReleased("wheelDown"))
				curTime += Conductor.stepCrochet * 0.4f / 1000f;

			if (Input.IsActionJustPressed("leftClick")) // testing testing (im cua 10 seconds after making this test it worked)
			{
				Vector2 curMousePos = GetViewport().GetMousePosition();
				
				if (curMousePos.X > 480f && curMousePos.X < 810f &&
					curMousePos.Y > 40f && curMousePos.Y + camera.Position.Y < 680f)
					interactNote();
			}
		}

		updateSong();
		
		playNoteSound();
	}

	/////////////////////////////////////////////////////////////// Tools
	void initVars()
	{
		camera = GetNode<Camera2D>("Camera2D");

		// Necessaries
		inst = GetNode<AudioStreamPlayer>("Inst");
		voices = GetNode<AudioStreamPlayer>("Voices");
		noteTick = GetNode<AudioStreamPlayer>("NoteTick");

		grid = GetNode<Sprite2D>("Grid");

		notePool = new NotePool(64);
		activeNotes = new List<Sprite2D>();

		noteLayer = GetNode<CanvasLayer>("NoteLayer");

		songData = new SongPack();
		songData.song = new SongData();
		songData.song.notes = new List<SectionData>();
		songData.song.bpm = curBpm; // sets bpm first so it ain't weird???
		Conductor.changeBPM(curBpm);
		Conductor.mapBPMChanges(songData);
		songData.song.speed = 1;
		updateGrid();
		//

		// Top
		songButton = GetNode<Button>("UILayer/SettingsContainer/Bg/Song");
		songButton.Pressed += songButtonCalled;
		sectionButton = GetNode<Button>("UILayer/SettingsContainer/Bg/Section");
		sectionButton.Pressed += sectionButtonCalled;
		noteButton = GetNode<Button>("UILayer/SettingsContainer/Bg/Note");
		noteButton.Pressed += noteButtonCalled;

		songLineEdit = songButton.GetNode<LineEdit>("Panel/SongLineEdit");
		songLineEdit.TextSubmitted += songLineEditCalled;

		needVoicesToggle = songButton.GetNode<CheckButton>("Panel/NeedVoicesToggle");
		needVoicesToggle.Toggled += needVoicesToggleCalled;

		bpmSpinBox = songButton.GetNode<SpinBox>("Panel/BpmSpinBox");
		bpmSpinBox.ValueChanged += bpmSpinBoxCalled;

		speedSpinBox = songButton.GetNode<SpinBox>("Panel/SpeedSpinBox");
		speedSpinBox.ValueChanged += speedSpinBoxCalled;

		opponentSelection = songButton.GetNode<OptionButton>("Panel/OpponentSelection");
		opponentSelection.ItemSelected += opponentSelectionCalled;

		protagonistSelection = songButton.GetNode<OptionButton>("Panel/ProtagonistSelection");
		protagonistSelection.ItemSelected += protagonistSelectionCalled;

		noteLengthSpinBox = noteButton.GetNode<SpinBox>("Panel/NoteLengthSpinBox");
		noteLengthSpinBox.ValueChanged += noteLengthSpinBoxCalled;

		saveButton = songButton.GetNode<Button>("Panel/SaveButton");
		saveButton.Pressed += saveButtonCalled;

		clearButton = songButton.GetNode<Button>("Panel/ClearButton");
		clearButton.Pressed += clearButtonCalled;

		mustHitSectionToggle = sectionButton.GetNode<Button>("Panel/MustHitSectionToggle");
		mustHitSectionToggle.Toggled += mustHitSectionToggleCalled;

		changeBpmToggle = sectionButton.GetNode<CheckButton>("Panel/ChangeBpmToggle");
		changeBpmToggle.Toggled += changeBpmToggleCalled;

		changeBpmSpinBox = sectionButton.GetNode<SpinBox>("Panel/ChangeBpmSpinBox");
		changeBpmSpinBox.ValueChanged += changeBpmSpinBoxCalled;

		copySectionButton = sectionButton.GetNode<Button>("Panel/CopySectionButton");
		copySectionButton.Pressed += copySectionButtonCalled;

		pasteSectionButton = sectionButton.GetNode<Button>("Panel/PasteSectionButton");
		pasteSectionButton.Pressed += pasteSectionButtonCalled;

		deleteSectionButton = sectionButton.GetNode<Button>("Panel/DeleteSectionButton");
		deleteSectionButton.Pressed += deleteSectionButtonCalled;

		protagonistNoteSoundToggle = noteButton.GetNode<CheckButton>("Panel/ProtagonistNoteSoundToggle");
		opponentNoteSoundToggle = noteButton.GetNode<CheckButton>("Panel/OpponentNoteSoundToggle");
		//

		// Bottom
		play_pauseButton = GetNode<Button>("UILayer/TimeStampContainer/Bg/Play_Pause");
		play_pauseButton.Pressed += play_pauseCalled;

		stopButton = GetNode<Button>("UILayer/TimeStampContainer/Bg/Stop");
		stopButton.Pressed += stopButtonCalled;

		curTimeLabel = GetNode<Label>("UILayer/TimeStampContainer/Bg/CurTimeLabel");
		totalTimeLabel = GetNode<Label>("UILayer/TimeStampContainer/Bg/TotalTimeLabel");

		timeStamp = GetNode<HSlider>("UILayer/TimeStampContainer/Bg/Timestamp");
		timeStamp.ValueChanged += timeStampCalled;
		//

		// Details
		stepLabel = GetNode<Label>("UILayer/DetailsContainer/Body/StepLabel");
		beatLabel = GetNode<Label>("UILayer/DetailsContainer/Body/BeatLabel");
		sectionLabel = GetNode<Label>("UILayer/DetailsContainer/Body/SectionLabel");
		noteCountLabel = GetNode<Label>("UILayer/DetailsContainer/Body/NoteCountLabel");
		//
	}
	
	void playNoteSound()
	{
		foreach(Sprite2D note in activeNotes)
		{
			Note noteScript = (Note)note;

			if (noteScript.strumTime <= Conductor.songPosition && noteScript.strumTime >= tickPlayedAtLast)
			{
				if ((noteScript.shouldHit && protagonistNoteSoundToggle.ButtonPressed) || 
					(!noteScript.shouldHit && opponentNoteSoundToggle.ButtonPressed))
				{
					noteTick.Play();
					tickPlayedAtLast = noteScript.strumTime;
				}
			}
		}
	}

    void updateSong()
	{
		curTimeLabel.Text = formatTime((int)curTime);

		timeStamp.SetValueNoSignal(curTime);

		Conductor.songPosition = curTime * 1000f;

		stepLabel.Text = "Step: " + curStep;
		beatLabel.Text = "Beat: " + curBeat;
		sectionLabel.Text = "Section: " + curSection;
		noteCountLabel.Text = "Note Count: " + totalNotes;

		camera.Position = new Vector2(0, getYfromStrum((Conductor.songPosition - sectionStartTime()) % (Conductor.stepCrochet * 16f)) - 40f); // this code is the reason for thhe note flicker, try changing it later will ya
	}

	void addSection() // filler
	{
		SectionData tempoSection = new SectionData();
		tempoSection.mustHitSection = false;
		tempoSection.bpm = curBpm;
		tempoSection.lengthInSteps = 16;
		tempoSection.sectionNotes = new List<List<float>>();
		tempoSection.changeBPM = false;

		songData.song.notes.Add(tempoSection);
	}

	void changeSustain(float value)
	{
		if (selectedNote != null)
		{
			selectedNote[2] = value;
			selectedNote[2] = Mathf.Max(selectedNote[2], 0);
		}

		updateGrid();
	}

	void selectNote(List<float> noteData)
	{
		selectedNote = noteData;
		noteLengthSpinBox.SetValueNoSignal(noteData[2]);
	}

	void updateGrid()
	{
		for (int pSection = 0; pSection < curSection + 1; pSection++) // put it in a for loop incase someone uses the timestamp too quickly
		{
			if (pSection > songData.song.notes.Count - 1)
				addSection();
		}

		foreach(Sprite2D curNote in activeNotes.ToArray())
		{
			notePool.Release(curNote);
			activeNotes.Remove(curNote);
		}
		
		SectionData section = songData.song.notes[curSection];
		for (int i = 0; i < section.sectionNotes.Count; i++)
		{
			float noteStrum = section.sectionNotes[i][0];
			int noteData = (int)section.sectionNotes[i][1];
			float noteSus = section.sectionNotes[i][2];

			Sprite2D note = notePool.Get(); 
			note.Centered = false;
			Note noteScript = (Note)note;
			noteScript.Scale = new Vector2(1f / (157f / 40f), 1f / (154f / 40f));
			noteScript.strumTime = noteStrum;
			noteScript.noteData = noteData % 4;
			noteScript.isSustain = false;
			noteScript.isSustainEnd = false;
			noteScript.shouldHit = section.mustHitSection;
			activeNotes.Add(note);
			if (!noteLayer.HasNode((string)note.Name))
				noteLayer.AddChild(note);
			note.Position = new Vector2(Mathf.Floor(480f + (40f * noteData)), Mathf.Floor(getYfromStrum((noteStrum - sectionStartTime()) % (Conductor.stepCrochet * 16f))));
			noteScript.resetNote();	
			if (noteSus > 0)
			{
				Sprite2D noteSusSprite = notePool.Get();
				noteSusSprite.Centered = false;
				noteSusSprite.Scale = new Vector2(1f / (50f / 20f), 1f / (44f / Mathf.Floor(Mathf.Remap(noteSus, 0, Conductor.stepCrochet * 16f, 0, 640f))));; // change this code later, please
				Note noteSusScript = (Note)noteSusSprite;
				noteSusScript.noteData = noteData % 4;
				noteSusScript.isSustain = true;
				noteSusScript.isSustainEnd = false;
				noteSusScript.strumTime = noteStrum;
				noteSusScript.shouldHit = section.mustHitSection;
				activeNotes.Add(noteSusSprite);
				if (!noteLayer.HasNode((string)noteSusSprite.Name))
					noteLayer.AddChild(noteSusSprite);
				noteSusSprite.Position = new Vector2(Mathf.Floor(490f + (40f * noteData)), note.Position.Y + 40f);
				noteSusScript.resetNote();
			}
		}
	}

    string formatTime(int second)
	{
		string timeString = (second / 60).ToString() + ":";
		int timeStringHelper = second % 60;
		if (timeStringHelper < 10)
			timeString += "0";
		timeString += timeStringHelper;

		return timeString;
	}

	float sectionStartTime() // stolen STRAIGHT from the original chart menu
	{
		float daBPM = songData.song.bpm;
		float daPos = 0;
		for (int i = 0; i < curSection; i++)
		{
			if (songData.song.notes[i].changeBPM)
				daBPM = songData.song.notes[i].bpm;

			daPos += 4f * (1000f * 60f / daBPM);
		}
		return daPos;
	}

	void resetSectionVars()
	{
		mustHitSectionToggle.SetPressedNoSignal(songData.song.notes[curSection].mustHitSection);

		changeBpmSpinBox.Value = songData.song.notes[curSection].bpm;
		changeBpmToggle.ButtonPressed = songData.song.notes[curSection].changeBPM;
	}

	void interactNote()
	{
		float noteStrum = getStrumTime(Mathf.Floor((GetViewport().GetMousePosition().Y + camera.Position.Y) / 40f) * 40f) + sectionStartTime();
		int noteData = Mathf.FloorToInt((GetViewport().GetMousePosition().X - 480) / 40);

		SectionData section = songData.song.notes[curSection];

		List<float> data = [noteStrum, noteData, 0];

		for (int i = 0; i < section.sectionNotes.Count; i++) // checks if this specific note already exists. If so, removes it
		{
			if (data[0] == section.sectionNotes[i][0] && data[1] == section.sectionNotes[i][1]) // Not checking sustain length or else this TERRIBLE code will count it as a new typr of note
			{
				if (!Input.IsActionPressed("control"))
				{
					totalNotes--;
					section.sectionNotes.Remove(section.sectionNotes[i]);
					updateGrid();
				}
				else
					selectNote(section.sectionNotes[i]);

				return;
			}
		}

		totalNotes++;
		section.sectionNotes.Add(data);
		selectNote(data);
		updateGrid();

		//Math.floor(getYfromStrum((daStrumTime - sectionStartTime()) % (Conductor.stepCrochet * _song.notes[curSection].lengthInSteps)));
	}

	float getYfromStrum(float strumTime)
	{
		return Mathf.Remap(strumTime, 0f, 16f * Conductor.stepCrochet, grid.Position.Y, grid.Position.Y + 640f);
	}

	float getStrumTime(float yPos)
	{
		return Mathf.Remap(yPos, grid.Position.Y, grid.Position.Y + 640f , 0, 16f * Conductor.stepCrochet);
	}

    /////////////////////////////////////////////////////////////// Callbacks
    public override void sectionHit()
    {
        base.sectionHit();

		updateGrid();
		resetSectionVars();
    }
    void songButtonCalled() //messy ass codes i can't do this shit no more
	{
		Panel panel = songButton.GetNode<Panel>("Panel");

		panel.Visible = !panel.Visible;
		if (panel.Visible) panel.ProcessMode = ProcessModeEnum.Inherit;
		else panel.ProcessMode = ProcessModeEnum.Disabled;

		Panel sectionPanel = sectionButton.GetNode<Panel>("Panel");
		sectionPanel.Visible = false;
		sectionPanel.ProcessMode = ProcessModeEnum.Disabled;

		Panel notePanel = noteButton.GetNode<Panel>("Panel");
		notePanel.Visible = false;
		notePanel.ProcessMode = ProcessModeEnum.Disabled;
	}

	void sectionButtonCalled()
	{
		Panel panel = sectionButton.GetNode<Panel>("Panel");

		panel.Visible = !panel.Visible;
		if (panel.Visible) panel.ProcessMode = ProcessModeEnum.Inherit;
		else panel.ProcessMode = ProcessModeEnum.Disabled;

		Panel songPanel = songButton.GetNode<Panel>("Panel");
		songPanel.Visible = false;
		songPanel.ProcessMode = ProcessModeEnum.Disabled;
		
		Panel notePanel = noteButton.GetNode<Panel>("Panel");
		notePanel.Visible = false;
		notePanel.ProcessMode = ProcessModeEnum.Disabled;
	}

	void noteButtonCalled()
	{
		Panel panel = noteButton.GetNode<Panel>("Panel");

		panel.Visible = !panel.Visible;
		if (panel.Visible) panel.ProcessMode = ProcessModeEnum.Inherit;
		else panel.ProcessMode = ProcessModeEnum.Disabled;

		Panel songPanel = songButton.GetNode<Panel>("Panel");
		songPanel.Visible = false;
		songPanel.ProcessMode = ProcessModeEnum.Disabled;
		
		Panel sectionPanel = sectionButton.GetNode<Panel>("Panel");
		sectionPanel.Visible = false;
		sectionPanel.ProcessMode = ProcessModeEnum.Disabled;
	}

	void protagonistSelectionCalled(long charIdx)
	{
		songData.song.player1 = protagonistSelection.GetItemText((int)charIdx);
	}

	void opponentSelectionCalled(long charIdx)
	{
		songData.song.player2 = protagonistSelection.GetItemText((int)charIdx);
	}

	void songLineEditCalled(string song)
	{
		if (song != curSong)
		{
			curSong = song;

			inst.Stop();
			voices.Stop();

			tickPlayedAtLast = 0;

			inst.Stream = ResourceLoader.Load<AudioStream>("res://assets/songs/" + curSong.ToLower() + "/Inst.ogg");
			if (FileAccess.FileExists("res://assets/songs/" + curSong.ToLower() + "/Voices.ogg"))
				voices.Stream = ResourceLoader.Load<AudioStream>("res://assets/songs/" + curSong.ToLower() + "/Voices.ogg");

			int lengthOfInst = (int)inst.Stream.GetLength();

			totalTimeLabel.Text = formatTime(lengthOfInst);

			timeStamp.MaxValue = lengthOfInst;

			songData.song.song = curSong;
		}
		songLineEdit.ReleaseFocus();
	}

	void bpmSpinBoxCalled(double bpm)
	{
		curBpm = (float)bpm;

		songData.song.bpm = curBpm;
			
		Conductor.changeBPM(curBpm);

		noteLengthSpinBox.Step = Conductor.stepCrochet;

		bpmSpinBox.ReleaseFocus();
	}

	void speedSpinBoxCalled(double speed)
	{
		songData.song.speed = (float)speed;

		speedSpinBox.ReleaseFocus();
	}

	void noteLengthSpinBoxCalled(double length)
	{
		changeSustain((float)length);
		updateGrid();
	}

	void needVoicesToggleCalled(bool need)
	{
		if (need) voices.VolumeDb = 0;
		else voices.VolumeDb = -80;

		songData.song.needsVoices = need;
	}

	void mustHitSectionToggleCalled(bool must)
	{
		songData.song.notes[curSection].mustHitSection = must;

		updateGrid();
	}

	void timeStampCalled(double value)
	{
		play_pauseButton.ButtonPressed = true;
		play_pauseCalled(); // callback in a callback!

		curTime = (float)value;
	}

	void saveButtonCalled()
	{
		using FileAccess jsonFile = FileAccess.Open("res://assets/" + curSong + ".json", FileAccess.ModeFlags.Write);
		jsonFile.StoreString(JsonConvert.SerializeObject(songData, Formatting.Indented));
	}
	
	void changeBpmToggleCalled(bool change)
	{
		SectionData section = songData.song.notes[curSection];

		section.changeBPM = change;

		if (change)
			Conductor.changeBPM(section.bpm);
		else
		{
			float curBpm = songData.song.bpm;
			for (int i = 0; i < curSection; i++)
			{
				SectionData sectionToCheck = songData.song.notes[i];
				if (sectionToCheck.changeBPM)
					curBpm = sectionToCheck.bpm;
			}

			Conductor.changeBPM(curBpm);
		}

		Conductor.mapBPMChanges(songData);
	}

	void changeBpmSpinBoxCalled(double bpm)
	{
		if (bpm > 0)
			songData.song.notes[curSection].bpm = (float)bpm;

		changeBpmSpinBox.ReleaseFocus();
	}

	void clearButtonCalled()
	{
		songData.song.notes.Clear();
		totalNotes = 0;
		updateGrid();
	}

	void play_pauseCalled() // todo: redo this code
	{		
		tickPlayedAtLast = curTime * 1000f;

		if (!play_pauseButton.ButtonPressed)
		{
			songIsPlaying = true;

			play_pauseButton.Icon = ResourceLoader.Load<Texture2D>("res://assets/images/menus/pause.png");

			inst.Play(curTime);
			voices.Play(curTime);
		}
		else
		{
			songIsPlaying = false;

			play_pauseButton.Icon = ResourceLoader.Load<Texture2D>("res://assets/images/menus/play.png");

			inst.Stop();
			voices.Stop();
		}
	}

	void stopButtonCalled()
	{		
		play_pauseButton.ButtonPressed = true;
		play_pauseCalled();

		curTime = 0;
		tickPlayedAtLast = 0;
		timeStamp.SetValueNoSignal(0);
	}

	void copySectionButtonCalled()
	{
		copiedSection = songData.song.notes[curSection].sectionNotes.ToList();
		copiedSectionId = curSection;
	}

	void pasteSectionButtonCalled()
	{
		int mul = curSection - copiedSectionId;
		foreach (List<float> noteInfo in copiedSection.ToList())
		{
			noteInfo[0] = noteInfo[0] + Conductor.stepCrochet * 16f * mul;
			songData.song.notes[curSection].sectionNotes.Add(noteInfo);
		}

		totalNotes += copiedSection.Count;

		updateGrid();
	}

	void deleteSectionButtonCalled()
	{
		List<List<float>> sectionNotes = songData.song.notes[curSection].sectionNotes;

		totalNotes -= sectionNotes.Count;
		sectionNotes.Clear();

		updateGrid();
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

/* lype oggy

####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBGGGGGBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBGPP555Y5YY5B#BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBGBBBBBBBBBBBBBBBBBBBBBBBBBBBGGGBBBBBB
####BBBBBBBBBBBG5YJJYYY5P55J5B##BBBBBBBBBBBBBGGGGBGGGGGGGBGGGGGPBGGGGG5GBBBGGGGGGBGGBGGGBBG5PBGGGGGG
####BBBBBBBBBB5JJJJJYY5GPPP5JJ5B##BBBBBBBBBBB5GB5G5GBPGBPG5PPPG5G5GGGB5GBBG5GGGPPB5GGPP5BBBPPB5GBPPG
####BBBBBBBBBP?JJ?YP55PPPPP5YJ?5BB#BBBBBBBBBB5PGGBPGBGGGGBGGGGG5BGGGGBPGBBBGGGGGPGPGGPGPBBBGGBPGBGPG
####BBBBBBBBBJ?JJJPP55PPPPPPPPGBBB##BBBBBBBBBGGBBBBBBBBBBBBBBBGGBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBYJJJYGP5PPPPPGBGPPGBG##BBBBBBBBBGGGGGPGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGBBBBBBBBBB
####BBBBBBBBB5JJ?J55JYPYJJJYJYPPGB#BBBBBBBBBGGPJJJJJJPGGPGGGPGGGGGGP?JPJJPYPGGPGGGPGPGGPGGBBBBG5BBPJ
####BBBBBBBBBBYJJJ?YY?????J??JPPP#BBBBBBBBBBGG7J?Y??JJY?J5YJYJP7P5J5~J5!?P75??J?P?Y7Y5?J5GBBBBPJBBP!
####BBBBBBBBBBBPYJJJJJJYYY555GG5GBBBBBBBBBBBGP7Y75???Y5??J7JPYP!5J7G!5G7JG!5!YP!5!J?P5???GBBBB5?BBP7
####BBBBBBBBBBBBBG5YJJYB#GBBGGGGBBBBBBBBBBBBGG5?JJJ?5GPYY5PYY5P5YY5G5PG5PG5P5PP5Y!J?JP5Y5GBBBBGPBBGP
####BBBBBBBBBBBBBBBBGGPGBGBBBBBBBBBBBBBBBBBBBGGGGGPGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGPPPGGGGGGBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
####BBBBBBBBBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
####BBBBBBBBBBGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG
*/