using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

[RequireComponent (typeof(AudioSource))]
public class LoadMidi : MonoBehaviour {
	
	public string testMidiFile = "Midis/Video_Game_Themes_-_Secret_Of_Mana.mid";

	public string bankFilePath = "FM Bank/gm";
	public int bufferSize = 1024;

	private float[] sampleBuffer;
	private float gain = 1f;
	private MidiSequencer midiSequencer;
	private StreamSynthesizer midiStreamSynthesizer;

	private System.Random sysRandom = new System.Random();

	private bool allMute = false;

	private bool generating=false;
	
	private static int frameCount = 0;
	private static int bufferCount = 0;
	private static int nextBlock = 0;

	// temporary hack to get around the input/multithreading junk
	private bool[] alphaNumsDown = new bool[10];

	// To learn a bit about what's being produced by the midi as it plays
	private int minChannel = int.MaxValue;
	private int maxChannel = int.MinValue;
	private int minNote    = int.MaxValue;
	private int maxNote    = int.MinValue;
	
	int getOneCount = 0;
	List<float[]> dataSequence = new List<float[]>();

	// first note is a hack for now to bootstrap the generators
	private List<int> firstNote = new List<int>();
	private List<int> lastLastNote = new List<int>();
	private List<int> lastNote = new List<int>();
	private List<StochasticMatrix> markov = new List<StochasticMatrix>();

	void Awake()
	{
		// For now, imitating the behavior of UnitySynthTest.cs with two goals:
		// 1. Understand how UnitySynth loads and plays midi files
		// 2. Figure out where in that process to get the data for the markov chain

		// copied straight from UnitySynthTest.cs, almost
		midiStreamSynthesizer = new StreamSynthesizer(44100, 2, bufferSize, 40);
		sampleBuffer = new float[midiStreamSynthesizer.BufferSize];

		midiStreamSynthesizer.LoadBank(bankFilePath);
		midiSequencer = new MidiSequencer(midiStreamSynthesizer);
		midiSequencer.LoadMidi(testMidiFile, false);

		midiSequencer.NoteOnEvent += new MidiSequencer.NoteOnEventHandler (MidiNoteOnHandler);
		midiSequencer.NoteOffEvent += new MidiSequencer.NoteOffEventHandler (MidiNoteOffHandler);

		initializeMatrix(16, 128*128,128);
	}

	void Start()
	{

	}

	// Keys:
	//  P/S: 					Play/Stop
	//  N:						Normalize matrix
	//  G:						Toggle song generation on/off (play and stop combined, for generated)
	//  ` (tilde/backquote):	Mute/Unmute all channels
	//							If any channel is not muted, this will mute all. Else it unmutes all.
	//  1-9,0:					Produce the next note on that channel
	void Update()
	{
		frameCount = Time.frameCount;
		// add some play/stop keys since I'm not using the GUILayout stuff.

		if(Input.GetKeyDown(KeyCode.P))
		{
			midiSequencer.Play();
		}
		else if(Input.GetKeyDown(KeyCode.S))
		{
			midiSequencer.Stop(true);
		}
		if(Input.GetKeyDown(KeyCode.D))
		{
			Debug.Log("MinChannel: "+minChannel);
			Debug.Log("MaxChannel: "+maxChannel);
			Debug.Log("MinNote:    "+minNote);
			Debug.Log("MaxNote:    "+maxNote);
		}
		if(Input.GetKeyDown(KeyCode.R))
		{
			displayNextBlock();
		}
		if(Input.GetKeyDown(KeyCode.G))
		{
			generating = !generating;
		}

		if(Input.GetKeyDown(KeyCode.BackQuote))
		{
			if(allMute)
			{
				midiSequencer.UnMuteAllChannels();
			}
			else
			{
				midiSequencer.MuteAllChannels();
				allMute = true;
			}
		}

		for(int key=0; key<10; key++)
		{
			if(Input.GetKeyUp(key.ToString()))
			{
				midiStreamSynthesizer.NoteOff(key, lastNote[key]);
			}
			if(Input.GetKeyDown(key.ToString()))
			{
				int note = getNote(key, lastNote[key], lastLastNote[key]);
				Debug.Log("Generated: "+note+" on channel "+key);
				if(note < 0)
				{
					continue;
				}
				midiStreamSynthesizer.NoteOn(key, note, 127, 0);
				lastNote[key] = note;
			}
		}

		if(Input.GetKeyDown(KeyCode.G))
		{
			int note = getNote(1, lastNote[1], lastLastNote[1]);
			Debug.Log("Generated: "+note+" on channel 1");
			if(note >= 0)
			{
				midiStreamSynthesizer.NoteOn(1, note, 127, 0);
				lastNote[1] = note;
			}
		}
		if(Input.GetKeyUp(KeyCode.G))
		{
			midiStreamSynthesizer.NoteOff(1, lastNote[1]);
		}
	}

	private void checkKeyDown(int key)
	{
		if(key < 10)
		{
			alphaNumsDown[key] = Input.GetKey(key.ToString());
		}
	}

	private void checkSingleMuteKey(int channel)
	{
		if(channel < 10)
		{
			if(Input.GetKeyDown(channel.ToString())){
				if(midiSequencer.isChannelMuted(channel))
				{
					midiSequencer.UnMuteChannel(channel);
					allMute = false;
				}
				else
				{
					midiSequencer.MuteChannel(channel);
					// TODO: Update bookkeeping to so that if you individually mute
					// 		 each channel, allMute = false ends up true.
				}
			}
		}
		else
		{
			// convert to use QWERTY as 10-15
			// Maybe ABCDEF would be better?
		}
	}

	private void OnAudioFilterRead (float[] data, int channels)
	{
		midiStreamSynthesizer.GetNext (sampleBuffer);

		float[] tempBuffer;

		tempBuffer = new float[data.Length];
		bool bufferEmpty = true;

		for (int i = 0; i < data.Length; i++)
		{
			data[i] 		= sampleBuffer[i] * gain;
			tempBuffer[i] 	= sampleBuffer[i] * gain;
			if(sampleBuffer[i] != 0)
			{
				bufferEmpty = false;
			}
		}
		if(!bufferEmpty)
		{
			dataSequence.Add(tempBuffer);
			bufferCount += tempBuffer.Length;
		}
	}

	public void displayNextBlock()
	{
		if(dataSequence.Count == 0)
		{
			Debug.Log("Can't display block: no blocks added.");
			return;
		}
		int block = dataSequence.Count-1;
		for(int i=0; i<dataSequence[block].Length; i++)
		{
			Debug.Log("Block: "+block+"."+i+": "+dataSequence[block][i]);
		}
	}

	public void resetBlocks()
	{
		nextBlock = 0;
	}

	public void MidiNoteOnHandler (int channel, int note, int velocity)
	{
		// In theory could get data here, but that means we need to play through the song 
		// to initialize the chain. Weird.
//		if(channel==1)
//		{
		Debug.Log("MNOnH: c"+channel+" n"+note+" v"+velocity+" DS: "+dataSequence.Count);
		minChannel = Mathf.Min(minChannel, channel);
		maxChannel = Mathf.Max(maxChannel, channel);
		minNote = Mathf.Min(minNote, note);
		maxNote = Mathf.Max(maxNote, note);

		recordNote(channel,note,velocity, 0.0f);
//		}
	}
	
	public void MidiNoteOffHandler (int channel, int note)
	{
//		if(channel==1)
//		{
//			Debug.Log("MNOffH: c"+channel+" n"+note);
//		}
	}



	////////////////////////////////////////////////////////////////////////////////////////
	#region Markov Junk

	private void initializeMatrix(int channels, int fromCount, int toCount)
	{
		firstNote = new List<int>();
		lastLastNote = new List<int>();
		lastNote = new List<int>();

		markov = new List<StochasticMatrix>();
		for(int chan=0; chan<channels; chan++)
		{
			firstNote.Add(-1);
			lastLastNote.Add(-1);
			lastNote.Add(-1);

			markov.Add(new StochasticMatrix(toCount));
		}
	}

	// For now doesn't use velocity or duration, only single previous state
	private void recordNote(int channel, int note, int velocity, float duration)
	{
		List<int> noteHistory = new List<int>();
		if(lastLastNote[channel] >= 0 && lastNote[channel] >= 0)
		{
			noteHistory.Add(lastLastNote[channel]);
			noteHistory.Add(lastNote[channel]);
			markov[channel].incrementTransition(noteHistory,note,1.0f);
		}
		if(firstNote[channel] < 0)
		{
			firstNote[channel] = note;
		}
		lastLastNote[channel] = lastNote[channel];
		lastNote[channel] = note;
	}

	private int getNote(int channel, int lastNote, int lastLastNote)
	{
		float roll = Random.value;
		float sum = 0.0f;
		if(lastNote < 0 || lastLastNote < 0)
		{
			return Random.Range(57,88);
		}

		List<int> history = new List<int>();
		history.Add(lastLastNote);
		history.Add(lastNote);

		return markov[channel].getSampleNote(history);
	}

	#endregion Markov Junk
	////////////////////////////////////////////////////////////////////////////////////////
}
