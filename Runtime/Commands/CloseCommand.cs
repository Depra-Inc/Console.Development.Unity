// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Linq;
using UnityEngine;

namespace Depra.Console.Development
{
	[System.Serializable]
	public sealed class CloseCommand : IDevelopmentCommand
	{
		[field: SerializeField] public string Alias { get; set; } = "close";
		[field: SerializeField] public string Usage { get; set; } = "close";
		[field: SerializeField] public string Description { get; set; } = "Close the development console.";

		private IDevelopmentConsoleOutput _console;
		private IDevelopmentConsoleOutput Console => _console ??=
			(IDevelopmentConsoleOutput)Object
				.FindObjectsOfType(typeof(Object))
				.FirstOrDefault(x => x.GetType()
					.IsAssignableFrom(typeof(IDevelopmentConsoleOutput)));

		bool IDevelopmentCommand.Execute(string[] args)
		{
			if (Console != null)
			{
				Console.Show = false;
			}

			return Console != null;
		}
	}
}