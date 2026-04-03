using System;
using System.Text;

namespace StardewValley.Hashing;

/// <inheritdoc cref="T:StardewValley.Hashing.IHashUtility" />
public class HashUtility : IHashUtility
{
	/// <inheritdoc />
	public int GetDeterministicHashCode(string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		return GetDeterministicHashCode(bytes);
	}

	/// <inheritdoc />
	public int GetDeterministicHashCode(params int[] values)
	{
		byte[] array = new byte[values.Length * 4];
		Buffer.BlockCopy(values, 0, array, 0, array.Length);
		return GetDeterministicHashCode(array);
	}

	/// <summary>Get a deterministic hash code for a byte data array.</summary>
	/// <param name="data">The data to hash.</param>
	public int GetDeterministicHashCode(byte[] data)
	{
		const uint fnvOffset = 2166136261;
		const uint fnvPrime = 16777619;
		uint hash = fnvOffset;
		for (int i = 0; i < data.Length; i++)
		{
			hash ^= data[i];
			hash *= fnvPrime;
		}
		return unchecked((int)hash);
	}
}
