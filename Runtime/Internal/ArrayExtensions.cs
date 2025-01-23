// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Runtime.CompilerServices;

namespace Depra.Console.Development
{
	internal static class ArrayExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] SubArray<T>(this T[] self, int offset, int length) =>
			new ArraySegment<T>(self, offset, length).ToArray();
	}
}