MIDIM - MIDI Markov
===================

MIDIM follows in the footsteps of dozens of other projects crossing MIDI music
with Markov Chain-based MIDI output.

But this one... is in C#!

Eventually I hope to wrap this as a Unity plugin so others can generate music
on the fly, but first things first.

Currently, this makes use of UnitySynth, found here:
http://forum.unity3d.com/threads/unitysynth-full-xplatform-midi-synth.130104/

For this to work in Unity, PlayerSettings>Other Settings>Api Compatibility Level
must be set to .NET 2.0, as opposed to .NET 2.0 Subset.

MIT License

Credits:
http://forum.unity3d.com/threads/unitysynth-full-xplatform-midi-synth.130104/
I believe that UnitySynth is itself MIT License, but have so far not been able
to confirm.

And via that, further credits:
http://csharpsynthproject.codeplex.com/license

