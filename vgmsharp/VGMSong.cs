using System;
using System.IO;
using CSCore;

namespace Megadrive
{
    // Modernized VGM player: outputs PCM via ISampleSource for integration with CSCore/Asmo mixer
    public class VgmSampleSource : ISampleSource
    {
        private readonly VGMSong _song;
        private readonly int[] _intBuffer;
        private readonly float[] _floatBuffer;
        private int _bufferPos = 0;
        private int _bufferLen = 0;
        private bool _eof = false;
        public WaveFormat WaveFormat { get; }

        public VgmSampleSource(string vgmPath)
        {
            _song = new VGMSong(vgmPath, forStreaming: true);
            WaveFormat = new WaveFormat(44100, 32, 2, AudioEncoding.IeeeFloat); // stereo, 44.1kHz, float
            _intBuffer = new int[2048];
            _floatBuffer = new float[2048];
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int written = 0;
            while (written < count && !_eof)
            {
                if (_bufferPos >= _bufferLen)
                {
                    // Fill _intBuffer with new PCM data from VGM
                    _bufferLen = _song.RenderPcm(_intBuffer, _intBuffer.Length / 2); // stereo
                    _bufferPos = 0;
                    if (_bufferLen == 0)
                    {
                        _eof = true;
                        break;
                    }
                    // Convert to float
                    for (int i = 0; i < _bufferLen; i++)
                        _floatBuffer[i] = _intBuffer[i] / 32768f;
                }
                int toCopy = Math.Min(count - written, _bufferLen - _bufferPos);
                Array.Copy(_floatBuffer, _bufferPos, buffer, offset + written, toCopy);
                _bufferPos += toCopy;
                written += toCopy;
            }
            return written;
        }

        public bool CanSeek => false;
        public long Position { get => 0; set { } }
        public long Length => 0;
        public void Dispose() { _song.Dispose(); }
    }

    // --- VGMSong modernized for streaming ---
    public partial class VGMSong : IDisposable
    {
        private YM2612 _chip;
        private SN76489 _psg;
        private int[] _intBuffer;
        private BinaryReader _vgmReader;
        private VGM_HEADER _VGMHead;
        private byte[] _DACData;
        private int _DACOffset = 0;
        private int _VGMDataOffset;
        private byte _lastCommand;
        private int _wait = 0;
        public bool enableFM = true;
        public bool enablePSG = true;
        public bool gameFroze = false;

        // Add a constructor for streaming mode (no XNA)
        public VGMSong(string file, bool forStreaming)
        {
            // Use the original OpenVGMFile logic from the old constructor
            bool zipped = CheckIfZip(file, 3, "1F-8B-08");
            uint FileSize = 0;
            FileStream vgmFile = File.Open(file, FileMode.Open);
            if (zipped)
            {
                vgmFile.Position = vgmFile.Length - 4;
                byte[] b = new byte[4];
                vgmFile.Read(b, 0, 4);
                uint fileSize = BitConverter.ToUInt32(b, 0);
                FileSize = fileSize;
                vgmFile.Position = 0;
                var stream = new System.IO.Compression.GZipStream(vgmFile, System.IO.Compression.CompressionMode.Decompress);
                _vgmReader = new BinaryReader(stream);
                zipped = true;
            }
            else
            {
                FileSize = (uint)vgmFile.Length;
                _vgmReader = new BinaryReader(vgmFile);
            }
            uint fccHeader = _vgmReader.ReadUInt32();
            if (fccHeader != 0x206D6756) // 'Vgm '
                throw new InvalidDataException("Not a VGM file");
            _VGMHead = ReadVGMHeader(_vgmReader);
            if (zipped)
            {
                _vgmReader.Close();
                vgmFile = File.Open(file, FileMode.Open);
                var stream = new System.IO.Compression.GZipStream(vgmFile, System.IO.Compression.CompressionMode.Decompress);
                _vgmReader = new BinaryReader(stream);
            }
            else
                _vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);
            int offset = (int)_VGMHead.lngDataOffset;
            if (offset == 0 || offset == 0x0000000C)
                offset = 0x40;
            _VGMDataOffset = offset;
            _vgmReader.ReadBytes(offset);
            var _VGMData = _vgmReader.ReadBytes((int)(FileSize - offset));
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
            int Sound_Rate = 44100;
            int Clock_NTSC = (int)_VGMHead.lngHzYM2612;
            _chip = new YM2612();
            _psg = new SN76489();
            _chip.Initialize(Clock_NTSC, Sound_Rate);
            _psg.Initialize(_VGMHead.lngHzPSG);
            _intBuffer = new int[2048];
        }

        // Render PCM samples into a buffer (returns number of samples written, stereo)
        public int RenderPcm(int[] buffer, int samples)
        {
            int samplesWritten = 0;
            int[] bufferData = new int[2];
            bool songEnded = false;
            while (samplesWritten < samples)
            {
                Console.WriteLine($"Wait: {_wait}, LastCmd: {_lastCommand:X2}, Pos: {_vgmReader.BaseStream.Position}/{_vgmReader.BaseStream.Length}, DACOffset: {_DACOffset}");
                if (_wait == 0 && !gameFroze)
                {
                    if (_vgmReader.BaseStream.Position >= _vgmReader.BaseStream.Length)
                    {
                        songEnded = true;
                        break;
                    }
                    byte command = _vgmReader.ReadByte();
                    _lastCommand = command;
                    switch (command)
                    {
                        case 0x4F:
                            _vgmReader.ReadByte();
                            break;
                        case 0x50:
                            _psg.Write(_vgmReader.ReadByte());
                            break;
                        case 0x52:
                            _chip.WritePort0(_vgmReader.ReadByte(), _vgmReader.ReadByte());
                            break;
                        case 0x53:
                            _chip.WritePort1(_vgmReader.ReadByte(), _vgmReader.ReadByte());
                            break;
                        case 0x61:
                            _wait = _vgmReader.ReadUInt16();
                            break;
                        case 0x62:
                            _wait = 735;
                            break;
                        case 0x63:
                            _wait = 882;
                            break;
                        case 0xE0:
                            _DACOffset = (int)_vgmReader.ReadUInt32();
                            break;
                        case 0x67:
                            _vgmReader.ReadByte();
                            _vgmReader.ReadByte();
                            uint size = _vgmReader.ReadUInt32();
                            _vgmReader.BaseStream.Position += size;
                            break;
                        case 0x66:
                            // End of song
                            songEnded = true;
                            break;
                    }
                    if (command >= 0x70 && command <= 0x7F)
                        _wait = (command & 15) + 1;
                    else if (command >= 0x80 && command <= 0x8F)
                    {
                        _wait = (command & 15);
                        if (_DACData == null)
                        {
                            Console.WriteLine("DAc is null");
                        }    
                        _chip.WritePort0(0x2A, _DACData[_DACOffset]);
                        _DACOffset++;
                    }
                    if (_wait != 0)
                        _wait -= 1;
                }
                else
                {
                    if (_wait > 0)
                        _wait--;
                }
                if (songEnded)
                    break;
                // Mix chips
                _chip.Update(bufferData, 1);
                short aLeft = (short)bufferData[0];
                short aRight = (short)bufferData[1];
                if (!enableFM) aLeft = aRight = 0;
                _psg.Update(bufferData, 1);
                short bLeft = (short)bufferData[0];
                short bRight = (short)bufferData[1];
                if (!enablePSG) bLeft = bRight = 0;
                buffer[samplesWritten * 2] = Math.Min(Math.Max((aLeft + bLeft) * 2, short.MinValue), short.MaxValue);
                buffer[samplesWritten * 2 + 1] = Math.Min(Math.Max((aRight + bRight) * 2, short.MinValue), short.MaxValue);
                samplesWritten++;
            }
            return samplesWritten * 2; // stereo samples
        }

        public void Dispose() { /* Add cleanup if needed */ }

        // Add static helpers from original VGMSong
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
        static VGM_HEADER ReadVGMHeader(BinaryReader hFile)
        {
            VGM_HEADER CurHead = new VGM_HEADER();
            var fields = typeof(VGM_HEADER).GetFields();
            foreach (var field in fields)
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
                CurHead.bytPSG_SRWidth = 0x10;
                CurHead.lngHzYM2612 = CurHead.lngHzYM2413;
                CurHead.lngHzYM2151 = CurHead.lngHzYM2413;
            }
            if (CurHead.lngHzPSG != 0)
            {
                if (CurHead.shtPSG_Feedback == 0)
                    CurHead.shtPSG_Feedback = 0x0009;
                if (CurHead.bytPSG_SRWidth == 0)
                    CurHead.bytPSG_SRWidth = 0x10;
            }
            return CurHead;
        }
    }

    // Add VGM_HEADER definition (from original file)
#pragma warning disable 0649, 0169
    public class VGM_HEADER
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
