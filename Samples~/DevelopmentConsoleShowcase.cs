using UnityEngine;

namespace Depra.Console.Development.Samples
{
	[RequireComponent(typeof(IDevelopmentConsoleInput))]
	[RequireComponent(typeof(IDevelopmentConsoleOutput))]
	internal sealed class DevelopmentConsoleShowcase : MonoBehaviour
	{
		private DevelopmentConsole _console;

		private void Start() => _console = new DevelopmentConsole(
			inputPort: GetComponent<IDevelopmentConsoleInput>(),
			outputPort: GetComponent<IDevelopmentConsoleOutput>(),
			commands: new DevelopmentCommandList()
				.Add(new QuitCommand())
				.Add(new CloseCommand())
				.Add(new CloseCommand())
				.Add(new DestroyCommand())
				.Add(new PrimitiveCommand()));

		private void OnDestroy() => _console?.Dispose();
	}
}