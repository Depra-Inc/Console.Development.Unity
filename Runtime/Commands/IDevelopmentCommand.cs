// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

namespace Depra.Console.Development
{
	public interface IDevelopmentCommand
	{
		string Alias { get; set; }
		string Usage { get; set; }
		string Description { get; set; }

		bool Execute(string[] args);
	}
}