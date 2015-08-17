MIDIM - MIDI Markov
===================

MIDIM follows in the footsteps of dozens of other projects crossing MIDI music
with Markov Chain-based MIDI output.

But this one... is in C#!

Eventually I hope to wrap this as a Unity plugin so others can generate music
on the fly, but first things first.

Currently, this makes use of the [tebjan/Sanford.Multimedia.Midi] project.

For this to work in Unity, PlayerSettings>Other Settings>Api Compatibility Level
must be set to .NET 2.0, as opposed to .NET 2.0 Subset.

I would also recommend excluding the Sanford.Multimedia.Midi.UI folder entirely
when you import to Unity.

MIT License

Credits:
https://github.com/tebjan/Sanford.Multimedia.Midi

And via that, further credits:
http://www.codeproject.com/Articles/6228/C-MIDI-Toolkit
https://code.google.com/p/vsticks/
