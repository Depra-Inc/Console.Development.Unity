using UnityEngine;
using UnityEngine.InputSystem;

namespace Depra.Console.Development.InputSystem
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(IDevelopmentConsoleView))]
	[AddComponentMenu(Module.MENU_PATH + nameof(DevelopmentConsoleActionMapsToggle))]
	internal sealed class DevelopmentConsoleActionMapsToggle : MonoBehaviour
	{
		[SerializeField] private InputActionAsset _actionAsset;
		[SerializeField] private string[] _actionMapsToDisable;

		private IDevelopmentConsoleView _view;

		private void OnEnable()
		{
			_view ??= GetComponent<IDevelopmentConsoleView>();
			_view.Opened += OnOpened;
			_view.Closed += OnClosed;
		}

		private void OnDisable()
		{
			if (_view == null)
			{
				return;
			}

			_view.Opened -= OnOpened;
			_view.Closed -= OnClosed;
		}

		private void OnClosed()
		{
			if (_actionMapsToDisable.Length == 0)
			{
				return;
			}

			foreach (var actionMapId in _actionMapsToDisable)
			{
				_actionAsset.FindActionMap(actionMapId)?.Enable();
			}
		}

		private void OnOpened()
		{
			if (_actionMapsToDisable.Length == 0)
			{
				return;
			}

			foreach (var actionMapId in _actionMapsToDisable)
			{
				_actionAsset.FindActionMap(actionMapId)?.Disable();
			}
		}
	}
}