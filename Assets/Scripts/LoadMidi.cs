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
	
	private static int frameCount = 0;
	private static int bufferCount = 0;
	private static int nextBlock = 0;

	int getOneCount = 0;
	List<float[]> dataSequence = new List<float[]>();

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

	}

	void Start()
	{

	}

	// Keys:
	//  P/S: 					Play/Stop
	//  ` (tilde/backquote):	Mute/Unmute all channels
	//							If any channel is not muted, this will mute all. Else it unmutes all.
	//  1-9,0:					Toggle mute for channel <key>
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
			displayNextBlock();
		}
		if(Input.GetKeyDown(KeyCode.R))
		{
			displayNextBlock();
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

		for(int channel=0; channel<10; channel++)
		{
			checkSingleMuteKey(channel);
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
//			Debug.Log("DS: "+dataSequence.Count+", count: "+bufferCount+" frame: "+frameCount);
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
		if(channel==1)
		{
			Debug.Log("MNOnH: c"+channel+" n"+note+" v"+velocity+" DS: "+dataSequence.Count);
		}
	}
	
	public void MidiNoteOffHandler (int channel, int note)
	{
		if(channel==1)
		{
			Debug.Log("MNOffH: c"+channel+" n"+note);
		}
	}
}
