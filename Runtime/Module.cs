// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Depra.Console.Development.IMGUI")]

namespace Depra.Console.Development
{
	internal static class Module
	{
		public const string MENU_PATH = nameof(Console) + "/" + nameof(Development) + "/";
	}
}