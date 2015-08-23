using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

//[RequireComponent (typeof(AudioSource))]
public class LoadMidi : MonoBehaviour {
	
	public string testMidiFile = "Midis/Video_Game_Themes_-_Secret_Of_Mana.mid";

	public string bankFilePath = "FM Bank/gm";
	public int bufferSize = 1024;

	private float[] sampleBuffer;
	private float gain = 1f;
	private MidiSequencer midiSequencer;
	private StreamSynthesizer midiStreamSynthesizer;
	
	void Awake()
	{
		// For now, imitating the behavior of UnitySynthTest.cs with two goals:
		// 1. Understand how UnitySynth loads and plays midi files
		// 2. Figure out where in that process to get the data for the markov chain

		// copied straight for UnitySynthTest.cs, almost
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
	
	void Update()
	{
		// add some play/stop keys since I'm not using the GUILayout stuff.
		if(Input.GetKeyDown(KeyCode.P))
		{
			midiSequencer.Play();
		}
		else if(Input.GetKeyDown(KeyCode.S))
		{
			midiSequencer.Stop(true);
		}
	}

	private void OnAudioFilterRead (float[] data, int channels)
	{
		midiStreamSynthesizer.GetNext (sampleBuffer);
		
		for (int i = 0; i < data.Length; i++) {
			data [i] = sampleBuffer [i] * gain;
		}
	}
	
	public void MidiNoteOnHandler (int channel, int note, int velocity)
	{

	}
	
	public void MidiNoteOffHandler (int channel, int note)
	{

	}
}
