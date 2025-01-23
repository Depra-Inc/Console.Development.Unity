// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

namespace Depra.Console.Development
{
	public interface IDevelopmentConsoleOutput
	{
		bool Show { get; set; }
		string Value { get; set; }

		void Clear();
	}
}