// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Depra.Console.Development
{
	[Serializable]
	public sealed class GameObjectCommand : IDevelopmentCommand
	{
		[field: SerializeField] public string Alias { get; set; } = "gameobject";
		[field: SerializeField] public string Usage { get; set; } = "gameobject 'gameobject-name' destroy|activate|deactivate|move|rotate [0.0,0.0,0.0]";
		[field: SerializeField] public string Description { get; set; } = "Operations on GameObjects.";

		bool IDevelopmentCommand.Execute(string[] args)
		{
			if (args.Length <= 1)
			{
				return false;
			}

			var name = args[0];
			var command = args[1];

			GameObject gameObject = null;
			// @HACK: Active and inactive GameObjects.
			var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
			for (var index = 0; index < gameObjects.Length && gameObject == null; ++index)
			{
				if (name.Equals(gameObjects[index].name.ToLower()))
				{
					gameObject = gameObjects[index];
				}
			}

			if (gameObject != null)
			{
				switch (command)
				{
					case "destroy":
#if UNITY_EDITOR
						Object.DestroyImmediate(gameObject, true);
#else
						Object.Destroy(gameObject);
#endif
						return true;

					case "activate":
						gameObject.SetActive(true);
						return true;

					case "deactivate":
						gameObject.SetActive(false);
						return true;

					case "move":
						if (args.Length == 3)
						{
							gameObject.transform.position = args[2].ToVector3();

							return true;
						}

						break;

					case "rotate":
						if (args.Length == 3)
						{
							var euler = args[2].ToVector3();
							gameObject.transform.Rotate(Vector3.right, euler.x);
							gameObject.transform.Rotate(Vector3.up, euler.y);
							gameObject.transform.Rotate(Vector3.forward, euler.z);

							return true;
						}

						break;

					default:
						Debug.LogWarning($"'{command}' unrecognized.");
						break;
				}
			}
			else
			{
				Debug.LogWarning($"GameObject '{name}' not found.");
			}

			return false;
		}
	}
}