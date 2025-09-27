using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Megadrive
{
	//YM2612 MAME implementation, Thrown drunkenly into a class and converted to C# by Landon Podbielski.
	//All code work is by the fellows below.
	/*
	** software implementation of Yamaha FM sound generator (YM2612/YM3438)
	**
	** Original code (MAME fm.c)
	**
	** Copyright (C) 2001, 2002, 2003 Jarek Burczynski (bujar at mame dot net)
	** Copyright (C) 1998 Tatsuyuki Satoh , MultiArcadeMachineEmulator development
	**
	** Version 1.4 (final beta) 
	**
	** Additional code & fixes by Eke-Eke for Genesis Plus GX
	**
	** Huge thanks to Nemesis, most of those fixes came from his tests on Sega Genesis hardware
	** More informations at http://gendev.spritesmind.net/forum/viewtopic.php?t=386
	**
	**  TODO:
	**  - better documentation
	**  - BUSY flag emulation
	*/

	/*
	**  CHANGELOG:
	**
	** 2006~2011  Eke-Eke (Genesis Plus GX):
	**  - removed unused multichip support
	**  - added YM2612 Context external access functions
	**  - fixed LFO implementation:
	**      .added support for CH3 special mode: fixes various sound effects (birds in Warlock, bug sound in Aladdin...)
	**      .modified LFO behavior when switched off (AM/PM current level is held) and on (LFO step is reseted): fixes intro in Spider-Man & Venom : Separation Anxiety
	**      .improved LFO timing accuracy: now updated AFTER sample output, like EG/PG updates, and without any precision loss anymore.
	**  - improved internal timers emulation
	**  - adjusted lowest EG rates increment values
	**  - fixed Attack Rate not being updated in some specific cases (Batman & Robin intro)
	**  - fixed EG behavior when Attack Rate is maximal
	**  - fixed EG behavior when SL=0 (Mega Turrican tracks 03,09...) or/and Key ON occurs at minimal attenuation 
	**  - implemented EG output immediate changes on register writes
	**  - fixed YM2612 initial values (after the reset): fixes missing intro in B.O.B
	**  - implemented Detune overflow (Ariel, Comix Zone, Shaq Fu, Spiderman & many other games using GEMS sound engine)
	**  - implemented accurate CSM mode emulation
	**  - implemented accurate SSG-EG emulation (Asterix, Beavis&Butthead, Bubba'n Stix & many other games)
	**  - implemented accurate address/data ports behavior
	**  - added preliminar support for DAC precision
	**
	**
	** 03-08-2003 Jarek Burczynski:
	**  - fixed YM2608 initial values (after the reset)
	**  - fixed flag and irqmask handling (YM2608)
	**  - fixed BUFRDY flag handling (YM2608)
	**
	** 14-06-2003 Jarek Burczynski:
	**  - implemented all of the YM2608 status register flags
	**  - implemented support for external memory read/write via YM2608
	**  - implemented support for deltat memory limit register in YM2608 emulation
	**
	** 22-05-2003 Jarek Burczynski:
	**  - fixed LFO PM calculations (copy&paste bugfix)
	**
	** 08-05-2003 Jarek Burczynski:
	**  - fixed SSG support
	**
	** 22-04-2003 Jarek Burczynski:
	**  - implemented 100% correct LFO generator (verified on real YM2610 and YM2608)
	**
	** 15-04-2003 Jarek Burczynski:
	**  - added support for YM2608's register 0x110 - status mask
	**
	** 01-12-2002 Jarek Burczynski:
	**  - fixed register addressing in YM2608, YM2610, YM2610B chips. (verified on real YM2608)
	**    The addressing patch used for early Neo-Geo games can be removed now.
	**
	** 26-11-2002 Jarek Burczynski, Nicola Salmoria:
	**  - recreated YM2608 ADPCM ROM using data from real YM2608's output which leads to:
	**  - added emulation of YM2608 drums.
	**  - output of YM2608 is two times lower now - same as YM2610 (verified on real YM2608)
	**
	** 16-08-2002 Jarek Burczynski:
	**  - binary exact Envelope Generator (verified on real YM2203);
	**    identical to YM2151
	**  - corrected 'off by one' error in feedback calculations (when feedback is off)
	**  - corrected connection (algorithm) calculation (verified on real YM2203 and YM2610)
	**
	** 18-12-2001 Jarek Burczynski:
	**  - added SSG-EG support (verified on real YM2203)
	**
	** 12-08-2001 Jarek Burczynski:
	**  - corrected sin_tab and tl_tab data (verified on real chip)
	**  - corrected feedback calculations (verified on real chip)
	**  - corrected phase generator calculations (verified on real chip)
	**  - corrected envelope generator calculations (verified on real chip)
	**  - corrected FM volume level (YM2610 and YM2610B).
	**  - changed YMxxxUpdateOne() functions (YM2203, YM2608, YM2610, YM2610B, YM2612) :
	**    this was needed to calculate YM2610 FM channels output correctly.
	**    (Each FM channel is calculated as in other chips, but the output of the channel
	**    gets shifted right by one *before* sending to accumulator. That was impossible to do
	**    with previous implementation).
	**
	** 23-07-2001 Jarek Burczynski, Nicola Salmoria:
	**  - corrected YM2610 ADPCM type A algorithm and tables (verified on real chip)
	**
	** 11-06-2001 Jarek Burczynski:
	**  - corrected end of sample bug in ADPCMA_calc_cha().
	**    Real YM2610 checks for equality between current and end addresses (only 20 LSB bits).
	**
	** 08-12-98 hiro-shi:
	** rename ADPCMA -> ADPCMB, ADPCMB -> ADPCMA
	** move ROM limit check.(CALC_CH? -> 2610Write1/2)
	** test program (ADPCMB_TEST)
	** move ADPCM A/B end check.
	** ADPCMB repeat flag(no check)
	** change ADPCM volume rate (8->16) (32->48).
	**
	** 09-12-98 hiro-shi:
	** change ADPCM volume. (8->16, 48->64)
	** replace ym2610 ch0/3 (YM-2610B)
	** change ADPCM_SHIFT (10->8) missing bank change 0x4000-0xffff.
	** add ADPCM_SHIFT_MASK
	** change ADPCMA_DECODE_MIN/MAX.
	*/

	/************************************************************************/
	/*    comment of hiro-shi(Hiromitsu Shioya)                             */
	/*    YM2610(B) = OPN-B                                                 */
	/*    YM2610  : PSG:3ch FM:4ch ADPCM(18.5KHz):6ch DeltaT ADPCM:1ch      */
	/*    YM2610B : PSG:3ch FM:6ch ADPCM(18.5KHz):6ch DeltaT ADPCM:1ch      */
	/************************************************************************/

	class YM2612
	{
		private YM2612Core _chip;
		public YM2612()
		{
			_chip = new YM2612Core();
		}
		public void Initialize(double clock, int soundRate)
		{
			_chip.YM2612Init(clock, soundRate);
			_chip.YM2612ResetChip();
		}
		public void Update(int[] buffer, int length)
		{
			_chip.YM2612Update(buffer, length);
		}
		public void Write(int address, int value)
		{
			_chip.YM2612Write((uint)address, (uint)value);
		}
		public void WritePort0(int register, int value)
		{
			_chip.YM2612Write((uint)0, (uint)register);
			_chip.YM2612Write((uint)1, (uint)value);
		}
		public void WritePort1(int register, int value)
		{
			_chip.YM2612Write((uint)2, (uint)register);
			_chip.YM2612Write((uint)3, (uint)value);
		}
	}

	class LongPointer
	{
		public long value;
	}

	class LongPointerArray32
	{
		public long[] value = new long[32];
	}


	class t_config
	{
		public char[] version = new char[16];
		public byte hq_fm;
		public byte filter;
		public byte psgBoostNoise;
		public byte dac_bits;
		public byte ym2413;
		public short psg_preamp;
		public short fm_preamp;
		public short lp_range;
		public short low_freq;
		public short high_freq;
		public short lg;
		public short mg;
		public short hg;
		public float rolloff;
		public byte system;
		public byte region_detect;
		public byte master_clock;
		public byte vdp_mode;
		public byte force_dtack;
		public byte addr_error;
		public byte tmss = 0;
		public byte bios;
		public byte lock_on;
		public byte hot_swap;
		public byte invert_mouse;
		public byte[] gun_cursor = new byte[2];
		public byte overscan;
		public byte ntsc;
		public byte vsync;
		public byte render;
		public byte tv_mode;
		public byte bilinear;
		public byte aspect;
		public short xshift;
		public short yshift;
		public short xscale;
		public short yscale;
		public byte autoload;
		public byte autocheat;
		public byte s_auto;
		public byte s_default;
		public byte s_device;
		public byte l_device;
		public byte bg_overlay;
		public short screen_w;
		public float bgm_volume;
		public float sfx_volume;
	};

	class YM2612Core
	{
		/* globals */
		const int FREQ_SH = 16;    /* 16.16 fixed point (frequency calculations) */
		const int EG_SH = 16;    /* 16.16 fixed point (envelope generator timing) */
		const int LFO_SH = 24;    /*  8.24 fixed point (LFO calculations)       */
		const int TIMER_SH = 16;    /* 16.16 fixed point (timers calculations)    */

		const int FREQ_MASK = ((1 << FREQ_SH) - 1);


		/* envelope generator */
		const int ENV_BITS = 10;
		const int ENV_LEN = (1 << ENV_BITS);
		const float ENV_STEP_GX = (128.0f / ENV_LEN);

		const int MAX_ATT_INDEX = (ENV_LEN - 1); /* 1023 */
		const int MIN_ATT_INDEX = (0);      /* 0 */

		const int EG_ATT = 4;
		const int EG_DEC = 3;
		const int EG_SUS = 2;
		const int EG_REL = 1;
		const int EG_OFF = 0;

		/* operator unit */
		const int SIN_BITS = 10;
		const int SIN_LEN = (1 << SIN_BITS);
		const int SIN_MASK_GX = (SIN_LEN - 1);
		const int TL_RES_LEN = (256); /* 8 bits addressing (real chip) */

		/*  TL_TAB_LEN is calculated as:
		*   13 - sinus amplitude bits     (Y axis)
		*   2  - sinus sign bit           (Y axis)
		*   TL_RES_LEN - sinus resolution (X axis)
		*/

		/* sustain level table (3dB per step) */
		/* bit0, bit1, bit2, bit3, bit4, bit5, bit6 */
		/* 1,    2,    4,    8,    16,   32,   64   (value)*/
		/* 0.75, 1.5,  3,    6,    12,   24,   48   (dB)*/

		/* 0 - 15: 0, 3, 6, 9,12,15,18,21,24,27,30,33,36,39,42,93 (dB)*/
		/* attenuation value (10 bits) = (SL << 2) << 3 */
		private static uint SC(int db) { return (uint)(db * (4.0f / ENV_STEP_GX)); }
		static uint[] sl_table = new uint[16] {
		 SC( 0),SC( 1),SC( 2),SC(3 ),SC(4 ),SC(5 ),SC(6 ),SC( 7),
		 SC( 8),SC( 9),SC(10),SC(11),SC(12),SC(13),SC(14),SC(31)
		};

		const int RATE_STEPS = (8);
		static byte[] eg_inc = new byte[19 * RATE_STEPS]
		{
			/*cycle:0 1  2 3  4 5  6 7*/

			/* 0 */ 0,1, 0,1, 0,1, 0,1, /* rates 00..11 0 (increment by 0 or 1) */
			/* 1 */ 0,1, 0,1, 1,1, 0,1, /* rates 00..11 1 */
			/* 2 */ 0,1, 1,1, 0,1, 1,1, /* rates 00..11 2 */
			/* 3 */ 0,1, 1,1, 1,1, 1,1, /* rates 00..11 3 */

			/* 4 */ 1,1, 1,1, 1,1, 1,1, /* rate 12 0 (increment by 1) */
			/* 5 */ 1,1, 1,2, 1,1, 1,2, /* rate 12 1 */
			/* 6 */ 1,2, 1,2, 1,2, 1,2, /* rate 12 2 */
			/* 7 */ 1,2, 2,2, 1,2, 2,2, /* rate 12 3 */

			/* 8 */ 2,2, 2,2, 2,2, 2,2, /* rate 13 0 (increment by 2) */
			/* 9 */ 2,2, 2,4, 2,2, 2,4, /* rate 13 1 */
			/*10 */ 2,4, 2,4, 2,4, 2,4, /* rate 13 2 */
			/*11 */ 2,4, 4,4, 2,4, 4,4, /* rate 13 3 */

			/*12 */ 4,4, 4,4, 4,4, 4,4, /* rate 14 0 (increment by 4) */
			/*13 */ 4,4, 4,8, 4,4, 4,8, /* rate 14 1 */
			/*14 */ 4,8, 4,8, 4,8, 4,8, /* rate 14 2 */
			/*15 */ 4,8, 8,8, 4,8, 8,8, /* rate 14 3 */

			/*16 */ 8,8, 8,8, 8,8, 8,8, /* rates 15 0, 15 1, 15 2, 15 3 (increment by 8) */
			/*17 */ 16,16,16,16,16,16,16,16, /* rates 15 2, 15 3 for attack */
			/*18 */ 0,0, 0,0, 0,0, 0,0, /* infinity rates for attack and decay(s) */
		};

		private static byte eg_rate_selectO(byte a) { return (byte)(a * RATE_STEPS); }

		/*note that there is no O(17) in this table - it's directly in the code */
		static byte[] eg_rate_select = new byte[32 + 64 + 32]{  /* Envelope Generator rates (32 + 64 rates + 32 RKS) */
		/* 32 infinite time rates (same as Rate 0) */
		eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),
		eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),
		eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),
		eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO(18),

		/* rates 00-11 */
		/*
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		*/
		eg_rate_selectO(18),eg_rate_selectO(18),eg_rate_selectO( 0),eg_rate_selectO( 0),
		eg_rate_selectO( 0),eg_rate_selectO( 0),eg_rate_selectO( 2),eg_rate_selectO( 2),   /* Nemesis's tests */

		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),
		eg_rate_selectO( 0),eg_rate_selectO( 1),eg_rate_selectO( 2),eg_rate_selectO( 3),

		/* rate 12 */
		eg_rate_selectO( 4),eg_rate_selectO( 5),eg_rate_selectO( 6),eg_rate_selectO( 7),

		/* rate 13 */
		eg_rate_selectO( 8),eg_rate_selectO( 9),eg_rate_selectO(10),eg_rate_selectO(11),

		/* rate 14 */
		eg_rate_selectO(12),eg_rate_selectO(13),eg_rate_selectO(14),eg_rate_selectO(15),

		/* rate 15 */
		eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),

		/* 32 dummy rates (same as 15 3) */
		eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),
		eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),
		eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),
		eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16),eg_rate_selectO(16)

		};


		/*rate  0,    1,    2,   3,   4,   5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15*/
		/*shift 11,   10,   9,   8,   7,   6,  5,  4,  3,  2, 1,  0,  0,  0,  0,  0 */
		/*mask  2047, 1023, 511, 255, 127, 63, 31, 15, 7,  3, 1,  0,  0,  0,  0,  0 */

		private static byte eg_rate_shiftO(byte a) { return (byte)(a * 1); }
		static byte[] eg_rate_shift = new byte[32 + 64 + 32]{  /* Envelope Generator counter shifts (32 + 64 rates + 32 RKS) */
		/* 32 infinite time rates */
		/* eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),
		eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),
		eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),
		eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0),eg_rate_shiftO(0), */

		/* fixed (should be the same as rate 0, even if it makes no difference since increment value is 0 for these rates) */
		eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),
		eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),
		eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),
		eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),

		/* rates 00-11 */
		eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),eg_rate_shiftO(11),
		eg_rate_shiftO(10),eg_rate_shiftO(10),eg_rate_shiftO(10),eg_rate_shiftO(10),
		eg_rate_shiftO( 9),eg_rate_shiftO( 9),eg_rate_shiftO( 9),eg_rate_shiftO( 9),
		eg_rate_shiftO( 8),eg_rate_shiftO( 8),eg_rate_shiftO( 8),eg_rate_shiftO( 8),
		eg_rate_shiftO( 7),eg_rate_shiftO( 7),eg_rate_shiftO( 7),eg_rate_shiftO( 7),
		eg_rate_shiftO( 6),eg_rate_shiftO( 6),eg_rate_shiftO( 6),eg_rate_shiftO( 6),
		eg_rate_shiftO( 5),eg_rate_shiftO( 5),eg_rate_shiftO( 5),eg_rate_shiftO( 5),
		eg_rate_shiftO( 4),eg_rate_shiftO( 4),eg_rate_shiftO( 4),eg_rate_shiftO( 4),
		eg_rate_shiftO( 3),eg_rate_shiftO( 3),eg_rate_shiftO( 3),eg_rate_shiftO( 3),
		eg_rate_shiftO( 2),eg_rate_shiftO( 2),eg_rate_shiftO( 2),eg_rate_shiftO( 2),
		eg_rate_shiftO( 1),eg_rate_shiftO( 1),eg_rate_shiftO( 1),eg_rate_shiftO( 1),
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),

		/* rate 12 */
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),

		/* rate 13 */
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),

		/* rate 14 */
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),

		/* rate 15 */
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),

		/* 32 dummy rates (same as 15 3) */
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),
		eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0),eg_rate_shiftO( 0)

		};

		static byte[] dt_tab = new byte[4 * 32]{
		/* this is YM2151 and YM2612 phase increment data (in 10.10 fixed point format)*/
		/* FD=0 */
		  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		/* FD=1 */
		  0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
		  2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8, 8, 8, 8,
		/* FD=2 */
		  1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
		  5, 6, 6, 7, 8, 8, 9,10,11,12,13,14,16,16,16,16,
		/* FD=3 */
		  2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7,
		  8 , 8, 9,10,11,12,13,14,16,17,19,20,22,22,22,22
		};


		/* OPN key frequency number -> key code follow table */
		/* fnum higher 4bit -> keycode lower 2bit */
		static byte[] opn_fktable = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 3, 3, 3 };


		/* 8 LFO speed parameters */
		/* each value represents number of samples that one LFO level will last for */
		static uint[] lfo_samples_per_step = new uint[8] { 108, 77, 71, 67, 62, 44, 8, 5 };


		/*There are 4 different LFO AM depths available, they are:
		  0 dB, 1.4 dB, 5.9 dB, 11.8 dB
		  Here is how it is generated (in EG steps):

		  11.8 dB = 0, 2, 4, 6, 8, 10,12,14,16...126,126,124,122,120,118,....4,2,0
		   5.9 dB = 0, 1, 2, 3, 4, 5, 6, 7, 8....63, 63, 62, 61, 60, 59,.....2,1,0
		   1.4 dB = 0, 0, 0, 0, 1, 1, 1, 1, 2,...15, 15, 15, 15, 14, 14,.....0,0,0

		  (1.4 dB is loosing precision as you can see)

		  It's implemented as generator from 0..126 with step 2 then a shift
		  right N times, where N is:
			8 for 0 dB
			3 for 1.4 dB
			1 for 5.9 dB
			0 for 11.8 dB
		*/
		static byte[] lfo_ams_depth_shift = new byte[4] { 8, 3, 1, 0 };


		/*There are 8 different LFO PM depths available, they are:
		  0, 3.4, 6.7, 10, 14, 20, 40, 80 (cents)

		  Modulation level at each depth depends on F-NUMBER bits: 4,5,6,7,8,9,10
		  (bits 8,9,10 = FNUM MSB from OCT/FNUM register)

		  Here we store only first quarter (positive one) of full waveform.
		  Full table (lfo_pm_table) containing all 128 waveforms is build
		  at run (init) time.

		  One value in table below represents 4 (four) basic LFO steps
		  (1 PM step = 4 AM steps).

		  For example:
		   at LFO SPEED=0 (which is 108 samples per basic LFO step)
		   one value from "lfo_pm_output" table lasts for 432 consecutive
		   samples (4*108=432) and one full LFO waveform cycle lasts for 13824
		   samples (32*432=13824; 32 because we store only a quarter of whole
					waveform in the table below)
		*/
		static byte[,] lfo_pm_output = new byte[7 * 8, 8]{
		/* 7 bits meaningful (of F-NUMBER), 8 LFO output levels per one depth (out of 32), 8 LFO depths */
		/* FNUM BIT 4: 000 0001xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 2 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 3 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 4 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 5 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 6 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 7 */ {0,   0,   0,   0,   1,   1,   1,   1},

		/* FNUM BIT 5: 000 0010xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 2 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 3 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 4 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 5 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 6 */ {0,   0,   0,   0,   1,   1,   1,   1},
		/* DEPTH 7 */ {0,   0,   1,   1,   2,   2,   2,   3},

		/* FNUM BIT 6: 000 0100xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 2 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 3 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 4 */ {0,   0,   0,   0,   0,   0,   0,   1},
		/* DEPTH 5 */ {0,   0,   0,   0,   1,   1,   1,   1},
		/* DEPTH 6 */ {0,   0,   1,   1,   2,   2,   2,   3},
		/* DEPTH 7 */ {0,   0,   2,   3,   4,   4,   5,   6},

		/* FNUM BIT 7: 000 1000xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 2 */ {0,   0,   0,   0,   0,   0,   1,   1},
		/* DEPTH 3 */ {0,   0,   0,   0,   1,   1,   1,   1},
		/* DEPTH 4 */ {0,   0,   0,   1,   1,   1,   1,   2},
		/* DEPTH 5 */ {0,   0,   1,   1,   2,   2,   2,   3},
		/* DEPTH 6 */ {0,   0,   2,   3,   4,   4,   5,   6},
		/* DEPTH 7 */ {0,   0,   4,   6,   8,   8, 0xa, 0xc},

		/* FNUM BIT 8: 001 0000xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   1,   1,   1,   1},
		/* DEPTH 2 */ {0,   0,   0,   1,   1,   1,   2,   2},
		/* DEPTH 3 */ {0,   0,   1,   1,   2,   2,   3,   3},
		/* DEPTH 4 */ {0,   0,   1,   2,   2,   2,   3,   4},
		/* DEPTH 5 */ {0,   0,   2,   3,   4,   4,   5,   6},
		/* DEPTH 6 */ {0,   0,   4,   6,   8,   8, 0xa, 0xc},
		/* DEPTH 7 */ {0,   0,   8, 0xc,0x10,0x10,0x14,0x18},

		/* FNUM BIT 9: 010 0000xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   2,   2,   2,   2},
		/* DEPTH 2 */ {0,   0,   0,   2,   2,   2,   4,   4},
		/* DEPTH 3 */ {0,   0,   2,   2,   4,   4,   6,   6},
		/* DEPTH 4 */ {0,   0,   2,   4,   4,   4,   6,   8},
		/* DEPTH 5 */ {0,   0,   4,   6,   8,   8, 0xa, 0xc},
		/* DEPTH 6 */ {0,   0,   8, 0xc,0x10,0x10,0x14,0x18},
		/* DEPTH 7 */ {0,   0,0x10,0x18,0x20,0x20,0x28,0x30},

		/* FNUM BIT10: 100 0000xxxx */
		/* DEPTH 0 */ {0,   0,   0,   0,   0,   0,   0,   0},
		/* DEPTH 1 */ {0,   0,   0,   0,   4,   4,   4,   4},
		/* DEPTH 2 */ {0,   0,   0,   4,   4,   4,   8,   8},
		/* DEPTH 3 */ {0,   0,   4,   4,   8,   8, 0xc, 0xc},
		/* DEPTH 4 */ {0,   0,   4,   8,   8,   8, 0xc,0x10},
		/* DEPTH 5 */ {0,   0,   8, 0xc,0x10,0x10,0x14,0x18},
		/* DEPTH 6 */ {0,   0,0x10,0x18,0x20,0x20,0x28,0x30},
		/* DEPTH 7 */ {0,   0,0x20,0x30,0x40,0x40,0x50,0x60},

		};


		/* all 128 LFO PM waveforms */

		const int STATE_SIZE = 0x48100;
		const string STATE_VERSION = "GENPLUS-GX 1.6.1";


		const int ENV_QUIET = (TL_TAB_LEN >> 3);

		/* register number to channel number , slot offset */
		//#define OPN_CHAN(N) (N&3)
		//#define OPN_SLOT(N) ((N>>2)&3)

		/* slot number */
		const int SLOT1 = 0;
		const int SLOT2 = 2;
		const int SLOT3 = 1;
		const int SLOT4 = 3;

		/* Function prototypes */
		//extern int state_load(unsigned char *state);
		//extern int state_save(unsigned char *state);

		const int TL_TAB_LEN = (13 * 2 * TL_RES_LEN);


		/* struct describing a single operator (SLOT) */
		class FM_SLOT
		{
			public LongPointerArray32 DT = new LongPointerArray32();        /* detune          :dt_tab[DT]      */
			public byte KSR;        /* key scale rate  :3-KSR           */
			public uint ar;         /* attack rate                      */
			public uint d1r;        /* decay rate                       */
			public uint d2r;        /* sustain rate                     */
			public uint rr;         /* release rate                     */
			public byte ksr;        /* key scale rate  :kcode>>(3-KSR)  */
			public uint mul;        /* multiple        :ML_TABLE[ML]    */

			/* Phase Generator */
			public uint phase;      /* phase counter */
			public long Incr;       /* phase step */

			/* Envelope Generator */
			public byte state;      /* phase type */
			public uint tl;         /* total level: TL << 3 */
			public long volume;     /* envelope counter */
			public uint sl;         /* sustain level:sl_table[SL] */
			public uint vol_out;    /* current output from EG circuit (without AM from LFO) */

			public byte eg_sh_ar;    /*  (attack state)  */
			public byte eg_sel_ar;   /*  (attack state)  */
			public byte eg_sh_d1r;   /*  (decay state)   */
			public byte eg_sel_d1r;  /*  (decay state)   */
			public byte eg_sh_d2r;   /*  (sustain state) */
			public byte eg_sel_d2r;  /*  (sustain state) */
			public byte eg_sh_rr;    /*  (release state) */
			public byte eg_sel_rr;   /*  (release state) */

			public byte ssg;         /* SSG-EG waveform  */
			public byte ssgn;        /* SSG-EG negated output  */

			public byte key;         /* 0=last key was KEY OFF, 1=KEY ON */

			/* LFO */
			public uint AMmask;     /* AM enable flag */

		};

		class FM_CH
		{
			public FM_SLOT[] SLOT = new FM_SLOT[4];     /* four SLOTs (operators) */

			public byte ALGO;         /* algorithm */
			public byte FB;           /* feedback shift */
			public long[] op1_out = new long[2];   /* op1 output for feedback */

			public LongPointer connect1;    /* SLOT1 output pointer */
			public LongPointer connect3;    /* SLOT3 output pointer */
			public LongPointer connect2;    /* SLOT2 output pointer */
			public LongPointer connect4;    /* SLOT4 output pointer */

			public LongPointer mem_connect; /* where to put the delayed sample (MEM) */
			public long mem_value;    /* delayed sample (MEM) value */

			public long pms;          /* channel PMS */
			public byte ams;          /* channel AMS */

			public uint fc;           /* fnum,blk:adjusted to sample rate */
			public byte kcode;        /* key code */
			public uint block_fnum;   /* current blk/fnum value for this slot (can be different betweeen slots of one channel in 3slot mode) */

			public FM_CH()
			{
				SLOT[0] = new FM_SLOT();
				SLOT[1] = new FM_SLOT();
				SLOT[2] = new FM_SLOT();
				SLOT[3] = new FM_SLOT();
			}
		};


		class FM_ST
		{
			public double clock;          /* master clock  (Hz)   */
			public uint rate;           /* sampling rate (Hz)   */
			public ushort address;        /* address register     */
			public byte status;         /* status flag          */
			public uint mode;           /* mode  CSM / 3SLOT    */
			public byte fn_h;           /* freq latch           */
			public long TimerBase;      /* Timer base time      */
			public long TA;             /* timer a value        */
			public long TAL;            /* timer a base          */
			public long TAC;            /* timer a counter      */
			public long TB;             /* timer b value        */
			public long TBL;            /* timer b base          */
			public long TBC;            /* timer b counter      */
			public LongPointerArray32[] dt_tab = new LongPointerArray32[8];  /* DeTune table         */
			public FM_ST()
			{
				for (int i = 0; i < 8; i++)
					dt_tab[i] = new LongPointerArray32();
			}
		};


		/***********************************************************/
		/* OPN unit                                                */
		/***********************************************************/

		/* OPN 3slot struct */
		class FM_3SLOT
		{
			public uint[] fc = new uint[3];          /* fnum3,blk3: calculated */
			public byte fn_h;           /* freq3 latch */
			public byte[] kcode = new byte[3];       /* key code */
			public uint[] block_fnum = new uint[3];  /* current fnum value for this slot (can be different betweeen slots of one channel in 3slot mode) */
			public byte key_csm;        /* CSM mode Key-ON flag */

		};

		/* OPN/A/B common state */
		class FM_OPN
		{
			public FM_ST ST = new FM_ST();                  /* general state */
			public FM_3SLOT SL3 = new FM_3SLOT();             /* 3 slot mode state */
			public uint[] pan = new uint[6 * 2];      /* fm channels output masks (0xffffffff = enable) */

			public uint eg_cnt;             /* global envelope generator counter */
			public uint eg_timer;           /* global envelope generator counter works at frequency = chipclock/144/3 */
			public uint eg_timer_add;       /* step of eg_timer */
			public uint eg_timer_overflow;  /* envelope generator timer overlfows every 3 samples (on real chip) */

			/* there are 2048 FNUMs that can be generated using FNUM/BLK registers
				  but LFO works with one more bit of a precision so we really need 4096 elements */
			public uint[] fn_table = new uint[4096];     /* fnumber->increment counter */
			public uint fn_max;             /* max increment (required for calculating phase overflow) */

			/* LFO */
			public byte lfo_cnt;            /* current LFO phase (out of 128) */
			public uint lfo_timer;          /* current LFO phase runs at LFO frequency */
			public uint lfo_timer_add;      /* step of lfo_timer */
			public uint lfo_timer_overflow; /* LFO timer overflows every N samples (depends on LFO frequency) */
			public uint LFO_AM;             /* current LFO AM step */
			public uint LFO_PM;             /* current LFO PM step */
		};

		/***********************************************************/
		/* YM2612 chip                                                */
		/***********************************************************/
		class _YM2612_data
		{
			public FM_CH[] CH = new FM_CH[6];  /* channel state */
			public byte dacen;  /* DAC mode  */
			public long dacout; /* DAC output */
			public FM_OPN OPN = new FM_OPN();    /* OPN state */
			public _YM2612_data()
			{
				for (int i = 0; i < 6; i++)
					CH[i] = new FM_CH();
			}
		};

		/* limiter */
		/*#define Limit(val, max,min) { \
		  if ( val > max )      val = max; \
		  else if ( val < min ) val = min; \
		}*/

		int[] tl_tab = new int[TL_TAB_LEN];

		/* sin waveform table in 'decibel' scale */
		uint[] sin_tab = new uint[SIN_LEN];

		long[] lfo_pm_table = new long[128 * 8 * 32]; /* 128 combinations of 7 bits meaningful (of F-NUMBER), 8 LFO depths, 32 LFO output levels per one depth */
		t_config config = new t_config();

		/* emulated chip */
		_YM2612_data ym2612 = new _YM2612_data();

		/* current chip state */
		LongPointer m2 = new LongPointer();
		LongPointer c1 = new LongPointer();
		LongPointer c2 = new LongPointer();   /* Phase Modulation input for operators 2,3,4 */
		LongPointer mem = new LongPointer();        /* one sample delay memory */
		LongPointer[] out_fm = new LongPointer[8];  /* outputs of working channels */
		public YM2612Core()
		{
			for (int i = 0; i < 8; i++)
				out_fm[i] = new LongPointer();
		}

		void FM_KEYON(FM_CH CH, int s)
		{
			FM_SLOT SLOT = CH.SLOT[s];

			if (SLOT.key == 0 && ym2612.OPN.SL3.key_csm == 0)
			{
				/* restart Phase Generator */
				SLOT.phase = 0;

				/* reset SSG-EG inversion flag */
				SLOT.ssgn = 0;

				if ((SLOT.ar + SLOT.ksr) < 94 /*32+62*/)
				{
					SLOT.state = (byte)((SLOT.volume <= MIN_ATT_INDEX) ? ((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC) : EG_ATT);
				}
				else
				{
					/* force attenuation level to 0 */
					SLOT.volume = MIN_ATT_INDEX;

					/* directly switch to Decay (or Sustain) */
					SLOT.state = (byte)((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC);
				}

				/* recalculate EG output */
				if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)
					SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
				else
					SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
			}

			SLOT.key = 1;
		}


		void FM_KEYOFF(FM_CH CH, int s)
		{
			FM_SLOT SLOT = CH.SLOT[s];

			if (SLOT.key > 0 && ym2612.OPN.SL3.key_csm == 0)
			{
				if (SLOT.state > EG_REL)
				{
					SLOT.state = EG_REL; /* phase . Release */

					/* SSG-EG specific update */
					if ((SLOT.ssg & 0x08) > 0)
					{
						/* convert EG attenuation level */
						if ((SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)
							SLOT.volume = (0x200 - SLOT.volume);

						/* force EG attenuation level */
						if (SLOT.volume >= 0x200)
						{
							SLOT.volume = MAX_ATT_INDEX;
							SLOT.state = EG_OFF;
						}

						/* recalculate EG output */
						SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
					}
				}
			}

			SLOT.key = 0;
		}

		void FM_KEYON_CSM(FM_CH CH, int s)
		{
			FM_SLOT SLOT = CH.SLOT[s];

			if (SLOT.key == 0 && ym2612.OPN.SL3.key_csm == 0)
			{
				/* restart Phase Generator */
				SLOT.phase = 0;

				/* reset SSG-EG inversion flag */
				SLOT.ssgn = 0;

				if ((SLOT.ar + SLOT.ksr) < 94 /*32+62*/)
				{
					SLOT.state = (byte)((SLOT.volume <= MIN_ATT_INDEX) ? ((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC) : EG_ATT);
				}
				else
				{
					/* force attenuation level to 0 */
					SLOT.volume = MIN_ATT_INDEX;

					/* directly switch to Decay (or Sustain) */
					SLOT.state = (byte)((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC);
				}

				/* recalculate EG output */
				if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)
					SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
				else
					SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
			}
		}

		void FM_KEYOFF_CSM(FM_CH CH, int s)
		{
			FM_SLOT SLOT = CH.SLOT[s];
			if (SLOT.key == 0)
			{
				if (SLOT.state > EG_REL)
				{
					SLOT.state = EG_REL; /* phase . Release */

					/* SSG-EG specific update */
					if ((SLOT.ssg & 0x08) > 0)
					{
						/* convert EG attenuation level */
						if ((SLOT.ssgn ^ (SLOT.ssg & 0x04)) > 0)
							SLOT.volume = (0x200 - SLOT.volume);

						/* force EG attenuation level */
						if (SLOT.volume >= 0x200)
						{
							SLOT.volume = MAX_ATT_INDEX;
							SLOT.state = EG_OFF;
						}

						/* recalculate EG output */
						SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
					}
				}
			}
		}


		/* CSM Key Controll */
		void CSMKeyControll(FM_CH CH)
		{
			/* all key ON (verified by Nemesis on real hardware) */
			FM_KEYON_CSM(CH, SLOT1);
			FM_KEYON_CSM(CH, SLOT2);
			FM_KEYON_CSM(CH, SLOT3);
			FM_KEYON_CSM(CH, SLOT4);
			ym2612.OPN.SL3.key_csm = 1;
		}

		void INTERNAL_TIMER_A()
		{
			if ((ym2612.OPN.ST.mode & 0x01) != 0)
			{
				if ((ym2612.OPN.ST.TAC -= ym2612.OPN.ST.TimerBase) <= 0)
				{
					/* set status (if enabled) */
					if ((ym2612.OPN.ST.mode & 0x04) != 0)
						ym2612.OPN.ST.status |= 0x01;

					/* reload the counter */
					if (ym2612.OPN.ST.TAL != 0)
						ym2612.OPN.ST.TAC += ym2612.OPN.ST.TAL;
					else
						ym2612.OPN.ST.TAC = ym2612.OPN.ST.TAL;

					/* CSM mode auto key on */
					if ((ym2612.OPN.ST.mode & 0xC0) == 0x80)
						CSMKeyControll(ym2612.CH[2]);
				}
			}
		}

		void INTERNAL_TIMER_B(int step)
		{
			if ((ym2612.OPN.ST.mode & 0x02) != 0)
			{
				if ((ym2612.OPN.ST.TBC -= (ym2612.OPN.ST.TimerBase * step)) <= 0)
				{
					/* set status (if enabled) */
					if ((ym2612.OPN.ST.mode & 0x08) != 0)
						ym2612.OPN.ST.status |= 0x02;

					/* reload the counter */
					if (ym2612.OPN.ST.TBL != 0)
						ym2612.OPN.ST.TBC += ym2612.OPN.ST.TBL;
					else
						ym2612.OPN.ST.TBC = ym2612.OPN.ST.TBL;
				}
			}
		}

		/* OPN Mode Register Write */
		void set_timers(int v)
		{
			/* b7 = CSM MODE */
			/* b6 = 3 slot mode */
			/* b5 = reset b */
			/* b4 = reset a */
			/* b3 = timer enable b */
			/* b2 = timer enable a */
			/* b1 = load b */
			/* b0 = load a */

			if (((ym2612.OPN.ST.mode ^ v) & 0xC0) != 0)
			{
				/* phase increment need to be recalculated */
				ym2612.CH[2].SLOT[SLOT1].Incr = -1;

				/* CSM mode disabled and CSM key ON active*/
				if (((v & 0xC0) != 0x80) && (ym2612.OPN.SL3.key_csm != 0))
				{
					/* CSM Mode Key OFF (verified by Nemesis on real hardware) */
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT1);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT2);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT3);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT4);
					ym2612.OPN.SL3.key_csm = 0;
				}
			}

			/* reload Timers */
			if ((v & 1) != 0 && (ym2612.OPN.ST.mode & 1) == 0)
				ym2612.OPN.ST.TAC = ym2612.OPN.ST.TAL;
			if ((v & 2) != 0 && (ym2612.OPN.ST.mode & 2) == 0)
				ym2612.OPN.ST.TBC = ym2612.OPN.ST.TBL;

			/* reset Timers flags */
			ym2612.OPN.ST.status &= (byte)(~v >> 4);

			ym2612.OPN.ST.mode = (uint)v;
		}



		/* set algorithm connection */
		void setup_connection(FM_CH CH, int ch)
		{
			LongPointer carrier = out_fm[ch];

			switch (CH.ALGO)
			{
				case 0:
					/* M1---C1---MEM---M2---C2---OUT */
					CH.connect1 = c1;
					CH.connect2 = mem;
					CH.connect3 = c2;
					CH.mem_connect = m2;
					break;
				case 1:
					/* M1------+-MEM---M2---C2---OUT */
					/*      C1-+                     */
					CH.connect1 = mem;
					CH.connect2 = mem;
					CH.connect3 = c2;
					CH.mem_connect = m2;
					break;
				case 2:
					/* M1-----------------+-C2---OUT */
					/*      C1---MEM---M2-+          */
					CH.connect1 = c2;
					CH.connect2 = mem;
					CH.connect3 = c2;
					CH.mem_connect = m2;
					break;
				case 3:
					/* M1---C1---MEM------+-C2---OUT */
					/*                 M2-+          */
					CH.connect1 = c1;
					CH.connect2 = mem;
					CH.connect3 = c2;
					CH.mem_connect = c2;
					break;
				case 4:
					/* M1---C1-+-OUT */
					/* M2---C2-+     */
					/* MEM: not used */
					CH.connect1 = c1;
					CH.connect2 = carrier;
					CH.connect3 = c2;
					CH.mem_connect = mem;  /* store it anywhere where it will not be used */
					break;
				case 5:
					/*    +----C1----+     */
					/* M1-+-MEM---M2-+-OUT */
					/*    +----C2----+     */
					CH.connect1 = null;  /* special mark */
					CH.connect2 = carrier;
					CH.connect3 = carrier;
					CH.mem_connect = m2;
					break;
				case 6:
					/* M1---C1-+     */
					/*      M2-+-OUT */
					/*      C2-+     */
					/* MEM: not used */
					CH.connect1 = c1;
					CH.connect2 = carrier;
					CH.connect3 = carrier;
					CH.mem_connect = mem;  /* store it anywhere where it will not be used */
					break;
				case 7:
					/* M1-+     */
					/* C1-+-OUT */
					/* M2-+     */
					/* C2-+     */
					/* MEM: not used*/
					CH.connect1 = carrier;
					CH.connect2 = carrier;
					CH.connect3 = carrier;
					CH.mem_connect = mem;  /* store it anywhere where it will not be used */
					break;
			}

			CH.connect4 = carrier;
		}

		/* set detune & multiple */
		void set_det_mul(FM_CH CH, FM_SLOT SLOT, int v)
		{
			SLOT.mul = (uint)(((v & 0x0f) != 0) ? (v & 0x0f) * 2 : 1);
			SLOT.DT = ym2612.OPN.ST.dt_tab[(v >> 4) & 7];
			CH.SLOT[SLOT1].Incr = -1;
		}

		/* set total level */
		void set_tl(FM_SLOT SLOT, int v)
		{
			SLOT.tl = (uint)((v & 0x7f) << (ENV_BITS - 7)); /* 7bit TL */

			/* recalculate EG output */
			if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0 && (SLOT.state > EG_REL))
				SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
			else
				SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
		}

		/* set attack rate & key scale  */
		void set_ar_ksr(FM_CH CH, FM_SLOT SLOT, int v)
		{
			byte old_KSR = SLOT.KSR;

			SLOT.ar = (uint)(((v & 0x1f) != 0) ? 32 + ((v & 0x1f) << 1) : 0);

			SLOT.KSR = (byte)(3 - (v >> 6));
			if (SLOT.KSR != old_KSR)
			{
				CH.SLOT[SLOT1].Incr = -1;
			}

			/* Even if it seems unnecessary to do it here, it could happen that KSR and KC  */
			/* are modified but the resulted SLOT.ksr value (kc >> SLOT.KSR) remains unchanged. */
			/* In such case, Attack Rate would not be recalculated by "refresh_fc_eg_slot". */
			/* This actually fixes the intro of "The Adventures of Batman & Robin" (Eke-Eke)         */
			if ((SLOT.ar + SLOT.ksr) < (32 + 62))
			{
				SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + SLOT.ksr];
				SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + SLOT.ksr];
			}
			else
			{
				/* verified by Nemesis on real hardware (Attack phase is blocked) */
				SLOT.eg_sh_ar = 0;
				SLOT.eg_sel_ar = 18 * RATE_STEPS;
			}
		}

		/* set decay rate */
		void set_dr(FM_SLOT SLOT, int v)
		{
			SLOT.d1r = (uint)(((v & 0x1f) != 0) ? 32 + ((v & 0x1f) << 1) : 0);

			SLOT.eg_sh_d1r = eg_rate_shift[SLOT.d1r + SLOT.ksr];
			SLOT.eg_sel_d1r = eg_rate_select[SLOT.d1r + SLOT.ksr];

		}

		/* set sustain rate */
		void set_sr(FM_SLOT SLOT, int v)
		{
			SLOT.d2r = (uint)(((v & 0x1f) != 0) ? 32 + ((v & 0x1f) << 1) : 0);

			SLOT.eg_sh_d2r = eg_rate_shift[SLOT.d2r + SLOT.ksr];
			SLOT.eg_sel_d2r = eg_rate_select[SLOT.d2r + SLOT.ksr];
		}

		/* set release rate */
		void set_sl_rr(FM_SLOT SLOT, int v)
		{
			SLOT.sl = sl_table[v >> 4];

			/* check EG state changes */
			if ((SLOT.state == EG_DEC) && (SLOT.volume >= (int)(SLOT.sl)))
				SLOT.state = EG_SUS;

			SLOT.rr = (uint)(34 + ((v & 0x0f) << 2));

			SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + SLOT.ksr];
			SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + SLOT.ksr];
		}

		/* advance LFO to next sample */
		void advance_lfo()
		{
			if (ym2612.OPN.lfo_timer_overflow != 0)   /* LFO enabled ? */
			{
				/* increment LFO timer */
				ym2612.OPN.lfo_timer += ym2612.OPN.lfo_timer_add;

				/* when LFO is enabled, one level will last for 108, 77, 71, 67, 62, 44, 8 or 5 samples */
				while (ym2612.OPN.lfo_timer >= ym2612.OPN.lfo_timer_overflow)
				{
					ym2612.OPN.lfo_timer -= ym2612.OPN.lfo_timer_overflow;

					/* There are 128 LFO steps */
					ym2612.OPN.lfo_cnt = (byte)((ym2612.OPN.lfo_cnt + 1) & 127);

					/* triangle */
					/* AM: 0 to 126 step +2, 126 to 0 step -2 */
					if (ym2612.OPN.lfo_cnt < 64)
						ym2612.OPN.LFO_AM = (uint)(ym2612.OPN.lfo_cnt * 2);
					else
						ym2612.OPN.LFO_AM = (uint)(126 - ((ym2612.OPN.lfo_cnt & 63) * 2));

					/* PM works with 4 times slower clock */
					ym2612.OPN.LFO_PM = (uint)(ym2612.OPN.lfo_cnt >> 2);
				}
			}
		}


		void advance_eg_channels()
		{
			uint eg_cnt = ym2612.OPN.eg_cnt;
			uint i = 0;
			uint j;
			FM_SLOT SLOT;
			int curSlot = SLOT1;

			do
			{
				curSlot = SLOT1;
				SLOT = ym2612.CH[i].SLOT[curSlot];
				j = 4; /* four operators per channel */
				do
				{
					switch (SLOT.state)
					{
						case EG_ATT:    /* attack phase */
							{
								if ((eg_cnt & ((1 << SLOT.eg_sh_ar) - 1)) == 0)
								{
									/* update attenuation level */
									SLOT.volume += (~SLOT.volume * (eg_inc[SLOT.eg_sel_ar + ((eg_cnt >> SLOT.eg_sh_ar) & 7)])) >> 4;

									/* check phase transition*/
									if (SLOT.volume <= MIN_ATT_INDEX)
									{
										SLOT.volume = MIN_ATT_INDEX;
										SLOT.state = (byte)((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC); /* special case where SL=0 */
									}

									/* recalculate EG output */
									if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)  /* SSG-EG Output Inversion */
										SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
									else
										SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
								}
								break;
							}

						case EG_DEC:  /* decay phase */
							{
								if ((eg_cnt & ((1 << SLOT.eg_sh_d1r) - 1)) == 0)
								{
									/* SSG EG type */
									if ((SLOT.ssg & 0x08) != 0)
									{
										/* update attenuation level */
										if (SLOT.volume < 0x200)
										{
											SLOT.volume += 4 * eg_inc[SLOT.eg_sel_d1r + ((eg_cnt >> SLOT.eg_sh_d1r) & 7)];

											/* recalculate EG output */
											if ((SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)   /* SSG-EG Output Inversion */
												SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
											else
												SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
										}
									}
									else
									{
										/* update attenuation level */
										SLOT.volume += eg_inc[SLOT.eg_sel_d1r + ((eg_cnt >> SLOT.eg_sh_d1r) & 7)];

										/* recalculate EG output */
										SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
									}

									/* check phase transition*/
									if (SLOT.volume >= (int)(SLOT.sl))
										SLOT.state = EG_SUS;
								}
								break;
							}

						case EG_SUS:  /* sustain phase */
							{
								if ((eg_cnt & ((1 << SLOT.eg_sh_d2r) - 1)) == 0)
								{
									/* SSG EG type */
									if ((SLOT.ssg & 0x08) != 0)
									{
										/* update attenuation level */
										if (SLOT.volume < 0x200)
										{
											SLOT.volume += 4 * eg_inc[SLOT.eg_sel_d2r + ((eg_cnt >> SLOT.eg_sh_d2r) & 7)];

											/* recalculate EG output */
											if ((SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)   /* SSG-EG Output Inversion */
												SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
											else
												SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
										}
									}
									else
									{
										/* update attenuation level */
										SLOT.volume += eg_inc[SLOT.eg_sel_d2r + ((eg_cnt >> SLOT.eg_sh_d2r) & 7)];

										/* check phase transition*/
										if (SLOT.volume >= MAX_ATT_INDEX)
											SLOT.volume = MAX_ATT_INDEX;
										/* do not change SLOT.state (verified on real chip) */

										/* recalculate EG output */
										SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
									}
								}
								break;
							}

						case EG_REL:  /* release phase */
							{
								if ((eg_cnt & ((1 << SLOT.eg_sh_rr) - 1)) == 0)
								{
									/* SSG EG type */
									if ((SLOT.ssg & 0x08) != 0)
									{
										/* update attenuation level */
										if (SLOT.volume < 0x200)
											SLOT.volume += 4 * eg_inc[SLOT.eg_sel_rr + ((eg_cnt >> SLOT.eg_sh_rr) & 7)];

										/* check phase transition */
										if (SLOT.volume >= 0x200)
										{
											SLOT.volume = MAX_ATT_INDEX;
											SLOT.state = EG_OFF;
										}
									}
									else
									{
										/* update attenuation level */
										SLOT.volume += eg_inc[SLOT.eg_sel_rr + ((eg_cnt >> SLOT.eg_sh_rr) & 7)];

										/* check phase transition*/
										if (SLOT.volume >= MAX_ATT_INDEX)
										{
											SLOT.volume = MAX_ATT_INDEX;
											SLOT.state = EG_OFF;
										}
									}

									/* recalculate EG output */
									SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;

								}
								break;
							}
					}
					curSlot += 1;
					if (curSlot < ym2612.CH[i].SLOT.Count())
						SLOT = ym2612.CH[i].SLOT[curSlot];
					j--;
				} while (j != 0);
				i++;
			} while (i < 6); /* 6 channels */
		}


		/* SSG-EG update process */
		/* The behavior is based upon Nemesis tests on real hardware */
		/* This is actually executed before each samples */
		void update_ssg_eg_channel(FM_SLOT[] SLOTS)
		{
			uint i = 4; /* four operators per channel */
			int curSlot = SLOT1;
			FM_SLOT SLOT = SLOTS[curSlot];
			do
			{
				/* detect SSG-EG transition */
				/* this is not required during release phase as the attenuation has been forced to MAX and output invert flag is not used */
				/* if an Attack Phase is programmed, inversion can occur on each sample */
				if ((SLOT.ssg & 0x08) != 0 && (SLOT.volume >= 0x200) && (SLOT.state > EG_REL))
				{
					if ((SLOT.ssg & 0x01) != 0)  /* bit 0 = hold SSG-EG */
					{
						/* set inversion flag */
						if ((SLOT.ssg & 0x02) != 0)
							SLOT.ssgn = 4;

						/* force attenuation level during decay phases */
						if ((SLOT.state != EG_ATT) && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) == 0)
							SLOT.volume = MAX_ATT_INDEX;
					}
					else  /* loop SSG-EG */
					{
						/* toggle output inversion flag or reset Phase Generator */
						if ((SLOT.ssg & 0x02) != 0)
							SLOT.ssgn ^= 4;
						else
							SLOT.phase = 0;

						/* same as Key ON */
						if (SLOT.state != EG_ATT)
						{
							if ((SLOT.ar + SLOT.ksr) < 94 /*32+62*/)
							{
								SLOT.state = (byte)((SLOT.volume <= MIN_ATT_INDEX) ? ((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC) : EG_ATT);
							}
							else
							{
								/* Attack Rate is maximal: directly switch to Decay or Substain */
								SLOT.volume = MIN_ATT_INDEX;
								SLOT.state = (byte)((SLOT.sl == MIN_ATT_INDEX) ? EG_SUS : EG_DEC);
							}
						}
					}

					/* recalculate EG output */
					if ((SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)
						SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
					else
						SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
				}

				/* next slot */
				curSlot++;
				if (curSlot < SLOTS.Count())
					SLOT = SLOTS[curSlot];
				i--;
			} while (i != 0);
		}

		void update_phase_lfo_slot(FM_SLOT SLOT, long pms, uint block_fnum)
		{
			long lfo_fn_table_index_offset = lfo_pm_table[(((block_fnum & 0x7f0) >> 4) << 8) + pms + ym2612.OPN.LFO_PM];

			if (lfo_fn_table_index_offset != 0)  /* LFO phase modulation active */
			{
				byte blk;
				int kc, fc;

				block_fnum = (uint)(block_fnum * 2 + lfo_fn_table_index_offset);
				blk = (byte)((block_fnum & 0x7000) >> 12);
				block_fnum = block_fnum & 0xfff;

				/* keyscale code */
				kc = (blk << 2) | opn_fktable[block_fnum >> 8];

				/* (frequency) phase increment counter */
				fc = (int)((ym2612.OPN.fn_table[block_fnum] >> (7 - blk)) + SLOT.DT.value[kc]);

				/* (frequency) phase overflow (credits to Nemesis) */
				if (fc < 0) fc += (int)ym2612.OPN.fn_max;

				/* update phase */
				SLOT.phase += (uint)((fc * SLOT.mul) >> 1);
			}
			else  /* LFO phase modulation  = zero */
			{
				SLOT.phase += (uint)SLOT.Incr;
			}
		}

		void update_phase_lfo_channel(FM_CH CH)
		{
			uint block_fnum = CH.block_fnum;

			long lfo_fn_table_index_offset = lfo_pm_table[(((block_fnum & 0x7f0) >> 4) << 8) + CH.pms + ym2612.OPN.LFO_PM];

			if (lfo_fn_table_index_offset != 0)  /* LFO phase modulation active */
			{
				byte blk;
				int kc, fc, finc;

				block_fnum = (uint)(block_fnum * 2 + lfo_fn_table_index_offset);
				blk = (byte)((block_fnum & 0x7000) >> 12);
				block_fnum = block_fnum & 0xfff;

				/* keyscale code */
				kc = (blk << 2) | opn_fktable[block_fnum >> 8];

				/* (frequency) phase increment counter */
				fc = (int)(ym2612.OPN.fn_table[block_fnum] >> (7 - blk));

				/* (frequency) phase overflow (credits to Nemesis) */
				finc = (int)(fc + CH.SLOT[SLOT1].DT.value[kc]);
				if (finc < 0) finc += (int)ym2612.OPN.fn_max;
				CH.SLOT[SLOT1].phase += (uint)((finc * CH.SLOT[SLOT1].mul) >> 1);

				finc = (int)(fc + CH.SLOT[SLOT2].DT.value[kc]);
				if (finc < 0) finc += (int)ym2612.OPN.fn_max;
				CH.SLOT[SLOT2].phase += (uint)((finc * CH.SLOT[SLOT2].mul) >> 1);

				finc = (int)(fc + CH.SLOT[SLOT3].DT.value[kc]);
				if (finc < 0) finc += (int)ym2612.OPN.fn_max;
				CH.SLOT[SLOT3].phase += (uint)((finc * CH.SLOT[SLOT3].mul) >> 1);

				finc = (int)(fc + CH.SLOT[SLOT4].DT.value[kc]);
				if (finc < 0) finc += (int)ym2612.OPN.fn_max;
				CH.SLOT[SLOT4].phase += (uint)((finc * CH.SLOT[SLOT4].mul) >> 1);
			}
			else  /* LFO phase modulation  = zero */
			{
				CH.SLOT[SLOT1].phase += (uint)CH.SLOT[SLOT1].Incr;
				CH.SLOT[SLOT2].phase += (uint)CH.SLOT[SLOT2].Incr;
				CH.SLOT[SLOT3].phase += (uint)CH.SLOT[SLOT3].Incr;
				CH.SLOT[SLOT4].phase += (uint)CH.SLOT[SLOT4].Incr;
			}
		}



		/* update phase increment and envelope generator */
		void refresh_fc_eg_slot(FM_SLOT SLOT, int fc, int kc)
		{
			/* add detune value */
			fc += (int)SLOT.DT.value[kc];

			/* (frequency) phase overflow (credits to Nemesis) */
			if (fc < 0) fc += (int)ym2612.OPN.fn_max;

			/* (frequency) phase increment counter */
			SLOT.Incr = (fc * SLOT.mul) >> 1;

			/* ksr */
			kc = kc >> SLOT.KSR;

			if (SLOT.ksr != kc)
			{
				SLOT.ksr = (byte)kc;

				/* recalculate envelope generator rates */
				if ((SLOT.ar + kc) < (32 + 62))
				{
					SLOT.eg_sh_ar = eg_rate_shift[SLOT.ar + kc];
					SLOT.eg_sel_ar = eg_rate_select[SLOT.ar + kc];
				}
				else
				{
					/* verified by Nemesis on real hardware (Attack phase is blocked) */
					SLOT.eg_sh_ar = 0;
					SLOT.eg_sel_ar = 18 * RATE_STEPS;
				}

				SLOT.eg_sh_d1r = eg_rate_shift[SLOT.d1r + kc];
				SLOT.eg_sel_d1r = eg_rate_select[SLOT.d1r + kc];

				SLOT.eg_sh_d2r = eg_rate_shift[SLOT.d2r + kc];
				SLOT.eg_sel_d2r = eg_rate_select[SLOT.d2r + kc];

				SLOT.eg_sh_rr = eg_rate_shift[SLOT.rr + kc];
				SLOT.eg_sel_rr = eg_rate_select[SLOT.rr + kc];
			}
		}

		/* update phase increment counters */
		void refresh_fc_eg_chan(FM_CH CH)
		{
			if (CH.SLOT[SLOT1].Incr == -1)
			{
				int fc = (int)CH.fc;
				int kc = (int)CH.kcode;
				refresh_fc_eg_slot(CH.SLOT[SLOT1], fc, kc);
				refresh_fc_eg_slot(CH.SLOT[SLOT2], fc, kc);
				refresh_fc_eg_slot(CH.SLOT[SLOT3], fc, kc);
				refresh_fc_eg_slot(CH.SLOT[SLOT4], fc, kc);
			}
		}


		//#define volume_calc(OP) ((OP)->vol_out + (AM & (OP)->AMmask))



		uint volume_calc(FM_SLOT slot, uint AM) { return slot.vol_out + (AM & slot.AMmask); }

		int op_calc(uint phase, uint env, int pm)
		{
			uint p = (env << 3) + sin_tab[(((int)((phase & ~FREQ_MASK) + (pm << 15))) >> FREQ_SH) & SIN_MASK_GX];

			if (p >= TL_TAB_LEN)
				return 0;
			return tl_tab[p];
		}

		int op_calc1(uint phase, uint env, int pm)
		{
			uint p = (env << 3) + sin_tab[(((int)((phase & ~FREQ_MASK) + pm)) >> FREQ_SH) & SIN_MASK_GX];

			if (p >= TL_TAB_LEN)
				return 0;
			return tl_tab[p];
		}

		void chan_calc(FM_CH CH)
		{
			uint AM = (uint)ym2612.OPN.LFO_AM >> CH.ams;
			uint eg_out = volume_calc(CH.SLOT[SLOT1], AM);

			m2.value = c1.value = c2.value = mem.value = 0;

			CH.mem_connect.value = CH.mem_value;  /* restore delayed sample (MEM) value to m2 or c2 */
			{
				long outVal = CH.op1_out[0] + CH.op1_out[1];
				CH.op1_out[0] = CH.op1_out[1];

				if (CH.connect1 == null)
				{
					/* algorithm 5  */
					mem.value = c1.value = c2.value = CH.op1_out[0];
				}
				else
				{
					/* other algorithms */
					CH.connect1.value += CH.op1_out[0];
				}

				CH.op1_out[1] = 0;
				if (eg_out < ENV_QUIET)  /* SLOT 1 */
				{
					if (CH.FB == 0)
						outVal = 0;

					CH.op1_out[1] = op_calc1(CH.SLOT[SLOT1].phase, eg_out, (int)(outVal << CH.FB));
				}
			}

			eg_out = volume_calc(CH.SLOT[SLOT3], AM);
			if (eg_out < ENV_QUIET)    /* SLOT 3 */
				CH.connect3.value += op_calc(CH.SLOT[SLOT3].phase, eg_out, (int)m2.value);

			eg_out = volume_calc(CH.SLOT[SLOT2], AM);
			if (eg_out < ENV_QUIET)    /* SLOT 2 */
				CH.connect2.value += op_calc(CH.SLOT[SLOT2].phase, eg_out, (int)c1.value);

			eg_out = volume_calc(CH.SLOT[SLOT4], AM);
			if (eg_out < ENV_QUIET)    /* SLOT 4 */
				CH.connect4.value += op_calc(CH.SLOT[SLOT4].phase, eg_out, (int)c2.value);


			/* store current MEM */
			CH.mem_value = mem.value;

			/* update phase counters AFTER output calculations */
			if (CH.pms != 0)
			{
				/* add support for 3 slot mode */
				if ((ym2612.OPN.ST.mode & 0xC0) > 0 && (CH == ym2612.CH[2]))
				{
					update_phase_lfo_slot(CH.SLOT[SLOT1], CH.pms, ym2612.OPN.SL3.block_fnum[1]);
					update_phase_lfo_slot(CH.SLOT[SLOT2], CH.pms, ym2612.OPN.SL3.block_fnum[2]);
					update_phase_lfo_slot(CH.SLOT[SLOT3], CH.pms, ym2612.OPN.SL3.block_fnum[0]);
					update_phase_lfo_slot(CH.SLOT[SLOT4], CH.pms, CH.block_fnum);
				}
				else update_phase_lfo_channel(CH);
			}
			else  /* no LFO phase modulation */
			{
				CH.SLOT[SLOT1].phase += (uint)CH.SLOT[SLOT1].Incr;
				CH.SLOT[SLOT2].phase += (uint)CH.SLOT[SLOT2].Incr;
				CH.SLOT[SLOT3].phase += (uint)CH.SLOT[SLOT3].Incr;
				CH.SLOT[SLOT4].phase += (uint)CH.SLOT[SLOT4].Incr;
			}
		}


		/* write a OPN mode register 0x20-0x2f */
		void OPNWriteMode(int r, int v)
		{
			byte c;
			FM_CH CH;

			switch (r)
			{
				case 0x21:  /* Test */
					break;

				case 0x22:  /* LFO FREQ (YM2608/YM2610/YM2610B/ym2612) */
					if ((v & 8) != 0) /* LFO enabled ? */
					{
						if (ym2612.OPN.lfo_timer_overflow == 0)
						{
							/* restart LFO */
							ym2612.OPN.lfo_cnt = 0;
							ym2612.OPN.lfo_timer = 0;
							ym2612.OPN.LFO_AM = 0;
							ym2612.OPN.LFO_PM = 0;
						}

						ym2612.OPN.lfo_timer_overflow = lfo_samples_per_step[v & 7] << LFO_SH;
					}
					else
					{
						ym2612.OPN.lfo_timer_overflow = 0;
					}

					break;
				case 0x24:  /* timer A High 8*/
					ym2612.OPN.ST.TA = (ym2612.OPN.ST.TA & 0x03) | (((long)v) << 2);
					ym2612.OPN.ST.TAL = (1024 - ym2612.OPN.ST.TA) << TIMER_SH;
					break;
				case 0x25:  /* timer A Low 2*/
					ym2612.OPN.ST.TA = (ym2612.OPN.ST.TA & 0x3fc) | ((long)v & 3);
					ym2612.OPN.ST.TAL = (1024 - ym2612.OPN.ST.TA) << TIMER_SH;
					break;
				case 0x26:  /* timer B */
					ym2612.OPN.ST.TB = v;
					ym2612.OPN.ST.TBL = (256 - ym2612.OPN.ST.TB) << (TIMER_SH + 4);
					break;
				case 0x27:  /* mode, timer control */
					set_timers(v);
					break;
				case 0x28:  /* key on / off */
					c = (byte)(v & 0x03);
					if (c == 3) break;
					if ((v & 0x04) != 0) c += 3; /* CH 4-6 */
					CH = ym2612.CH[c];

					if ((v & 0x10) != 0) FM_KEYON(CH, SLOT1); else FM_KEYOFF(CH, SLOT1);
					if ((v & 0x20) != 0) FM_KEYON(CH, SLOT2); else FM_KEYOFF(CH, SLOT2);
					if ((v & 0x40) != 0) FM_KEYON(CH, SLOT3); else FM_KEYOFF(CH, SLOT3);
					if ((v & 0x80) != 0) FM_KEYON(CH, SLOT4); else FM_KEYOFF(CH, SLOT4);
					break;
			}
		}

		/* register number to channel number , slot offset */
		byte OPN_CHAN(int N) { return (byte)(N & 3); }
		int OPN_SLOT(int N) { return (N >> 2) & 3; }

		/* write a OPN register (0x30-0xff) */
		void OPNWriteReg(int r, int v)
		{
			FM_CH CH;
			FM_SLOT SLOT;

			byte c = OPN_CHAN(r);

			if (c == 3) return; /* 0xX3,0xX7,0xXB,0xXF */

			if (r >= 0x100) c += 3;

			CH = ym2612.CH[c];

			SLOT = (CH.SLOT[OPN_SLOT(r)]);

			switch (r & 0xf0)
			{
				case 0x30:  /* DET , MUL */
					set_det_mul(CH, SLOT, v);
					break;

				case 0x40:  /* TL */
					set_tl(SLOT, v);
					break;

				case 0x50:  /* KS, AR */
					set_ar_ksr(CH, SLOT, v);
					break;

				case 0x60:  /* bit7 = AM ENABLE, DR */
					set_dr(SLOT, v);
					SLOT.AMmask = (uint)(((v & 0x80) != 0) ? ~0 : 0);
					break;

				case 0x70:  /*     SR */
					set_sr(SLOT, v);
					break;

				case 0x80:  /* SL, RR */
					set_sl_rr(SLOT, v);
					break;

				case 0x90:  /* SSG-EG */
					SLOT.ssg = (byte)(v & 0x0f);

					/* recalculate EG output */
					if (SLOT.state > EG_REL)
					{
						if ((SLOT.ssg & 0x08) != 0 && (SLOT.ssgn ^ (SLOT.ssg & 0x04)) != 0)
							SLOT.vol_out = ((uint)(0x200 - SLOT.volume) & MAX_ATT_INDEX) + SLOT.tl;
						else
							SLOT.vol_out = (uint)SLOT.volume + SLOT.tl;
					}

					/* SSG-EG envelope shapes :

					E AtAlH
					1 0 0 0  \\\\

					1 0 0 1  \___

					1 0 1 0  \/\/
							  ___
					1 0 1 1  \

					1 1 0 0  ////
							  ___
					1 1 0 1  /

					1 1 1 0  /\/\

					1 1 1 1  /___


					E = SSG-EG enable


					The shapes are generated using Attack, Decay and Sustain phases.

					Each single character in the diagrams above represents this whole
					sequence:

					- when KEY-ON = 1, normal Attack phase is generated (*without* any
					  difference when compared to normal mode),

					- later, when envelope level reaches minimum level (max volume),
					  the EG switches to Decay phase (which works with bigger steps
					  when compared to normal mode - see below),

					- later when envelope level passes the SL level,
					  the EG swithes to Sustain phase (which works with bigger steps
					  when compared to normal mode - see below),

					- finally when envelope level reaches maximum level (min volume),
					  the EG switches to Attack phase again (depends on actual waveform).

					Important is that when switch to Attack phase occurs, the phase counter
					of that operator will be zeroed-out (as in normal KEY-ON) but not always.
					(I havent found the rule for that - perhaps only when the output level is low)

					The difference (when compared to normal Envelope Generator mode) is
					that the resolution in Decay and Sustain phases is 4 times lower;
					this results in only 256 steps instead of normal 1024.
					In other words:
					when SSG-EG is disabled, the step inside of the EG is one,
					when SSG-EG is enabled, the step is four (in Decay and Sustain phases).

					Times between the level changes are the same in both modes.


					Important:
					Decay 1 Level (so called SL) is compared to actual SSG-EG output, so
					it is the same in both SSG and no-SSG modes, with this exception:

					when the SSG-EG is enabled and is generating raising levels
					(when the EG output is inverted) the SL will be found at wrong level !!!
					For example, when SL=02:
					  0 -6 = -6dB in non-inverted EG output
					  96-6 = -90dB in inverted EG output
					Which means that EG compares its level to SL as usual, and that the
					output is simply inverted afterall.


					The Yamaha's manuals say that AR should be set to 0x1f (max speed).
					That is not necessary, but then EG will be generating Attack phase.

					*/


					break;

				case 0xa0:
					switch (OPN_SLOT(r))
					{
						case 0:    /* 0xa0-0xa2 : FNUM1 */
							{
								uint fn = (uint)((((uint)((ym2612.OPN.ST.fn_h) & 7)) << 8) + v);
								byte blk = (byte)(ym2612.OPN.ST.fn_h >> 3);
								/* keyscale code */
								CH.kcode = (byte)((blk << 2) | opn_fktable[fn >> 7]);
								/* phase increment counter */
								CH.fc = ym2612.OPN.fn_table[fn * 2] >> (7 - blk);

								/* store fnum in clear form for LFO PM calculations */
								CH.block_fnum = (uint)(((uint)blk << 11) | fn);

								CH.SLOT[SLOT1].Incr = -1;
								break;
							}
						case 1:    /* 0xa4-0xa6 : FNUM2,BLK */
							ym2612.OPN.ST.fn_h = (byte)(v & 0x3f);
							break;
						case 2:    /* 0xa8-0xaa : 3CH FNUM1 */
							if (r < 0x100)
							{
								uint fn = (uint)((((uint)(ym2612.OPN.SL3.fn_h & 7)) << 8) + v);
								byte blk = (byte)(ym2612.OPN.SL3.fn_h >> 3);
								/* keyscale code */
								ym2612.OPN.SL3.kcode[c] = (byte)((blk << 2) | opn_fktable[fn >> 7]);
								/* phase increment counter */
								ym2612.OPN.SL3.fc[c] = ym2612.OPN.fn_table[fn * 2] >> (7 - blk);
								ym2612.OPN.SL3.block_fnum[c] = (uint)(((uint)blk << 11) | fn);
								ym2612.CH[2].SLOT[SLOT1].Incr = -1;
							}
							break;
						case 3:    /* 0xac-0xae : 3CH FNUM2,BLK */
							if (r < 0x100)
								ym2612.OPN.SL3.fn_h = (byte)(v & 0x3f);
							break;
					}
					break;

				case 0xb0:
					switch (OPN_SLOT(r))
					{
						case 0:    /* 0xb0-0xb2 : FB,ALGO */
							{
								int feedback = (v >> 3) & 7;
								CH.ALGO = (byte)(v & 7);
								CH.FB = (byte)((feedback != 0) ? feedback + 6 : 0);
								setup_connection(CH, c);
								break;
							}
						case 1:    /* 0xb4-0xb6 : L , R , AMS , PMS (ym2612/YM2610B/YM2610/YM2608) */
							/* b0-2 PMS */
							CH.pms = (v & 7) * 32; /* CH.pms = PM depth * 32 (index in lfo_pm_table) */

							/* b4-5 AMS */
							CH.ams = lfo_ams_depth_shift[(v >> 4) & 0x03];

							/* PAN :  b7 = L, b6 = R */
							ym2612.OPN.pan[c * 2] = (uint)(((v & 0x80) != 0) ? ~0 : 0);
							ym2612.OPN.pan[c * 2 + 1] = (uint)(((v & 0x40) != 0) ? ~0 : 0);
							break;
					}
					break;
			}
		}


		/* initialize time tables */
		void init_timetables(double freqbase)
		{
			int i, d;
			double rate;

			/* DeTune table */
			for (d = 0; d <= 3; d++)
			{
				for (i = 0; i <= 31; i++)
				{
					rate = ((double)dt_tab[d * 32 + i]) * freqbase * (1 << (FREQ_SH - 10)); /* -10 because chip works with 10.10 fixed point, while we use 16.16 */
					ym2612.OPN.ST.dt_tab[d].value[i] = (int)rate;
					ym2612.OPN.ST.dt_tab[d + 4].value[i] = -ym2612.OPN.ST.dt_tab[d].value[i];
				}
			}

			/* there are 2048 FNUMs that can be generated using FNUM/BLK registers
				but LFO works with one more bit of a precision so we really need 4096 elements */
			/* calculate fnumber . increment counter table */
			for (i = 0; i < 4096; i++)
			{
				/* freq table for octave 7 */
				/* OPN phase increment counter = 20bit */
				/* the correct formula is : F-Number = (144 * fnote * 2^20 / M) / 2^(B-1) */
				/* where sample clock is  M/144 */
				/* this means the increment value for one clock sample is FNUM * 2^(B-1) = FNUM * 64 for octave 7 */
				/* we also need to handle the ratio between the chip frequency and the emulated frequency (can be 1.0)  */
				ym2612.OPN.fn_table[i] = (uint)((double)i * 32 * freqbase * (1 << (FREQ_SH - 10))); /* -10 because chip works with 10.10 fixed point, while we use 16.16 */
			}

			/* maximal frequency is required for Phase overflow calculation, register size is 17 bits (Nemesis) */
			ym2612.OPN.fn_max = (uint)((double)0x20000 * freqbase * (1 << (FREQ_SH - 10)));
		}

		/* prescaler set (and make time tables) */
		void OPNSetPres(int pres)
		{
			/* frequency base (ratio between FM original samplerate & desired output samplerate)*/
			double freqbase = ym2612.OPN.ST.clock / ym2612.OPN.ST.rate / pres;

			/* YM2612 running at original frequency (~53267 Hz) */
			if (config.hq_fm != 0) freqbase = 1.0;

			/* EG is updated every 3 samples */
			ym2612.OPN.eg_timer_add = (uint)((1 << EG_SH) * freqbase);
			ym2612.OPN.eg_timer_overflow = (3) * (1 << EG_SH);

			/* LFO timer increment (every samples) */
			ym2612.OPN.lfo_timer_add = (uint)((1 << LFO_SH) * freqbase);

			/* Timers increment (every samples) */
			ym2612.OPN.ST.TimerBase = (int)((1 << TIMER_SH) * freqbase);

			/* make time tables */
			init_timetables(freqbase);
		}

		void reset_channels(FM_CH[] CH, int num)
		{
			int c, s;

			for (c = 0; c < num; c++)
			{
				CH[c].mem_value = 0;
				CH[c].op1_out[0] = 0;
				CH[c].op1_out[1] = 0;
				for (s = 0; s < 4; s++)
				{
					CH[c].SLOT[s].Incr = -1;
					CH[c].SLOT[s].key = 0;
					CH[c].SLOT[s].phase = 0;
					CH[c].SLOT[s].ssgn = 0;
					CH[c].SLOT[s].state = EG_OFF;
					CH[c].SLOT[s].volume = MAX_ATT_INDEX;
					CH[c].SLOT[s].vol_out = MAX_ATT_INDEX;
				}
			}
		}

		/* initialize generic tables */
		void init_tables()
		{
			int i, x;
			int n;
			double o, m;

			/* DAC precision */
			uint mask = (uint)(~((1 << (14 - config.dac_bits)) - 1));

			/* build Linear Power Table */
			for (x = 0; x < TL_RES_LEN; x++)
			{
				m = (1 << 16) / Math.Pow(2, (x + 1) * (ENV_STEP_GX / 4.0) / 8.0);
				m = Math.Floor(m);

				/* we never reach (1<<16) here due to the (x+1) */
				/* result fits within 16 bits at maximum */

				n = (int)m; /* 16 bits here */
				n >>= 4;    /* 12 bits here */
				if ((n & 1) != 0)    /* round to nearest */
					n = (n >> 1) + 1;
				else
					n = n >> 1;
				/* 11 bits here (rounded) */
				n <<= 2;    /* 13 bits here (as in real chip) */

				/* 14 bits (with sign bit) */
				tl_tab[x * 2 + 0] = (int)(n & mask);
				tl_tab[x * 2 + 1] = (int)(-tl_tab[x * 2 + 0] & mask);

				/* one entry in the 'Power' table use the following format, xxxxxyyyyyyyys with:            */
				/*        s = sign bit                                                                      */
				/* yyyyyyyy = 8-bits decimal part (0-TL_RES_LEN)                                            */
				/* xxxxx    = 5-bits integer 'shift' value (0-31) but, since Power table output is 13 bits, */
				/*            any value above 13 (included) would be discarded.                             */
				for (i = 1; i < 13; i++)
				{
					tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN] = (int)((tl_tab[x * 2 + 0] >> i) & mask);
					tl_tab[x * 2 + 1 + i * 2 * TL_RES_LEN] = (int)(-tl_tab[x * 2 + 0 + i * 2 * TL_RES_LEN] & mask);
				}
			}

			/* build Logarithmic Sinus table */
			for (i = 0; i < SIN_LEN; i++)
			{
				/* non-standard sinus */
				m = Math.Sin(((i * 2) + 1) * Math.PI / SIN_LEN); /* checked against the real chip */
				/* we never reach zero here due to ((i*2)+1) */

				if (m > 0.0)
					o = 8 * Math.Log(1.0 / m) / Math.Log(2.0f);  /* convert to 'decibels' */
				else
					o = 8 * Math.Log(-1.0 / m) / Math.Log(2.0f);  /* convert to 'decibels' */

				o = o / (ENV_STEP_GX / 4);

				n = (int)(2.0 * o);
				if ((n & 1) != 0)            /* round to nearest */
					n = (n >> 1) + 1;
				else
					n = n >> 1;

				/* 13-bits (8.5) value is formatted for above 'Power' table */
				sin_tab[i] = (uint)(n * 2 + (m >= 0.0 ? 0 : 1));
			}

			/* build LFO PM modulation table */
			for (i = 0; i < 8; i++) /* 8 PM depths */
			{
				byte fnum;
				for (fnum = 0; fnum < 128; fnum++) /* 7 bits meaningful of F-NUMBER */
				{
					byte value;
					byte step;
					uint offset_depth = (uint)i;
					uint offset_fnum_bit;
					uint bit_tmp;

					for (step = 0; step < 8; step++)
					{
						value = 0;
						for (bit_tmp = 0; bit_tmp < 7; bit_tmp++) /* 7 bits */
						{
							if ((fnum & (1 << (int)bit_tmp)) != 0) /* only if bit "bit_tmp" is set */
							{
								offset_fnum_bit = bit_tmp * 8;
								value += lfo_pm_output[offset_fnum_bit + offset_depth, step];
							}
						}
						/* 32 steps for LFO PM (sinus) */
						lfo_pm_table[(fnum * 32 * 8) + (i * 32) + step + 0] = value;
						lfo_pm_table[(fnum * 32 * 8) + (i * 32) + (step ^ 7) + 8] = value;
						lfo_pm_table[(fnum * 32 * 8) + (i * 32) + step + 16] = -value;
						lfo_pm_table[(fnum * 32 * 8) + (i * 32) + (step ^ 7) + 24] = -value;
					}
				}
			}
		}


		/* initialize ym2612 emulator(s) */
		public void YM2612Init(double clock, int rate)
		{
			config.psg_preamp = 150;
			config.fm_preamp = 100;
			config.hq_fm = 0;
			config.psgBoostNoise = 0;
			config.filter = 0;
			config.lp_range = 50;
			config.low_freq = 880;
			config.high_freq = 5000;
			config.lg = 1;
			config.mg = 1;
			config.hg = 1;
			config.rolloff = 0.990f;
			config.dac_bits = 14;
			config.ym2413 = 2; /* AUTO */
			config.system = 0; /* AUTO */
			config.region_detect = 0; /* AUTO */
			config.vdp_mode = 0; /* AUTO */
			config.master_clock = 0; /* AUTO */
			config.force_dtack = 0;
			config.addr_error = 1;
			config.bios = 0;
			config.lock_on = 0;
			config.hot_swap = 0;
			config.xshift = 0;
			config.yshift = 0;
			config.xscale = 0;
			config.yscale = 0;
			config.aspect = 1;
			config.overscan = 3; /* FULL */
			config.ntsc = 0;
			config.vsync = 1; /* AUTO */
			config.render = 0;
			config.bilinear = 0;
			config.tv_mode = 1; /* 50hz only */
			config.gun_cursor[0] = 1;
			config.gun_cursor[1] = 1;
			config.invert_mouse = 0;
			config.autoload = 0;
			config.autocheat = 0;
			config.s_auto = 0;
			config.s_default = 1;
			config.s_device = 0;
			config.l_device = 0;
			config.bg_overlay = 0;
			config.screen_w = 658;
			config.bgm_volume = 100.0f;
			config.sfx_volume = 100.0f;
			config.hot_swap &= 1;


			//memset(&ym2612,0,sizeof(_YM2612_data));
			init_tables();
			ym2612.OPN.ST.clock = clock;
			ym2612.OPN.ST.rate = (uint)rate;
			OPNSetPres(6 * 24); /* YM2612 prescaler is fixed to 1/6, one sample (6 mixed channels) is output for each 24 FM clocks */
		}

		/* reset OPN registers */
		public void YM2612ResetChip()
		{
			int i;

			ym2612.OPN.eg_timer = 0;
			ym2612.OPN.eg_cnt = 0;

			ym2612.OPN.lfo_timer_overflow = 0;
			ym2612.OPN.lfo_timer = 0;
			ym2612.OPN.lfo_cnt = 0;
			ym2612.OPN.LFO_AM = 0;
			ym2612.OPN.LFO_PM = 0;

			ym2612.OPN.ST.TAC = 0;
			ym2612.OPN.ST.TBC = 0;

			ym2612.OPN.SL3.key_csm = 0;

			ym2612.dacen = 0;
			ym2612.dacout = 0;

			set_timers(0x30);
			ym2612.OPN.ST.TB = 0;
			ym2612.OPN.ST.TBL = 256 << (TIMER_SH + 4);
			ym2612.OPN.ST.TA = 0;
			ym2612.OPN.ST.TAL = 1024 << TIMER_SH;

			reset_channels(ym2612.CH, 6);

			for (i = 0xb6; i >= 0xb4; i--)
			{
				OPNWriteReg(i, 0xc0);
				OPNWriteReg(i | 0x100, 0xc0);
			}
			for (i = 0xb2; i >= 0x30; i--)
			{
				OPNWriteReg(i, 0);
				OPNWriteReg(i | 0x100, 0);
			}
		}

		/* ym2612 write */
		/* n = number  */
		/* a = address */
		/* v = value   */
		public void YM2612Write(uint a, uint v)
		{
			v &= 0xff;  /* adjust to 8 bit bus */

			switch (a)
			{
				case 0:  /* address port 0 */
					ym2612.OPN.ST.address = (ushort)(v);
					break;

				case 2:  /* address port 1 */
					ym2612.OPN.ST.address = (ushort)(v | 0x100);
					break;

				default:  /* data port */
					{
						int addr = ym2612.OPN.ST.address; /* verified by Nemesis on real YM2612 */
						switch (addr & 0x1f0)
						{
							case 0x20:  /* 0x20-0x2f Mode */
								switch (addr)
								{
									case 0x2a:  /* DAC data (ym2612) */
										ym2612.dacout = ((int)v - 0x80) << 6; /* level unknown (5 is too low, 8 is too loud) */
										break;
									case 0x2b:  /* DAC Sel  (ym2612) */
										/* b7 = dac enable */
										ym2612.dacen = (byte)(v & 0x80);
										break;
									default:  /* OPN section */
										/* write register */
										OPNWriteMode(addr, (int)v);
										break;
								}
								break;
							default:  /* 0x30-0xff OPN section */
								/* write register */
								OPNWriteReg(addr, (int)v);
								break;
						}
						break;
					}
			}
		}


		public uint YM2612Read()
		{
			return (uint)(ym2612.OPN.ST.status & 0xff);
		}


		/* Generate samples for ym2612 */
		public void YM2612Update(int[] buffer, int length)
		{
			int i;
			int lt, rt;

			/* refresh PG increments and EG rates if required */
			refresh_fc_eg_chan(ym2612.CH[0]);
			refresh_fc_eg_chan(ym2612.CH[1]);

			if ((ym2612.OPN.ST.mode & 0xC0) == 0)
			{
				refresh_fc_eg_chan(ym2612.CH[2]);
			}
			else
			{
				/* 3SLOT MODE (operator order is 0,1,3,2) */
				if (ym2612.CH[2].SLOT[SLOT1].Incr == -1)
				{
					refresh_fc_eg_slot(ym2612.CH[2].SLOT[SLOT1], (int)ym2612.OPN.SL3.fc[1], ym2612.OPN.SL3.kcode[1]);
					refresh_fc_eg_slot(ym2612.CH[2].SLOT[SLOT2], (int)ym2612.OPN.SL3.fc[2], ym2612.OPN.SL3.kcode[2]);
					refresh_fc_eg_slot(ym2612.CH[2].SLOT[SLOT3], (int)ym2612.OPN.SL3.fc[0], ym2612.OPN.SL3.kcode[0]);
					refresh_fc_eg_slot(ym2612.CH[2].SLOT[SLOT4], (int)ym2612.CH[2].fc, ym2612.CH[2].kcode);
				}
			}

			refresh_fc_eg_chan(ym2612.CH[3]);
			refresh_fc_eg_chan(ym2612.CH[4]);
			refresh_fc_eg_chan(ym2612.CH[5]);


			int bufferPos = 0;
			/* buffering */
			for (i = 0; i < length; i++)
			{
				/* clear outputs */
				out_fm[0].value = 0;
				out_fm[1].value = 0;
				out_fm[2].value = 0;
				out_fm[3].value = 0;
				out_fm[4].value = 0;
				out_fm[5].value = 0;

				/* update SSG-EG output */
				update_ssg_eg_channel(ym2612.CH[0].SLOT);
				update_ssg_eg_channel(ym2612.CH[1].SLOT);
				update_ssg_eg_channel(ym2612.CH[2].SLOT);
				update_ssg_eg_channel(ym2612.CH[3].SLOT);
				update_ssg_eg_channel(ym2612.CH[4].SLOT);
				update_ssg_eg_channel(ym2612.CH[5].SLOT);

				/* calculate FM */
				chan_calc(ym2612.CH[0]);
				chan_calc(ym2612.CH[1]);
				chan_calc(ym2612.CH[2]);
				chan_calc(ym2612.CH[3]);
				chan_calc(ym2612.CH[4]);
				if (ym2612.dacen == 0)
				{
					chan_calc(ym2612.CH[5]);
				}
				else
				{
					/* DAC Mode */
					out_fm[5].value = ym2612.dacout;
				}

				/* advance LFO */
				advance_lfo();

				/* advance envelope generator */
				ym2612.OPN.eg_timer += ym2612.OPN.eg_timer_add;
				while (ym2612.OPN.eg_timer >= ym2612.OPN.eg_timer_overflow)
				{
					ym2612.OPN.eg_timer -= ym2612.OPN.eg_timer_overflow;
					ym2612.OPN.eg_cnt++;
					advance_eg_channels();
				}

				/* 14-bit DAC inputs (range is -8192;+8192) */
				if (out_fm[0].value > 8192) out_fm[0].value = 8192;
				else if (out_fm[0].value < -8192) out_fm[0].value = -8192;
				if (out_fm[1].value > 8192) out_fm[1].value = 8192;
				else if (out_fm[1].value < -8192) out_fm[1].value = -8192;
				if (out_fm[2].value > 8192) out_fm[2].value = 8192;
				else if (out_fm[2].value < -8192) out_fm[2].value = -8192;
				if (out_fm[3].value > 8192) out_fm[3].value = 8192;
				else if (out_fm[3].value < -8192) out_fm[3].value = -8192;
				if (out_fm[4].value > 8192) out_fm[4].value = 8192;
				else if (out_fm[4].value < -8192) out_fm[4].value = -8192;
				if (out_fm[5].value > 8192) out_fm[5].value = 8192;
				else if (out_fm[5].value < -8192) out_fm[5].value = -8192;

				/* 6-channels stereo mixing  */
				lt = (int)((out_fm[0].value) & ym2612.OPN.pan[0]);
				rt = (int)((out_fm[0].value) & ym2612.OPN.pan[1]);
				lt += (int)((out_fm[1].value) & ym2612.OPN.pan[2]);
				rt += (int)((out_fm[1].value) & ym2612.OPN.pan[3]);
				lt += (int)((out_fm[2].value) & ym2612.OPN.pan[4]);
				rt += (int)((out_fm[2].value) & ym2612.OPN.pan[5]);
				lt += (int)((out_fm[3].value) & ym2612.OPN.pan[6]);
				rt += (int)((out_fm[3].value) & ym2612.OPN.pan[7]);
				lt += (int)((out_fm[4].value) & ym2612.OPN.pan[8]);
				rt += (int)((out_fm[4].value) & ym2612.OPN.pan[9]);
				lt += (int)((out_fm[5].value) & ym2612.OPN.pan[10]);
				rt += (int)((out_fm[5].value) & ym2612.OPN.pan[11]);

				/* buffering */
				buffer[bufferPos] = lt;
				bufferPos += 1;
				buffer[bufferPos] = rt;
				bufferPos += 1;

				/* CSM mode: if CSM Key ON has occured, CSM Key OFF need to be sent       */
				/* only if Timer A does not overflow again (i.e CSM Key ON not set again) */
				ym2612.OPN.SL3.key_csm <<= 1;

				/* timer A control */
				INTERNAL_TIMER_A();

				/* CSM Mode Key ON still disabled */
				if ((ym2612.OPN.SL3.key_csm & 2) != 0)
				{
					/* CSM Mode Key OFF (verified by Nemesis on real hardware) */
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT1);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT2);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT3);
					FM_KEYOFF_CSM(ym2612.CH[2], SLOT4);
					ym2612.OPN.SL3.key_csm = 0;
				}
			}

			/* timer B control */
			INTERNAL_TIMER_B(length);
		}
	}
}
