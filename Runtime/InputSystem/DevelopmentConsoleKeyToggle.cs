using UnityEngine;
using UnityEngine.InputSystem;

namespace Depra.Console.Development.InputSystem
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(IDevelopmentConsoleView))]
	[AddComponentMenu(Module.MENU_PATH + nameof(DevelopmentConsoleKeyToggle))]
	internal sealed class DevelopmentConsoleKeyToggle : MonoBehaviour
	{
		[SerializeField] private Key _key = Key.Backquote;

		private IDevelopmentConsoleView _view;

		private void Awake() => _view = GetComponent<IDevelopmentConsoleView>();

		private void Update()
		{
			var keyboard = Keyboard.current;
			if (keyboard != null && keyboard[_key].wasReleasedThisFrame)
			{
				_view.Show = !_view.Show;
			}
		}
	}
}
