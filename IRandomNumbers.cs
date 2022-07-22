namespace TRNGTool
{
	public interface IRandomNumbers<T>
	{
		// Returns next random number
		public uint GetInt();

		// Returns a random number within a [min, max) range
		public uint GetInt(uint min, uint max);
	}
}