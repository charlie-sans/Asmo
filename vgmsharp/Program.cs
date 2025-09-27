using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Megadrive;
//using Microsoft.Xna;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Input;
//using Microsoft.Xna.Framework.Audio;
using System.Threading;
using System.Globalization;

namespace VGMPlayer
{
	class Program
	{
		private static VGMSong _song;
		//static void Main(string[] args)
		//{
		//	FrameworkDispatcher.Update();
		//	_song = new VGMSong("whirlwind.vgz");
		//	_song.Play();
		//	UpdateConsole();

		//	Thread update = new Thread(Update);
		//	update.CurrentCulture = CultureInfo.InvariantCulture;
		//	update.Priority = ThreadPriority.Normal;
		//	update.Start();

		//	ConsoleKeyInfo cki;
		//	while (true)
		//	{
		//		cki = Console.ReadKey();
		//		if (cki.Key == ConsoleKey.Z)
		//		{
		//			if (_song.state == SoundState.Paused)
		//				_song.Resume();
		//			else if (_song.state == SoundState.Playing)
		//				_song.Pause();
		//			else
		//				_song.Play();

		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.X)
		//		{
		//			_song.Stop();
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.C)
		//		{
		//			_song.playbackSpeed -= 0.1f;
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.V)
		//		{
		//			_song.playbackSpeed += 0.1f;
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.B)
		//		{
		//			_song.looped = !_song.looped;
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.N)
		//		{
		//			_song.gameFroze = !_song.gameFroze;
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.A)
		//		{
		//			_song.enableFM = !_song.enableFM;
		//			UpdateConsole();
		//		}
		//		else if (cki.Key == ConsoleKey.S)
		//		{
		//			_song.enablePSG = !_song.enablePSG;
		//			UpdateConsole();
		//		}
		//	}
		//}

		//private static void Update()
		//{
		//	while(true)
		//		FrameworkDispatcher.Update();
		//}

		//private static void UpdateConsole()
		//{
		//	Console.Clear();
		//	Console.WriteLine("C# VGM Player");
		//	Console.WriteLine("--------------------------------------------");
		//	Console.WriteLine("Z - Play/Pause   X - Stop");
		//	Console.WriteLine("C - Tempo Down   V - Tempo Up");
		//	Console.WriteLine("A - Toggle FM    S - Toggle PSG");
		//	Console.WriteLine("N - Freeze Game");
		//	Console.WriteLine("--------------------------------------------");
		//	Console.WriteLine("Current state: " + (_song.state == SoundState.Playing ? "Playing" : (_song.state == SoundState.Stopped ? "Stopped" : "Paused")));
		//	Console.WriteLine("Play Speed: " + _song.playbackSpeed.ToString("0.0", CultureInfo.InvariantCulture));
		//	Console.WriteLine("Looping: " + (_song.looped ? "Yes" : "No"));
		//	Console.WriteLine("Frozen: " + (_song.gameFroze ? "Yes" : "No"));
		//	Console.WriteLine("FM: " + (_song.enableFM ? "Enabled" : "Disabled") + "    PSG: " + (_song.enablePSG ? "Enabled" : "Disabled"));
		//}
	}
}
