using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord;
using Godot;
using Newtonsoft.Json;

[Serializable]
public class SectionData
{
    public List<List<float>> sectionNotes;
    public int lengthInSteps;
    public bool mustHitSection;
    public float bpm;
    public bool changeBPM;

} 

[Serializable]
public class SongData
{
    public string song;
    public List<SectionData> notes;
    public float bpm;
    public bool needsVoices;
    public float speed;
    public string player1;
    public string player2;
	public string gfVersion;
	public int keys = 4; // todo: 4+ keys support
}

[Serializable]
public class SongPack
{
    public SongData song;
}

[Serializable]
public class CharacterAnimationData
{
	public Vector2 animationOffsets;
    public bool reversed;
}

[Serializable]
public class CharacterData
{
	public Dictionary<string, CharacterAnimationData> animationData;
	public Vector2 cameraOffset;
    public string healthBarColor;
}

public class BPMChangeEvent
{
	public int stepTime;
	public float songTime;
	public float bpm;
}

public class Conductor
{
	public static int difficulty;
	public static List<string> difficultyNames = new List<string>(){"EASY", "NORMAL", "HARD"};

	public static float bpm;
    public static float crochet;
    public static float stepCrochet;
    public static float songPosition;

	public static List<BPMChangeEvent> bpmChangeMap;
	public static void _Ready()
	{
		bpm = 0;
		crochet = 0;
		stepCrochet = 0;
		songPosition = 0;

		bpmChangeMap = new List<BPMChangeEvent>();
	}

	public static void _Process(double delta)
	{
	}

	public static SongPack loadFromJson(string songName, int difficulty = 1)
	{
		Conductor.difficulty = difficulty;
		using FileAccess chartData = FileAccess.Open("res://assets/songs/" + songName + "/" + songName + "-" + difficultyNames[difficulty].ToLower() + ".json", FileAccess.ModeFlags.Read);

		return JsonConvert.DeserializeObject<SongPack>(chartData.GetAsText());
	} 

	public static CharacterData loadCharFromJson(string character)
	{
		using FileAccess charData = FileAccess.Open("res://assets/images/characters/" + character + "/data.json", FileAccess.ModeFlags.Read);

		return JsonConvert.DeserializeObject<CharacterData>(charData.GetAsText());
	}

	public static void changeBPM(float newBpm)
	{
		bpm = newBpm;
		crochet = 60f / newBpm * 1000f;
        stepCrochet = crochet / 4f;
	}

	public static void mapBPMChanges(SongPack SONG)
	{
		float curBPM = SONG.song.bpm;
		int totalSteps = 0;
		float totalPos = 0;
		
		for (int i = 0; i < SONG.song.notes.Count; i++)
		{
			if (SONG.song.notes[i].changeBPM && SONG.song.notes[i].bpm != curBPM)
			{
				curBPM = SONG.song.notes[i].bpm;

				BPMChangeEvent eventBPM = new BPMChangeEvent();
				eventBPM.stepTime = totalSteps;
				eventBPM.songTime = totalPos;
				eventBPM.bpm = curBPM;

				bpmChangeMap.Add(eventBPM);
			}

			totalSteps += 16;
			totalPos += 60f / curBPM * 1000f / 4f * 16f;
		}
	}
}
