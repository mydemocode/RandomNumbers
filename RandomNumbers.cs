using System;
using System.Reflection;

namespace TRNGTool
{
	static public class RandomShuffler
	{
		public static void ShuffleRandomly<E, T>(E[] data, RandomNumbers<T> rg)
		{
			for (int i = data.Length; i > 1; --i)
			{
				uint r = rg.GetInt(0, (uint)i);
				E t = data[r];
				data[r] = data[i - 1];
				data[i - 1] = t;
			}
		}

		public static void ShufflePseudoRandomly<E>(E[] data, Random rg)
		{
			for (int i = data.Length; i > 1; --i)
			{
				int r = rg.Next(0, i);
				E t = data[r];
				data[r] = data[i - 1];
				data[i - 1] = t;
			}
		}
	}

	// Value to indicate that all files from a random data directory should be used
	public enum RandomFilesToRead { AllFiles = -1 };

	/// <summary>
	///     Class providing random numbers in a required range. External binary files as a data source of random numbers are needed.
	///     It's supposed that you're using files containing true random numbers obtained from TRNG services such as www.random.org
	/// </summary>
	public class RandomNumbers<T> : IRandomNumbers<T>
	{
		DataPool<byte, UInt8Data> _dataPoolUInt8 = null;
		DataPool<UInt16, UInt16Data> _dataPoolUInt16 = null;
		DataPool<UInt32, UInt32Data> _dataPoolUInt32 = null;
		object _dataPoolObj;

		PropertyInfo __isReachedEnd;
		PropertyInfo __usage;

		delegate uint GetIntFunc();
		delegate uint GetBoundedIntFunc(uint range);
		GetIntFunc RandFunction;
		GetBoundedIntFunc BoundedRandFunction;

		uint RandUInt8() => _dataPoolUInt8.NextInt();
		uint RandUInt16() => _dataPoolUInt16.NextInt();
		uint RandUInt32() => _dataPoolUInt32.NextInt();

		public bool ReachedEnd { get => (bool)__isReachedEnd.GetValue(_dataPoolObj, null); }
		public double Usage { get => (double)__usage.GetValue(_dataPoolObj, null); }

		/// <summary>
		///     Creates buffer with random data from files
		/// </summary>
		/// <param name="Path">Directory containing binary random data files</param>
		/// <param name="SearchPattern">Mask for the files to read</param>
		/// <param name="NumFilesToRead">Number of files to read or FilesNumber.AllFiles</param>
		public RandomNumbers(string Path, int NumFilesToRead = (int)RandomFilesToRead.AllFiles, string SearchPattern = "*.bin")
		{
			if (typeof(T) == typeof(byte))
			{
				_dataPoolObj = _dataPoolUInt8 = new(Path, NumFilesToRead, SearchPattern);
				__isReachedEnd = _dataPoolUInt8.GetType().GetProperty(nameof(ReachedEnd));
				__usage = _dataPoolUInt8.GetType().GetProperty(nameof(Usage));
				RandFunction = RandUInt8;
				BoundedRandFunction = BoundedRandUInt8;
			}
			else if (typeof(T) == typeof(UInt16))
			{
				_dataPoolObj = _dataPoolUInt16 = new(Path, NumFilesToRead, SearchPattern);
				__isReachedEnd = _dataPoolUInt16.GetType().GetProperty(nameof(ReachedEnd));
				__usage = _dataPoolUInt16.GetType().GetProperty(nameof(Usage));
				RandFunction = RandUInt16;
				BoundedRandFunction = BoundedRandUInt16;
			}
			else if (typeof(T) == typeof(UInt32))
			{
				_dataPoolObj = _dataPoolUInt32 = new(Path, NumFilesToRead, SearchPattern);
				__isReachedEnd = _dataPoolUInt32.GetType().GetProperty(nameof(ReachedEnd));
				__usage = _dataPoolUInt32.GetType().GetProperty(nameof(Usage));
				RandFunction = RandUInt32;
				BoundedRandFunction = BoundedRandUInt32;
			}
			else
			{
				throw new Exception(String.Format("{0} buffer type is not supported yet", typeof(T).Name));
			}

			if (__isReachedEnd is null || __usage is null)
				throw new Exception("Check properties' names");
		}

		/// <returns>Random number of type T</returns>
		public uint GetInt()
		{
			var r = RandFunction();
			if (ReachedEnd)
				// load more data
				throw new NotImplementedException();

			return r;
		}

		/// <returns>Random number within a [0..r) range</returns>		
		public uint GetInt(uint min, uint max)
		{
			uint range = max - min;

			return min + BoundedRandFunction(range);
		}

		/// <remarks>
		///     Daniel Lemire's debiased integer multiplication algorithm modified by Melissa O'Neill:
		///     <href>https://www.pcg-random.org/posts/bounded-rands.html</href>
		///     <href>https://github.com/imneme/bounded-rands</href>
		/// </remarks>
		uint BoundedRandUInt32(uint r)
		{
			UInt32 range = (UInt32)r;
			UInt32 x = GetInt();
			UInt64 m = (UInt64)x * (UInt64)range;
			UInt32 l = (UInt32)m;

			if (l < range)
			{
				UInt32 t = (UInt32)(-range);

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = GetInt();
					m = (UInt64)x * (UInt64)range;
					l = (UInt32)m;
				}
			}

			return (UInt32)(m >> 32);
		}

		uint BoundedRandUInt16(uint r)
		{
			UInt16 range = (UInt16)r;
			UInt16 x = (UInt16)GetInt();
			UInt32 m = (UInt32)x * (UInt32)range;
			UInt16 l = (UInt16)m;

			if (l < range)
			{
				UInt16 t = (UInt16)(-range);

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = (UInt16)GetInt();
					m = (UInt32)x * (UInt32)range;
					l = (UInt16)m;
				}
			}

			return (UInt16)(m >> 16);
		}

		uint BoundedRandUInt8(uint r)
		{
			byte range = (byte)r;
			byte x = (byte)GetInt();
			UInt16 m = (UInt16)((UInt16)x * (UInt16)range);
			byte l = (byte)m;

			if (l < range)
			{
				byte t = (byte)-range;

				if (t >= range)
				{
					t -= range;

					if (t >= range)
						t %= range;
				}

				while (l < t)
				{
					x = (byte)GetInt();
					m = (UInt16)((UInt16)x * (UInt16)range);
					l = (byte)m;
				}
			}

			return (byte)(m >> 8);
		}
	}
}