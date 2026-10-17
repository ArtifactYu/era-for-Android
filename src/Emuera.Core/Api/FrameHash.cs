// andEmuera: 画面バッファの内容が前回と同じかを判定するためのハッシュ。
//
// 世代番号は「操作があった」だけで進むので、内容が同一でも PNG の再エンコードと
// 再転送が走る。SETANIMETIMER によるアニメ用タイマーは中身が止まっていても
// 25ms 周期で回り続けるため、これが効く場面は実際にある。
//
// 5.5 メガピクセル (22MB) のハッシュはメモリ帯域律速で数 ms、
// 同じ画像の PNG エンコードは 2 桁 ms なので、割に合う。

using System;
using System.Runtime.InteropServices;

namespace MinorShift.Emuera.Api
{
	/// <summary>
	/// ピクセルバッファの内容ハッシュ。暗号用途ではなく「前回と同じか」の判定にだけ使う。
	/// 4 レーンに分けて命令レベル並列を稼ぐ xor-multiply。
	/// </summary>
	internal static class FrameHash
	{
		const ulong Prime = 0x9E3779B97F4A7C15UL;

		public static ulong Compute(ReadOnlySpan<byte> bytes)
		{
			if (bytes.IsEmpty)
				return 0;

			var words = MemoryMarshal.Cast<byte, ulong>(bytes);
			ulong a = Prime, b = Prime ^ 1, c = Prime ^ 2, d = Prime ^ 3;

			int i = 0;
			for (; i + 4 <= words.Length; i += 4)
			{
				a = (a ^ words[i]) * Prime;
				b = (b ^ words[i + 1]) * Prime;
				c = (c ^ words[i + 2]) * Prime;
				d = (d ^ words[i + 3]) * Prime;
			}
			for (; i < words.Length; i++)
				a = (a ^ words[i]) * Prime;

			// 8 の倍数に満たない端数 (実際には Bgra8888 なので出ないが、念のため)
			for (int t = words.Length * sizeof(ulong); t < bytes.Length; t++)
				b = (b ^ bytes[t]) * Prime;

			ulong h = a ^ RotateLeft(b, 17) ^ RotateLeft(c, 31) ^ RotateLeft(d, 47);
			h ^= h >> 33;
			h *= Prime;
			h ^= h >> 29;
			return h ^ (ulong)bytes.Length;
		}

		static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
	}
}
