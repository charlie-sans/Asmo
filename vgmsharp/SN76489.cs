using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

//Code is ported from Shiru's AS3 VGM player http://shiru.untergrund.net/
namespace Megadrive
{
	public class SN76489
	{
		private SN76489Core _chip;
		public SN76489()
		{
			_chip = new SN76489Core();
		}
		public void Initialize(double clock)
		{
			_chip.clock((float)clock);
		}
		public void Update(int[] buffer, int length)
		{
			//Temporary shitty amp
			for (int i = 0; i < length; i+=1)
			{
				short val = (short)(_chip.render() * 8000);
				buffer[i * 2] = val;
				buffer[(i * 2) + 1] = val;
			}
		}
		public void Write(int value)
		{
			_chip.write(value);
		}
	}

	public sealed class SN76489Core
	{
		private static float[] volumeTable = new float[] {
				0.25f,0.2442f,0.1940f,0.1541f,0.1224f,0.0972f,0.0772f,0.0613f,0.0487f,0.0386f,0.0307f,0.0244f,0.0193f,0.0154f,0.0122f,0,
				-0.25f,-0.2442f,-0.1940f,-0.1541f,-0.1224f,-0.0972f,-0.0772f,-0.0613f,-0.0487f,-0.0386f,-0.0307f,-0.0244f,-0.0193f,-0.0154f,-0.0122f,0,
				0.25f,0.2442f,0.1940f,0.1541f,0.1224f,0.0972f,0.0772f,0.0613f,0.0487f,0.0386f,0.0307f,0.0244f,0.0193f,0.0154f,0.0122f,0,
				0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
		};

		private uint volA;
		private uint volB;
		private uint volC;
		private uint volD;
		private int divA;
		private int divB;
		private int divC;
		private int divD;
		private int cntA;
		private int cntB;
		private int cntC;
		private int cntD;
		private float outA;
		private float outB;
		private float outC;
		private float outD;
		private uint noiseLFSR;
		private uint noiseTap;
		private uint latchedChan;
		private bool latchedVolume;

		private float ticksPerSample;
		private float ticksCount;

		public SN76489Core()
		{
			clock(3500000);
			reset();
		}

		public void clock(float f)
		{
			ticksPerSample = f / 16 / 44100;
		}

		public void reset()
		{
			volA = 15;
			volB = 15;
			volC = 15;
			volD = 15;
			outA = 0;
			outB = 0;
			outC = 0;
			outD = 0;
			latchedChan = 0;
			latchedVolume = false;
			noiseLFSR = 0x8000;
			ticksCount = ticksPerSample;
		}

		public uint getDivByNumber(uint chan)
		{
			switch (chan)
			{
				case 0: return (uint)divA;
				case 1: return (uint)divB;
				case 2: return (uint)divC;
				case 3: return (uint)divD;
			}
			return 0;
		}

		public void setDivByNumber(uint chan, uint div)
		{
			switch (chan)
			{
				case 0: divA = (int)div; break;
				case 1: divB = (int)div; break;
				case 2: divC = (int)div; break;
				case 3: divD = (int)div; break;
			}
		}

		public uint getVolByNumber(uint chan)
		{
			switch (chan)
			{
				case 0: return volA;
				case 1: return volB;
				case 2: return volC;
				case 3: return volD;
			}
			return 0;
		}

		public void setVolByNumber(uint chan, uint vol)
		{
			switch (chan)
			{
				case 0: volA = vol; break;
				case 1: volB = vol; break;
				case 2: volC = vol; break;
				case 3: volD = vol; break;
			}
		}

		public void write(int val)
		{
			int chan;
			int cdiv;
			if ((val & 128) != 0)
			{
				chan = (val >> 5) & 3;
				cdiv = (int)((getDivByNumber((uint)chan) & 0xfff0) | ((uint)val & 15));

				latchedChan = (uint)chan;
				latchedVolume = ((val & 16) != 0) ? true : false;
			}
			else
			{
				chan = (int)latchedChan;
				cdiv = (int)((getDivByNumber((uint)chan) & 15) | (((uint)val & 63) << 4));
			}

			if (latchedVolume)
			{
				setVolByNumber((uint)chan, (uint)((getVolByNumber((uint)chan) & 16) | ((uint)val & 15)));
			}
			else
			{
				setDivByNumber((uint)chan, (uint)cdiv);
				if (chan == 3)
				{
					noiseTap = (uint)((((cdiv >> 2) & 1) != 0) ? 9 : 1);
					noiseLFSR = 0x8000;
				}
			}
		}

		public float render()
		{
			uint cdiv, tap;
			float outVal;

			while (ticksCount > 0)
			{
				cntA -= 1;
				if (cntA < 0)
				{
					if (divA > 1)
					{
						volA ^= 16;
						outA = volumeTable[volA];
					}
					cntA = divA;
				}

				cntB -= 1;
				if (cntB < 0)
				{
					if (divB > 1)
					{
						volB ^= 16;
						outB = volumeTable[volB];
					}
					cntB = divB;
				}

				cntC -= 1;
				if (cntC < 0)
				{
					if (divC > 1)
					{
						volC ^= 16;
						outC = volumeTable[volC];
					}
					cntC = divC;
				}

				cntD -= 1;
				if (cntD < 0)
				{
					cdiv = (uint)(divD & 3);
					if (cdiv < 3) cntD = (int)(0x10 << (int)cdiv); else cntD = divC << 1;

					if (noiseTap == 9)
					{
						tap = noiseLFSR & noiseTap;
						tap ^= tap >> 8;
						tap ^= tap >> 4;
						tap ^= tap >> 2;
						tap ^= tap >> 1;
						tap &= 1;
					}
					else
					{
						tap = noiseLFSR & 1;
					}

					noiseLFSR = (noiseLFSR >> 1) | (tap << 15);
					volD = (volD & 15) | ((noiseLFSR & 1 ^ 1) << 4);
					outD = volumeTable[volD];
				}

				ticksCount -= 1;
			}

			ticksCount += ticksPerSample;
			outVal = outA + outB + outC + outD;

			return outVal;
		}
	}
}