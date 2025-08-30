using System;

namespace Depra.Console.Development
{
	public interface IDevelopmentConsoleView
	{
		event Action Opened;
		event Action Closed;
	}
}