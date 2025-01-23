// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Depra.Console.Development
{
	public sealed class DevelopmentConsole : IDevelopmentConsole, IDisposable
	{
		private readonly IDevelopmentCommands _commands;
		private readonly IDevelopmentConsoleInput _inputPort;
		private readonly IDevelopmentConsoleOutput _outputPort;
		private readonly DevelopmentConsoleHistory _history = new();

		public DevelopmentConsole(
			IDevelopmentCommands commands, 
			IDevelopmentConsoleInput inputPort,
			IDevelopmentConsoleOutput outputPort)
		{
			_commands = commands;
			_inputPort = inputPort;
			_outputPort = outputPort;
			_inputPort.StateChanged += OnStateChanged;
		}

		public void Dispose()
		{
			if (_inputPort != null)
			{
				_inputPort.StateChanged -= OnStateChanged;
			}
		}

		private string ProcessInput(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return input;
			}

			input = input.Trim();
			_history.Add(input);

			var parts = input.Trim().ToLowerInvariant().Split(' ');
			if (parts.Length <= 0)
			{
				return string.Empty;
			}

			var id = parts[0];
			IDevelopmentCommand command = null;
			for (var index = 0; index < _commands.Count && command == null; ++index)
			{
				if (id.Equals(_commands[index].Alias))
				{
					command = _commands[index];
				}
			}

			if (command != null)
			{
				if (!command.Execute(parts.SubArray(1, parts.Length - 1)))
				{
					Debug.LogWarning($"Error executing command '{id}'. Usage: {command.Usage}");
				}
			}
			else
			{
				Debug.LogWarning($"Invalid command '{id}'");
			}

			return string.Empty;
		}

		private void OnStateChanged(ConsoleAction action) => _outputPort.Value = ProcessAction(action);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private string ProcessAction(ConsoleAction action) => action switch
		{
			ConsoleAction.EXECUTE_COMMAND => ProcessInput(_outputPort.Value),
			ConsoleAction.NEXT_COMMAND_IN_HISTORY => _history.Next(),
			ConsoleAction.PREVIOUS_COMMAND_IN_HISTORY => _history.Previous(),
			ConsoleAction.NONE => string.Empty,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
		};
	}
}