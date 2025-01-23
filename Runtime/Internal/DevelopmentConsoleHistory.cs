// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Collections.Generic;

namespace Depra.Console.Development
{
	internal sealed class DevelopmentConsoleHistory
	{
		private readonly List<string> _history = new();
		private int _historyPointer;

		public void Add(string command)
		{
			_history.Add(command);
			_historyPointer = 0;
		}

		public string Next()
		{
			if (_history.Count <= 0 || _historyPointer <= 0)
			{
				return string.Empty;
			}

			_historyPointer--;
			return _history[_historyPointer];
		}

		public string Previous()
		{
			if (_historyPointer >= _history.Count)
			{
				return string.Empty;
			}

			var command = _history[_historyPointer];
			_historyPointer++;
			return command;
		}
	}
}