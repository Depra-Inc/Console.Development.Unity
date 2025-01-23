using UnityEngine;

namespace Depra.Console.Development.Samples {
	[RequireComponent(typeof(ITextOutputPort))]
	[RequireComponent(typeof(IDevelopmentConsoleInterface))]
	internal sealed class DevelopmentConsoleShowcase : MonoBehaviour {
		private DevelopmentConsole _console;

		private void Start() {
			_console = new DevelopmentConsole(
				GetComponent<ITextOutputPort>(),
				GetComponent<IDevelopmentConsoleInterface>());

			_console.AddCommands(new IDevelopmentCommand[] {
				new QuitCommand(),
				new CloseCommand(),
				new DestroyCommand(),
				new PrimitiveCommand()
			});
		}

		private void OnDestroy() => _console?.Dispose();
	}
}