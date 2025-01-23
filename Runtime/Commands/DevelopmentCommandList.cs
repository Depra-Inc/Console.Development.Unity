// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Collections.Generic;

namespace Depra.Console.Development
{
	public sealed class DevelopmentCommandList : IDevelopmentCommands
	{
		private readonly List<IDevelopmentCommand> _commands = new();

		public DevelopmentCommandList Add(IDevelopmentCommand command)
		{
			_commands.Add(command);
			return this;
		}

		int IDevelopmentCommands.Count => _commands.Count;
		IDevelopmentCommand IDevelopmentCommands.this[int index] => _commands[index];

		bool IDevelopmentCommands.Contains(IDevelopmentCommand command) => _commands.Contains(command);
		void IDevelopmentCommands.Add(IDevelopmentCommand command) => _commands.Add(command);
		void IDevelopmentCommands.Remove(IDevelopmentCommand command) => _commands.Remove(command);
	}
}