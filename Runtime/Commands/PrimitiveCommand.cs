// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using UnityEngine;

namespace Depra.Console.Development
{
	[System.Serializable]
	public sealed class PrimitiveCommand : IDevelopmentCommand
	{
		[field: SerializeField] public string Alias { get; set; } = "primitive";
		[field: SerializeField] public string Usage { get; set; } = "primitive cube|sphere|plane|cylinder|capsule [0.0,0.0,0.0]";
		[field: SerializeField] public string Description { get; set; } = "Create primitive objects.";

		bool IDevelopmentCommand.Execute(string[] args)
		{
			if (args.Length <= 0)
			{
				return false;
			}

			var gameObject = args[0] switch
			{
				"cube" => GameObject.CreatePrimitive(PrimitiveType.Cube),
				"sphere" => GameObject.CreatePrimitive(PrimitiveType.Sphere),
				"plane" => GameObject.CreatePrimitive(PrimitiveType.Plane),
				"cylinder" => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
				"capsule" => GameObject.CreatePrimitive(PrimitiveType.Capsule),
				_ => null
			};

			if (args.Length != 2 || gameObject == null)
			{
				return gameObject != null;
			}

			var components = args[1].Split(',');
			if (components.Length != 3)
			{
				return gameObject != null;
			}

			gameObject.transform.position = args[1].ToVector3();
			return true;
		}
	}
}