using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using System.IO.Compression;
using System.Reflection;


//Sega Genesis VGM player. Player written and emulators ported by Landon Podbielski. 
//Emulators are written by authors mentioned in YM2612.cs and SN76489.cs
namespace Megadrive
{
	public class VGMSong
	{
		private DynamicSoundEffectInstance _instance;
		private byte[] _buffer;
		private int[] _intBuffer;
		private YM2612 _chip = new YM2612();
		private SN76489 _psg = new SN76489();

		private bool _enablePSG = true;
		public bool enablePSG { get { return _enablePSG; } set { _enablePSG = value; } }
		private bool _enableFM = true;
		public bool enableFM { get { return _enableFM; } set { _enableFM = value; } }

		public SoundState state { get { return _iSaidStop ? SoundState.Stopped : _instance.State; } }
		private bool _iSaidStop = false;
		private float _volume = 1.0f;
		public float volume
		{
			get { return _volume; }
			set
			{
				_volume = MathHelper.Clamp(value, 0, 1);
				if (_instance != null && _instance.State == SoundState.Playing)
					_instance.Volume = _volume;
			}
		}
		private bool _looped = true;
		public bool looped { get { return _looped; } set { _looped = value; } }
		public bool gameFroze { get; set; }

		private float _playbackSpeed = 1.0f;
		public float playbackSpeed { get { return _playbackSpeed; } set { _playbackSpeed = Math.Min(Math.Max(value, 0), 4); } }

		public VGMSong(string file)
		{
			_instance = new DynamicSoundEffectInstance(44100, (AudioChannels)2);
			_buffer = new byte[_instance.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(150))];
			_intBuffer = new int[_buffer.Length / 2];
			_instance.BufferNeeded += StreamVGM;
			
			OpenVGMFile(file);

			int Sound_Rate = 44100;
			int Clock_NTSC = (int)_VGMHead.lngHzYM2612;
			_chip.Initialize(Clock_NTSC, Sound_Rate);
			_psg.Initialize(_VGMHead.lngHzPSG);
		}

		public void Terminate()
		{
			_instance.Dispose();
		}

		public void Play()
		{
			_instance.Stop();
			_instance.Play();
			_instance.Volume = _volume;
			_iSaidStop = false;
		}

		public void Pause()
		{
			_instance.Pause();
			//_iSaidStop = true;
		}

		public void Resume()
		{
			_instance.Resume();
			_instance.Volume = _volume;
			_iSaidStop = false;
		}

		public void Stop()
		{
			_instance.Stop();
			_instance.Volume = 0.0f;
			_iSaidStop = true;
			_vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);
		}

		
		const uint FCC_VGM	= 0x206D6756;	// 'Vgm '
		uint _VGMDataLen;
		VGM_HEADER _VGMHead;
		static VGM_HEADER ReadVGMHeader(BinaryReader hFile)
		{
			VGM_HEADER CurHead = new VGM_HEADER();
			FieldInfo[] fields = typeof(VGM_HEADER).GetFields();
			foreach (FieldInfo field in fields)
			{
				if (field.FieldType == typeof(uint))
				{
					uint val = hFile.ReadUInt32();
					field.SetValue(CurHead, val);
				}
				else if (field.FieldType == typeof(ushort))
				{
					ushort val = hFile.ReadUInt16();
					field.SetValue(CurHead, val);
				}
				else if (field.FieldType == typeof(char))
				{
					char val = hFile.ReadChar();
					field.SetValue(CurHead, val);
				}
				else if (field.FieldType == typeof(byte))
				{
					byte val = hFile.ReadByte();
					field.SetValue(CurHead, val);
				}	
			}

			// Header preperations
			if (CurHead.lngVersion < 0x00000101)
			{
				CurHead.lngRate = 0;
			}
			if (CurHead.lngVersion < 0x00000110)
			{
				CurHead.shtPSG_Feedback = 0x0000;
				CurHead.bytPSG_SRWidth = 0x00;
				CurHead.lngHzYM2612 = CurHead.lngHzYM2413;
				CurHead.lngHzYM2151 = CurHead.lngHzYM2413;
			}

			if (CurHead.lngHzPSG != 0)
			{
				if ( CurHead.shtPSG_Feedback == 0)
					CurHead.shtPSG_Feedback = 0x0009;
				if ( CurHead.bytPSG_SRWidth == 0)
					CurHead.bytPSG_SRWidth = 0x10;
			}
			return CurHead;
		}

		public static bool CheckIfZip(string filepath, int signatureSize, string expectedSignature)
		{
			using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				if (fs.Length < signatureSize)
					return false;
				byte[] signature = new byte[signatureSize];
				int bytesRequired = signatureSize;
				int index = 0;
				while (bytesRequired > 0)
				{
					int bytesRead = fs.Read(signature, index, bytesRequired);
					bytesRequired -= bytesRead;
					index += bytesRead;
				}
				string actualSignature = BitConverter.ToString(signature);
				if (actualSignature == expectedSignature)
					return true;
				else
					return false;
			}
		}

		private BinaryReader _vgmReader;
		private byte[] _DACData;
		private byte[] _VGMData;
		private int _DACOffset = 0;
		private int _VGMDataOffset;
		private byte _lastCommand;
		bool OpenVGMFile(string fileName)
		{
			bool zipped = CheckIfZip(fileName, 3, "1F-8B-08");

			//Read size
			uint FileSize = 0;
			FileStream vgmFile = File.Open(fileName, FileMode.Open);
			if (zipped)
			{
				vgmFile.Position = vgmFile.Length - 4;
				byte[] b = new byte[4];
				vgmFile.Read(b, 0, 4);
				uint fileSize = BitConverter.ToUInt32(b, 0);
				FileSize = fileSize;
				vgmFile.Position = 0;

				GZipStream stream = new GZipStream(vgmFile, CompressionMode.Decompress);
				_vgmReader = new BinaryReader(stream);
				zipped = true;
			}
			else
			{
				FileSize = (uint)vgmFile.Length;
				_vgmReader = new BinaryReader(vgmFile);
			}

			uint fccHeader;
			fccHeader = (uint)_vgmReader.ReadUInt32();
			if (fccHeader != FCC_VGM)
				return false;

			_VGMDataLen = FileSize;
			_VGMHead = ReadVGMHeader(_vgmReader);

			if (zipped)
			{
				_vgmReader.Close();
				vgmFile = File.Open(fileName, FileMode.Open);
				GZipStream stream = new GZipStream(vgmFile, CompressionMode.Decompress);
				_vgmReader = new BinaryReader(stream);
			}
			else
				_vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);

			//Figure out header offset
			int offset = (int)_VGMHead.lngDataOffset;
			if (offset == 0 || offset == 0x0000000C)
				offset = 0x40;
			_VGMDataOffset = offset;

			_vgmReader.ReadBytes(offset);
			_VGMData = _vgmReader.ReadBytes((int)(FileSize - offset));
			_vgmReader = new BinaryReader(new MemoryStream(_VGMData));

			if ((byte)_vgmReader.PeekChar() == 0x67)
			{
				_vgmReader.ReadByte();
				if ((byte)_vgmReader.PeekChar() == 0x66)
				{
					_vgmReader.ReadByte();
					byte type = _vgmReader.ReadByte();
					uint size = _vgmReader.ReadUInt32();
					_DACData = _vgmReader.ReadBytes((int)size);
				}
			}

			vgmFile.Close();
			return true;
		}

		private int _wait = 0;
		private float _waitInc = 0.0f;
		private void StreamVGM(object sender, EventArgs e)
		{
			if (_iSaidStop)
				return;

			if (_lastCommand == 0x66 && _looped == false)
			{
				_lastCommand = 0;
				_instance.Volume = 0.0f;
				_iSaidStop = true;
				Stop();
				return;
			}

			int[] bufferData = new int[2];
			bool writeSample = false;
			int samplesWritten = 0;
			int samplesToWrite = _intBuffer.Length / 2;
			bool songEnded = false;
			while (samplesWritten != samplesToWrite)
			{
				
				if (_wait == 0 && !gameFroze)
				{
					writeSample = false;
					byte command = _vgmReader.ReadByte();
					_lastCommand = command;
					switch (command)
					{
						case 0x4F:
							{
								byte aa = _vgmReader.ReadByte();
							}
							break;

						case 0x50:
							{
								byte aa = _vgmReader.ReadByte();
								_psg.Write(aa);
							}
							break;

						case 0x52: //YM2612 Write Port 0
							{
								byte aa = _vgmReader.ReadByte();
								byte dd = _vgmReader.ReadByte();
								_chip.WritePort0(aa, dd);
							}
							break;

						case 0x53: //YM2612 Write Port 1
							{
								byte aa = _vgmReader.ReadByte();
								byte dd = _vgmReader.ReadByte();
								_chip.WritePort1(aa, dd);
							}
							break;

						case 0x61: //Wait N samples
							{
								ushort time = _vgmReader.ReadUInt16();
								 _wait = time;

								if (_wait != 0)
									writeSample = true;
							}
							break;

						case 0x62: //Wait 735 samples
								_wait = 735;
								writeSample = true;
							break;

						case 0x63: //Wait 882 samples
								_wait = 882;
								writeSample = true;
							break;

						case 0xE0: //Seek to offset in PCM databank
							uint offset = _vgmReader.ReadUInt32();
							_DACOffset = (int)offset;
							break;

						case 0x67: //Skip VGM Data
							{
								_vgmReader.ReadByte();
								byte type = _vgmReader.ReadByte();
								uint size = _vgmReader.ReadUInt32();
								_vgmReader.BaseStream.Position += size;
							}
							break;
						case 0x66:
							//End of song
							if (_looped == false)
							{
								_vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);
								songEnded = true;
								break;
							}
							else
							{
								if (_VGMHead.lngLoopOffset != 0 && _VGMDataOffset < _VGMHead.lngLoopOffset)
									_vgmReader.BaseStream.Seek((_VGMHead.lngLoopOffset - (_VGMDataOffset)) /*+ 0x1C*/, SeekOrigin.Begin);
								else
									_vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);
							}
							break;
					}
					if (command >= 0x70 && command <= 0x7F)
					{
						_wait = (command & 15) + 1;
						if (_wait != 0)
							writeSample = true;
					}
					else if (command >= 0x80 && command <= 0x8F)
					{
						_wait = (command & 15);
						_chip.WritePort0(0x2A, _DACData[_DACOffset]);
						_DACOffset++;
						if(_wait != 0)
							writeSample = true;
					}

					if (_wait != 0)
						_wait -= 1;
				}
				else
				{
					writeSample = true;

					if (_wait > 0)
					{
						_waitInc += _playbackSpeed;
						while (_wait > 0 && _waitInc >= 1.0f)
						{
							_waitInc -= 1.0f;
							_wait--;
						}
					}
				}

				if (songEnded)
					break;

				if (writeSample)
				{
					_chip.Update(bufferData, 1);

					short aLeft = (short)bufferData[0];
					short aRight = (short)bufferData[1];
					if (enableFM == false)
						aLeft = aRight = 0;

					_psg.Update(bufferData, 1);
					short bLeft = (short)bufferData[0];
					short bRight = (short)bufferData[1];
					if (enablePSG == false)
						bLeft = bRight = 0;

					//shitty mixing
					_intBuffer[(samplesWritten * 2)] = Math.Min(Math.Max((aLeft + bLeft) * 2, short.MinValue), short.MaxValue);
					_intBuffer[(samplesWritten * 2) + 1] = Math.Min(Math.Max((aRight + bRight) * 2, short.MinValue), short.MaxValue);

					samplesWritten += 1;
					if (samplesWritten == samplesToWrite)
						break;
				}
			}

			for (int i = 0; i < _intBuffer.Length; i++)
			{
				short sValue = (short)_intBuffer[i];
				_buffer[i * 2] = (byte)(sValue & 0xff);
				_buffer[i * 2 + 1] = (byte)((sValue >> 8) & 0xff);
			}

			samplesWritten *= 2;
			if (((float)samplesWritten / 4.0f) - (int)((float)samplesWritten / 4.0f) > 0)
				samplesWritten -= 2;

			_instance.SubmitBuffer(_buffer, 0, samplesWritten);
			_instance.SubmitBuffer(_buffer, samplesWritten, samplesWritten);
		}
	}

#pragma warning disable 0649, 0169
	class VGM_HEADER
	{
		public uint lngEOFOffset;
		public uint lngVersion;
		public uint lngHzPSG;
		public uint lngHzYM2413;
		public uint lngGD3Offset;
		public uint lngTotalSamples;
		public uint lngLoopOffset;
		public uint lngLoopSamples;
		public uint lngRate;
		public ushort shtPSG_Feedback;
		public byte bytPSG_SRWidth;
		public byte bytPSG_Flags;
		public uint lngHzYM2612;
		public uint lngHzYM2151;
		public uint lngDataOffset;
		public uint lngHzSPCM;
		public uint lngSPCMIntf;
		public uint lngHzRF5C68;
		public uint lngHzYM2203;
		public uint lngHzYM2608;
		public uint lngHzYM2610;
		public uint lngHzYM3812;
		public uint lngHzYM3526;
		public uint lngHzY8950;
		public uint lngHzYMF262;
		public uint lngHzYMF278B;
		public uint lngHzYMF271;
		public uint lngHzYMZ280B;
		public uint lngHzRF5C164;
		public uint lngHzPWM;
		public uint lngHzAY8910;
		public byte bytAYType;
		public byte bytAYFlag;
		public byte bytAYFlagYM2203;
		public byte bytAYFlagYM2608;
		public byte bytVolumeModifier;
		public byte bytReserved2;
		public char bytLoopBase;
		public byte bytLoopModifier;
		public uint lngHzGBDMG;
		public uint lngHzNESAPU;
		public uint lngHzMultiPCM;
		public uint lngHzUPD7759;
		public uint lngHzOKIM6258;
		public byte bytOKI6258Flags;
		public byte bytK054539Flags;
		public byte bytC140Type;
		public byte bytReservedFlags;
		public uint lngHzOKIM6295;
		public uint lngHzK051649;
		public uint lngHzK054539;
		public uint lngHzHuC6280;
		public uint lngHzC140;
		public uint lngHzK053260;
		public uint lngHzPokey;
		public uint lngHzQSound;
		public uint lngHzSCSP;
		public uint lngExtraOffset;
		public uint lngHzWSwan;
	}
#pragma warning restore 0649, 0169

}
