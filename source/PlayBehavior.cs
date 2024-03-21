using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class PlayBehavior : MusicBeatBehavior
{
	// Important Game Stuff
	public static PlayBehavior instance;

	SubViewport gameView;
	SubViewport hudView;
	SubViewport pauseView;
	Camera2D gameCam;
	Camera2D hudCam;
	Camera2D pauseCam;

	Settings settings;
	//
	
	// Song Info
	Vector2 gameCamZoom;
	Vector2 hudCamZoom;
	bool songStarted;

	public AudioStreamPlayer inst;
	public AudioStreamPlayer voices;
	//
	
	// Chart Stuff
	public SongPack SONG;

	[Export]
	Godot.Collections.Array<AnimatedSprite2D> strumNotes;
	[Export]
	Godot.Collections.Array<AnimatedSprite2D> opponentStrumNotes;
	public NotePool notes;
	public List<Sprite2D> activeNotes = new List<Sprite2D>();
	List<List<dynamic>> strumData = new List<List<dynamic>>();
	//

	// Rating
	Tween ratingTween;
	Tween comboTween;
	Sprite2D ratingSpr;
	Label comboText;
	public int combo;
	public int score;
	public List<float> accuracy = new List<float>() {100};
	public int misses;

	Label scoreTxt;

	public TextureProgressBar healthBar;
	//

	// Characters
	public AnimatedSprite2D opponent;
	public AnimatedSprite2D protagonist;
	public AnimatedSprite2D girlfriend;

	public Sprite2D opponentIcon;
	public Sprite2D protagonistIcon;

	Character opponentScript;
	Character protagonistScript;

	bool hasGf;
	Character girlfriendScript;
	//

	// pause stuff
	[Export]
	Array<Label> pauseOptions;
	
	bool paused;

	int curPausedSelection;

	public override void _Ready()
	{
		base._Ready();

		settings = GetNode<Settings>("/root/Settings");

		setSongData();
		
		discordSDK.changePresence("Now Playing: " + SONG.song.song);

		Label pauseName = pauseCam.GetNode<Label>("Name");
		pauseName.Text = SONG.song.song;
		pauseName.ResetSize();
		pauseName.Position = new Vector2(1280 - pauseName.Size.X - 70, 50);

		Label pauseDiff = pauseCam.GetNode<Label>("Difficulty");
		pauseDiff.Text = Conductor.difficultyNames[Conductor.difficulty];
		pauseDiff.ResetSize();
		pauseDiff.Position = new Vector2(1280 - pauseDiff.Size.X - 71, 121);

		foreach (AnimatedSprite2D strumNoteProtagonist in strumNotes)
		{
			StrumNote strumNoteScript = (StrumNote)strumNoteProtagonist;
			strumNoteScript.setCharacterToSing((Character)protagonist);

			if (!settings.downScroll)
				strumNoteProtagonist.Position = new Vector2(strumNoteProtagonist.Position.X, 100);
			if (settings.middleScroll)
				strumNoteProtagonist.Position = new Vector2(strumNoteProtagonist.Position.X - 320f, strumNoteProtagonist.Position.Y);
		}

		foreach (AnimatedSprite2D strumNoteOpponent in opponentStrumNotes)
		{
			StrumNote strumNoteScript = (StrumNote)strumNoteOpponent;
			strumNoteScript.setCharacterToSing((Character)opponent);

			if (!settings.downScroll)
				strumNoteOpponent.Position = new Vector2(strumNoteOpponent.Position.X, 100);
			if (settings.middleScroll)
				strumNoteOpponent.Hide();
		}

		if (!settings.downScroll)
		{
			scoreTxt.Position = new Vector2(331f, 656.03f);

			healthBar.Position = new Vector2(healthBar.Position.X, 621);
		}

		if (settings.hideHud)
		{
			scoreTxt.Hide();
			healthBar.Hide();
		}

		switchPauseSelection(0);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (!songStarted)
			Conductor.songPosition += (float)delta * 1000f;
		else
		{
			if (!paused)
			{
				if (Input.IsActionJustPressed("uiAccept"))
				{
					GetTree().Paused = true;

					paused = true;

					pauseCam.Visible = true;
					for (int i = 0; i < pauseOptions.Count; i++)
						pauseOptions[i].Visible = true;
				}
			}
			else
			{
				if (Input.IsActionJustPressed("uiDown"))
					switchPauseSelection(1);
				if (Input.IsActionJustPressed("uiUp"))
					switchPauseSelection(-1);
				if (Input.IsActionJustPressed("uiAccept"))
				{
					GetTree().Paused = false;
					switch (pauseOptions[curPausedSelection].Text)
					{
						case "RESUME":
							paused = false;

							pauseCam.Visible = false;
							for (int i = 0; i < pauseOptions.Count; i++)
								pauseOptions[i].Visible = false;

							break;
						
						case "RESTART":
							switchState("GameSongs/" + SONG.song.song);
								
							break;
						
						case "QUIT":
							switchState("MainMenu");
								
							break;
					}
				}

				pauseCam.Position = pauseCam.Position.Lerp(new Vector2(0, pauseOptions[curPausedSelection].Position.Y - 296), (float)delta / (1f / 60f) * 0.1f);
			}

			Conductor.songPosition = inst.GetPlaybackPosition() * 1000f;

			changeScoreText();

			float lerpCamPosWeight = (float)delta / (1f/60f) * 0.2f; // explanatory

			if (curSection < SONG.song.notes.Count)
			{
				if (SONG.song.notes[curSection].mustHitSection)
					gameCam.Position = gameCam.Position.Lerp(protagonist.Position + protagonistScript.charData.cameraOffset, lerpCamPosWeight);
				else
					gameCam.Position = gameCam.Position.Lerp(opponent.Position + opponentScript.charData.cameraOffset, lerpCamPosWeight);
			}
		}

		controlNotes();

		float lerpBeatWeight = (float)delta / (1f/60f) * 0.05f; // responsible for adjusting zooms n stuff after a beat, hence the name

		hudCam.Zoom = hudCam.Zoom.Lerp(hudCamZoom, lerpBeatWeight);
		gameCam.Zoom = hudCam.Zoom.Lerp(hudCamZoom, lerpBeatWeight);

		float xThing = healthBar.Position.X + (healthBar.Size.X * ((float)Mathf.Remap(healthBar.Value / 2f * 100f, 0f, 100f, 100f, 0f) * 0.01f));
		protagonistIcon.Position = new Vector2(xThing + 70f, healthBar.Position.Y);
		opponentIcon.Position = new Vector2(xThing - 70f, healthBar.Position.Y);

		Vector2 supposedIconScale = new Vector2(1, 1);
		protagonistIcon.Scale = protagonistIcon.Scale.Lerp(supposedIconScale, lerpBeatWeight);
		opponentIcon.Scale = opponentIcon.Scale.Lerp(supposedIconScale, lerpBeatWeight);
	}

    public override void stepHit()
    {
        base.stepHit();
    }

    public override void beatHit()
	{
		base.beatHit();

		if (songStarted)
		{
			if (curBeat % 2 == 0)
			{
				opponentScript.dance();
				protagonistScript.dance();

				if (hasGf)
					girlfriendScript.dance();
			}

			Vector2 beatScale = new Vector2(0.2f, 0.2f);
			opponentIcon.Scale += beatScale;
			protagonistIcon.Scale += beatScale;

			if (curSection < SONG.song.notes.Count)
			{
				if (SONG.song.notes[curSection] != null && SONG.song.notes[curSection].changeBPM)
					Conductor.changeBPM(SONG.song.notes[curSection].bpm);
			}
		}
			
		//hudCam.Zoom = new Vector2(1.05f, 1.05f);
		//gameCam.Zoom = new Vector2(1.1f, 1.1f);
	}

    public override void sectionHit()
    {
        base.sectionHit();

		Vector2 beatScale = new Vector2(0.03f, 0.03f);
		gameCam.Zoom += beatScale;
		hudCam.Zoom += beatScale;
    }

    /////////////////////////////////////////////////////////////////////// tools

    string formatTime(int second)
	{
		string timeString = (second / 60).ToString() + ":";
		int timeStringHelper = second % 60;
		if (timeStringHelper < 10)
			timeString += "0";
		timeString += timeStringHelper;

		return timeString;
	}

	void changeScoreText()
	{
		scoreTxt.Text = $"Score: {score} | Misses: {misses} | Acc: {Math.Round(accuracy.Sum() / accuracy.Count, 2)}% | Duration: {formatTime((int)(inst.Stream.GetLength() - inst.GetPlaybackPosition()))}";
		
		discordSDK.changePresence("Now Playing: " + SONG.song.song, scoreTxt.Text);

		scoreTxt.Position = new Vector2((1280f - scoreTxt.Size.X) / 2, scoreTxt.Position.Y);
	}

	void startCountdown()
	{
		Conductor.songPosition -= Conductor.crochet * 5;
		
		int counter = 0;

		Sprite2D countdownSprite = new Sprite2D();
		countdownSprite.Position = new Vector2(640, 360);
		AddChild(countdownSprite);

		AudioStreamPlayer countdownSound = new AudioStreamPlayer();

		Timer countdown = new Timer();
		countdown.WaitTime = Conductor.crochet / 1000;
		countdown.Timeout += () => {
			if (counter % 2 == 0)
			{
				protagonistScript.dance();
				opponentScript.dance();

				if (hasGf)
					girlfriendScript.dance();
			}

			switch (counter)
			{
				case 0:
					countdownSound.Stop();
					countdownSound.Stream = ResourceLoader.Load<AudioStream>("res://assets/sounds/intro3.ogg");
					countdownSound.Play();
					break;
				case 1:
					countdownSprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/ready.png");
					countdownSprite.Modulate = new Color(1, 1, 1, 1);
					CreateTween().TweenProperty(countdownSprite, "modulate:a", 0, 0.2f).SetEase(Tween.EaseType.Out);

					countdownSound.Stop();
					countdownSound.Stream = ResourceLoader.Load<AudioStream>("res://assets/sounds/intro2.ogg");
					countdownSound.Play();
					break;
				case 2:
					countdownSprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/set.png");
					countdownSprite.Modulate = new Color(1, 1, 1, 1);
					CreateTween().TweenProperty(countdownSprite, "modulate:a", 0, 0.2f).SetEase(Tween.EaseType.Out);

					countdownSound.Stop();
					countdownSound.Stream = ResourceLoader.Load<AudioStream>("res://assets/sounds/intro1.ogg");
					countdownSound.Play();
					break;
				case 3:
					countdownSprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/go.png");
					countdownSprite.Modulate = new Color(1, 1, 1, 1);
					CreateTween().TweenProperty(countdownSprite, "modulate:a", 0, 0.2f).SetEase(Tween.EaseType.Out);
		
					countdownSound.Stop();
					countdownSound.Stream = ResourceLoader.Load<AudioStream>("res://assets/sounds/introGo.ogg");
					countdownSound.Play();
					break;
				case 4:
					RemoveChild(countdown);
					RemoveChild(countdownSound);
					RemoveChild(countdownSprite);
					
					countdown.QueueFree();
					countdownSprite.Free();
					countdownSound.Free();

					inst.Finished += onSongEnd;
					inst.Play();
					voices.Play();

					songStarted = true;

					break;
			}
			counter++;
		};
		AddChild(countdown);
		AddChild(countdownSound);
		countdown.Start();

	}

	void setSongData()
	{
		instance = this;

		gameView = GetNode<SubViewport>("Viewports/Game");
		hudView = GetNode<SubViewport>("Viewports/Hud");
		pauseView = GetNode<SubViewport>("Viewports/Pause");

		gameCam = gameView.GetNode<Camera2D>("Camera2D");
		hudCam = hudView.GetNode<Camera2D>("Camera2D");
		pauseCam = pauseView.GetNode<Camera2D>("Camera2D");

		gameCamZoom = gameCam.Zoom;
		hudCamZoom = hudCam.Zoom;
		
		scoreTxt = hudView.GetNode<Label>("InfoDisplay/Text");

		ratingSpr = hudView.GetNode<Sprite2D>("Rating");
		ratingSpr.Position = settings.ratingPos;

		comboText = hudView.GetNode<Label>("Combo");
		comboText.Position = settings.comboPos;

		notes = new NotePool(64);

		SONG = Conductor.loadFromJson(Name);
		Conductor.changeBPM(SONG.song.bpm);
		Conductor.mapBPMChanges(SONG);

		protagonist = gameView.GetNode<AnimatedSprite2D>("Protagonist");
		protagonistScript = (Character)protagonist;
		protagonistScript.setCharacter(SONG.song.player1);
		protagonistIcon = hudView.GetNode<Sprite2D>("ProtagonistIcon");

		opponent = gameView.GetNode<AnimatedSprite2D>("Opponent");
		opponentScript = (Character)opponent;
		opponentScript.setCharacter(SONG.song.player2);
		opponentIcon = hudView.GetNode<Sprite2D>("OpponentIcon");

		if (gameView.HasNode("Girlfriend"))
		{
			girlfriend = gameView.GetNode<AnimatedSprite2D>("Opponent");

			hasGf = true;
			girlfriendScript = (Character)girlfriend;
			girlfriendScript.setCharacter(SONG.song.gfVersion);
		}

		protagonistIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player1 + "/icon.png");
		opponentIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player2 + "/icon.png");

		gameCam.Position = new Vector2(opponent.Position.X + opponentScript.charData.cameraOffset[0], opponent.Position.Y + opponentScript.charData.cameraOffset[1]);

		healthBar = hudView.GetNode<TextureProgressBar>("Healthbar");
		healthBar.TintUnder = new Color(opponentScript.charData.healthBarColor);
		healthBar.TintProgress = new Color(protagonistScript.charData.healthBarColor);
		healthBar.ValueChanged += onHealthValueChanged;

		inst = GetNode<AudioStreamPlayer>("Inst");
		voices = GetNode<AudioStreamPlayer>("Voices");

        inst.Stream = ResourceLoader.Load<AudioStream>("res://assets/songs/" + Name.ToString().ToLower() + "/" + "Inst.ogg");
		voices.Stream = ResourceLoader.Load<AudioStream>("res://assets/songs/" + Name.ToString().ToLower() + "/" + "Voices.ogg");

		foreach (SectionData section in SONG.song.notes)
        {
            foreach (List<float> songNotes in section.sectionNotes)
            {
				bool shouldHit = section.mustHitSection;
				if (songNotes[1] > SONG.song.keys - 1)
					shouldHit = !shouldHit;
				
				List<dynamic> temp = new List<dynamic>(){songNotes[0], songNotes[1], songNotes[2], shouldHit};
				strumData.Add(temp);	
            }
        }
		
		startCountdown();
	}

    public void popUpRating(string rating)
	{
		ratingSpr.Modulate = new Color(1, 1, 1, 1);
		ratingSpr.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/" + rating + ".png");
		if (ratingTween != null)
			ratingTween.Stop();

		ratingTween = CreateTween();
		ratingTween.TweenProperty(ratingSpr, "position", new Vector2(settings.ratingPos[0], settings.ratingPos[1] - 30f), 0.06f);
		ratingTween.TweenProperty(ratingSpr, "position", new Vector2(settings.ratingPos[0], settings.ratingPos[1]), 0.3f).SetEase(Tween.EaseType.Out);
		ratingTween.Parallel().TweenProperty(ratingSpr, "modulate:a", 0, 0.35f).SetEase(Tween.EaseType.Out);

		comboText.Modulate = new Color(1, 1, 1, 1);
		comboText.Text = combo.ToString();
		if (comboTween != null)
			comboTween.Stop();

		comboTween = CreateTween();
		comboTween.TweenProperty(comboText, "position", new Vector2(settings.comboPos[0], settings.comboPos[1] - 40f), 0.06f);
		comboTween.TweenProperty(comboText, "position", new Vector2(settings.comboPos[0], settings.comboPos[1]), 0.3f).SetEase(Tween.EaseType.Out);
		comboTween.Parallel().TweenProperty(comboText, "modulate:a", 0, 0.35f).SetEase(Tween.EaseType.Out);
	}

	void controlNotes()
	{
		foreach (List<dynamic> data in strumData.ToArray())
		{
			float strumTime = data[0];
			int noteData = (int)data[1] % 4;
			float noteLength = data[2];
			bool shouldHit = data[3];

			if (strumTime - Conductor.songPosition < 1800 / SONG.song.speed)
			{
				Sprite2D note = notes.Get();
				Note noteScript = (Note)note;
				noteScript.strumTime = strumTime;
				noteScript.noteData = noteData;
				noteScript.isSustain = false;
				noteScript.isSustainEnd = false;
				noteScript.shouldHit = shouldHit;
				activeNotes.Add(note);
				if (!hudView.HasNode((string)note.Name))
					hudView.AddChild(note);
				noteScript.resetNote();

				if (noteLength > 0)
				{
					for (int k = 0; k < Mathf.FloorToInt(noteLength / Conductor.stepCrochet); k++)
					{
						Sprite2D noteSus = notes.Get();
						Note noteSusScript = (Note)noteSus;
						noteSusScript.noteData = noteData;
						noteSusScript.isSustain = true;
						noteSusScript.isSustainEnd = k == Mathf.FloorToInt(noteLength / Conductor.stepCrochet) - 1;
						if (noteSusScript.isSustainEnd)
							noteSusScript.strumTime = strumTime + (Conductor.stepCrochet * k) + Conductor.stepCrochet * 0.655f;
						else
							noteSusScript.strumTime = strumTime + (Conductor.stepCrochet * k) + Conductor.stepCrochet;
						noteSusScript.shouldHit = shouldHit;
						activeNotes.Add(noteSus);
						if (!hudView.HasNode((string)noteSus.Name))
							hudView.AddChild(noteSus);
						noteSusScript.resetNote();
					}
				}

				strumData.Remove(data);
			}
		}

		foreach (Sprite2D note in activeNotes.ToArray())
		{
			Note noteScript = (Note)note;
			float directionToGo = (Conductor.songPosition - noteScript.strumTime) * (0.45f * SONG.song.speed);
			if (!settings.downScroll)
				directionToGo = -directionToGo;

			if (noteScript.shouldHit)
			{
				AnimatedSprite2D strumNote = strumNotes[noteScript.noteData];
				StrumNote strumNoteScript = (StrumNote)strumNote;

				if (settings.middleScroll)
					note.Show();

				note.Position = new Vector2(strumNote.Position.X,
					strumNote.Position.Y + directionToGo);

				if (!noteScript.isSustain || (noteScript.isSustain && noteScript.isSustainEnd))
					note.Scale = new Vector2(strumNote.Scale.X, strumNote.Scale.Y);
				else
					note.Scale = new Vector2(strumNote.Scale.X, strumNote.Scale.Y * Conductor.stepCrochet / 100f * 1.48f * SONG.song.speed);

				if (noteScript.strumTime < Conductor.songPosition + 160)
				{
					if (noteScript.strumTime > Conductor.songPosition - 200)
					{
						if (!noteScript.isSustain)
						{
							if (strumNoteScript.hitable == null)
								strumNoteScript.hitable = note;
						}
						else
						{
							if (noteScript.strumTime < Conductor.songPosition + 10)
							{
								if (strumNoteScript.hitableSus == null)
									strumNoteScript.hitableSus = note;
							}
						}
					}
					else
					{
						strumNoteScript.missNote();

						if (!((Note)note).isSustain) strumNoteScript.hitable = null;
						else strumNoteScript.hitableSus = null;
						
						PlayBehavior.instance.activeNotes.Remove(note);
						PlayBehavior.instance.notes.Release(note);
					}
				}
			}
			else
			{
				AnimatedSprite2D strumNote = opponentStrumNotes[noteScript.noteData];
				StrumNote opponentStrumNoteScript = (StrumNote)strumNote;

				note.Position = new Vector2(strumNote.Position.X,
					strumNote.Position.Y + directionToGo);

				if (settings.middleScroll)
					note.Hide();

				if (!noteScript.isSustain || (noteScript.isSustain && noteScript.isSustainEnd))
					note.Scale = new Vector2(strumNote.Scale.X, strumNote.Scale.Y);
				else
					note.Scale = new Vector2(strumNote.Scale.X,  strumNote.Scale.Y * Conductor.stepCrochet / 100f * 1.48f * SONG.song.speed);
			

				if (noteScript.strumTime < Conductor.songPosition + 160)
				{
					if (!noteScript.isSustain)
					{
						if (noteScript.strumTime < Conductor.songPosition)
						{
							if (opponentStrumNoteScript.hitable == null)
								opponentStrumNoteScript.hitable = note;
						}
					}
					else
					{
						if (noteScript.strumTime < Conductor.songPosition + 10)
						{
							if (opponentStrumNoteScript.hitableSus == null)
								opponentStrumNoteScript.hitableSus = note;
						}
					}
				}
			}
		}
	}

	void switchPauseSelection(int hit)
	{
		curPausedSelection += hit;

		if (curPausedSelection >= pauseOptions.Count)
			curPausedSelection = 0;
		if (curPausedSelection < 0)
			curPausedSelection = pauseOptions.Count - 1;

		for (int i = 0; i < pauseOptions.Count; i++)
		{
			Label option = pauseOptions[i];

			if (i == curPausedSelection)
			{
				option.Modulate = new Color("yellow");
				continue;
			}
			option.Modulate = new Color("white");
		}
	}


	/////////////////////////////////////////////////////////////////////// callbacks
	void onSongEnd()
	{
		switchState("MainMenu");
	}

	void onHealthValueChanged(double value)
	{
		if (value < 0.4)
			protagonistIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player1 + "/icon-lose.png");;
		if (value > 1.6)
			opponentIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player2 + "/icon-lose.png");
		if (value >= 0.4 && value <= 1.6)
		{
			protagonistIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player1 + "/icon.png");;
			opponentIcon.Texture = ResourceLoader.Load<Texture2D>("res://assets/images/characters/" + SONG.song.player2 + "/icon.png");;
		}

	}

	public override void _Notification(int what)
    {
        base._Notification(what);

		if (!paused)
		{
			switch (what)
			{
				case (int)MainLoop.NotificationApplicationFocusOut:
					GetTree().Paused = true;

					paused = true;

					pauseCam.Visible = true;
					for (int i = 0; i < pauseOptions.Count; i++)
						pauseOptions[i].Visible = true;
						
					break;

				case (int)MainLoop.NotificationApplicationFocusIn:
					GetTree().Paused = false;
					break;
			}
		}
    }
}

public class NotePool
{
	int maxCount = 0;
	int activeCount = 0;

	List<Sprite2D> pool;

	public NotePool(int max)
	{
		maxCount = max;
		pool = new List<Sprite2D>(max);
	}

    public Sprite2D createNote()
	{
		Sprite2D noteTemp = new Sprite2D();	
		ulong objID = noteTemp.GetInstanceId();
		noteTemp.SetScript(ResourceLoader.Load("res://source/Note.cs"));

		return (Sprite2D)GodotObject.InstanceFromId(objID);
	}

	public Sprite2D Get()
	{
		if (activeCount >= pool.Count)
		{
			Sprite2D note = createNote();
			note.ProcessMode = Node.ProcessModeEnum.Inherit;
			pool.Add(note);
			activeCount++;
			return note;
		}
		else
		{
			Sprite2D note = null;
			for (int i = 0; i < pool.Count; i++)
			{
				if (pool[i].ProcessMode == Node.ProcessModeEnum.Disabled)
				{
				    note = pool[i];
					break;
				}
			}
			note.Show();
			note.ProcessMode = Node.ProcessModeEnum.Inherit;
			activeCount++;
			return note;
		}
	}

	public void Release(Sprite2D note)
	{
		if (pool.Count > maxCount)
		{
			pool.Remove(note);
			note.Free();
		}
		else
		{
			note.Hide();
			note.ProcessMode = Node.ProcessModeEnum.Disabled;
		}

		activeCount--;
	}
}