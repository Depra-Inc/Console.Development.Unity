// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Depra.Console.Development
{
	public interface IDevelopmentCommands
	{
		int Count { get; }
		IDevelopmentCommand this[int index] { get; }

		bool Contains(IDevelopmentCommand command);
		void Add(IDevelopmentCommand command);
		void Remove(IDevelopmentCommand command);
	}

	public static class DevelopmentCommandsExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AddCommands(this IDevelopmentCommands self, IEnumerable<IDevelopmentCommand> commands)
		{
			foreach (var command in commands)
			{
				self.Add(command);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void RemoveCommands(this IDevelopmentCommands self, IEnumerable<IDevelopmentCommand> commands)
		{
			foreach (var command in commands)
			{
				self.Remove(command);
			}
		}
	}
}