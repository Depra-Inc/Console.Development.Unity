// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Depra.Console.Development
{
	internal static class StringExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 ToVector3(this string self)
		{
			var result = Vector3.zero;
			var args = self.Replace("f", string.Empty).Split(',');
			if (args.Length == 3)
			{
				float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out result.x);
				float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out result.y);
				float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out result.z);
			}

			return result;
		}
	}
}