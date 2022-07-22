using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace TRNGTool
{
	/// <summary>
	///     Creates array of T-s from array of bytes
	/// </summary>
	internal interface DataConstructor<T>
	{
		void ConstructElements(byte[] from, int index_from, T[] to, int index_to, int number_elements);
	}

	public struct UInt8Data : DataConstructor<byte>
	{
		public void ConstructElements(byte[] from, int index_from, byte[] to, int index_to, int number_elements)
		{
			Array.Copy(from, index_from, to, index_to, number_elements);
		}
	}

	public struct UInt16Data : DataConstructor<UInt16>
	{
		public void ConstructElements(byte[] from, int index_from, UInt16[] to, int index_to, int number_elements)
		{
			for (int i = 0; i < number_elements; i++)
			{
				to[index_to + i] = BitConverter.ToUInt16(from, index_from);
				index_from += sizeof(UInt16);
			}
		}
	}

	public struct UInt32Data : DataConstructor<UInt32>
	{
		public void ConstructElements(byte[] from, int index_from, UInt32[] to, int index_to, int number_elements)
		{
			for (int i = 0; i < number_elements; i++)
			{
				to[index_to + i] = BitConverter.ToUInt32(from, index_from);
				index_from += sizeof(UInt32);
			}
		}
	}

	/// <summary>
	///     Buffer for storing random data from files
	/// </summary>
	internal class DataPool<T, F> where F : DataConstructor<T>
	{
		static readonly F s_dataC = default;

		// Main buffer
		LinkedList<T[]> _randomDataPool = new();

		// Current file in main buffer
		LinkedList<T[]>.Enumerator _currentFile = new();

		// Current index in the currentFile
		int _fileCurrentIndex = 0;

		// Total size of a buffer
		long _overallSize = 0;

		// True when the current file in the buffer is the last file
		bool _lastFile = false;

		// True if there is only one available element left in the buffer
		public bool ReachedEnd
		{
			get => _lastFile && _fileCurrentIndex == _randomDataPool.Last().Length - 1;
		}

		// Amount of data used, from 0.0 to 1.0
		public double Usage
		{
			get
			{
				// determine how many of all elements are used
				long used = 0;
				foreach (var f in _randomDataPool)
				{
					if (!object.ReferenceEquals(f, _currentFile.Current))
					{
						used += f.Length;
					}
					else
					{
						used += _fileCurrentIndex;
						break;
					}
				}

				return (double)used / _overallSize;
			}
		}

		public DataPool(string DirectoryPath, int NumFilesToRead, string SearchPattern)
			=> FillDataPool(DirectoryPath, NumFilesToRead, SearchPattern);

		/// <summary>
		///     Gets next int from the random data pool
		/// </summary>
		public T NextInt()
		{
			var curFile = _currentFile.Current;

			if (_fileCurrentIndex >= curFile.Length)
			{
				if (!_currentFile.MoveNext())
					throw new Exception("Insufficient random data");
				else
				{
					curFile = _currentFile.Current;
					_fileCurrentIndex = 0;
					_lastFile = object.ReferenceEquals(curFile, _randomDataPool.Last.Value);
				}
			}

			return curFile[_fileCurrentIndex++];
		}

		/// <summary>
		///     Adds file content to the main buffer
		/// </summary>
		void AddFile(string FilePath)
		{
			if (!File.Exists(FilePath))
				throw new Exception("File does not exist");

			// Open file and add its contents to the buffer
			using var fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
			using var binaryStream = new BinaryReader(fileStream);

			var length = binaryStream.BaseStream.Length;

			int T_bytes = Marshal.SizeOf(typeof(T));
			var T_size = (int)(length / T_bytes);

			byte[] file_data = binaryStream.ReadBytes((int)length);
			if (file_data.Length != length)
				throw new Exception($"Unable to read all file (only {file_data.Length} of {length} bytes read): {FilePath}");

			var buffer = new T[T_size];
			s_dataC.ConstructElements(file_data, 0, buffer, 0, T_size);
			_randomDataPool.AddLast(buffer);

			_overallSize += T_size;
		}

		/// <summary>
		///     Reads NumFilesToRead files matching SearchPattern from a specified directory to the buffer
		///     NumFilesToRead == -1 means read all available files
		/// </summary>
		public void FillDataPool(string DirectoryPath, int NumFilesToRead, string SearchPattern)
		{
			var di = new DirectoryInfo(DirectoryPath);
			var files = di.GetFiles(SearchPattern);
			int files_n = NumFilesToRead == (int)RandomFilesToRead.AllFiles ? int.MaxValue : NumFilesToRead;

			var to_process = (from f in di.EnumerateFiles() select f).Take(files_n);

			// 256 -- approximate CLR overhead for storing array
			const int max_file_size = int.MaxValue - 256;

			int display_counter = 0;

			foreach (var file in to_process)
			{
				if (file.Length == 0)
					throw new Exception($"Empty data file: {file}");
				else if (file.Length > max_file_size)
					throw new Exception($"File is too big ({file}). Files bigger than {max_file_size} bytes are not supported");

				Console.WriteLine(String.Format("Reading file {0} of {1}: {2}", ++display_counter, files_n, file.Name));
				AddFile($@"{file.DirectoryName}/{file.Name}");
			}

			Console.WriteLine("All files read");

			// Set starting indexes, set flags

			_currentFile = _randomDataPool.GetEnumerator();
			// set to the first element
			if (!_currentFile.MoveNext())
				throw new Exception("No such files");

			_fileCurrentIndex = 0;
			_lastFile = _randomDataPool.Count == 1;
		}
	}
}