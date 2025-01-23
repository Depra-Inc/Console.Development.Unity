// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using UnityEngine;

namespace Depra.Console.Development
{
	[System.Serializable]
	public sealed class QuitCommand : IDevelopmentCommand
	{
		[field: SerializeField] public string Alias { get; set; } = "quit";
		[field: SerializeField] public string Usage { get; set; } = "quit";
		[field: SerializeField] public string Description { get; set; } = "Quit application.";

		bool IDevelopmentCommand.Execute(string[] args)
		{
			if (Application.isEditor)
			{
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#endif
			}
			else
			{
				Application.Quit();
			}

			return true;
		}
	}
}