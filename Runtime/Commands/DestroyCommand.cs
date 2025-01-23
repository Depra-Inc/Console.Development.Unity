// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Depra.Console.Development
{
	[System.Serializable]
	public sealed class DestroyCommand : IDevelopmentCommand
	{
		[field: SerializeField] public string Alias { get; set; } = "destroy";
		[field: SerializeField] public string Usage { get; set; } = "destroy 'gameobject-name'";
		[field: SerializeField] public string Description { get; set; } = "Destroy GameObject.";

		bool IDevelopmentCommand.Execute(string[] args)
		{
			if (args.Length != 1)
			{
				return false;
			}

			var gameObjectName = args[0];
			// @HACK: Active and inactive GameObjects.
			var gameObjectsToDestroy = Resources
				.FindObjectsOfTypeAll<GameObject>()
				.Where(gameObject => gameObjectName.Equals(gameObject.name.ToLower())).ToList();

			for (var index = gameObjectsToDestroy.Count - 1; index >= 0; index--)
			{
				Object.Destroy(gameObjectsToDestroy[index]);
			}

			return true;
		}
	}
}