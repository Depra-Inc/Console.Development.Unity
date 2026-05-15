// SPDX-License-Identifier: Apache-2.0
// © 2025-2026 Depra <n.melnikov@depra.org>

using UnityEngine;
using static Depra.Console.Development.Module;

namespace Depra.Console.Development.IMGUI
{
	[CreateAssetMenu(menuName = MENU_PATH + "IMGUI Theme", fileName = "New Console Theme", order = DEFAULT_ORDER)]
	public sealed class IMGUIDevelopmentConsoleTheme : ScriptableObject
	{
		[SerializeField] private Font _font;
		[Min(0)] [SerializeField] private int _fontSize = 16;
		[SerializeField] private float _padding = 10f;
		[SerializeField] private string _promptSymbol = "]";
		[Min(0)] [SerializeField] private float _inputHeight = 40f;

		[SerializeField] private Color _backgroundColor = new(0f, 0f, 0f, 0.85f);
		[SerializeField] private Color _logColor = new(0f, 1f, 0f, 1f);
		[SerializeField] private Color _commandColor = new(0f, 1f, 0.5f, 1f);
		[SerializeField] private Color _inputColor = new(1f, 1f, 0f, 1f);
		[SerializeField] private Color _inputFocusedColor = new(0, 0, 0, 0.4f);
		[SerializeField] private Color _inputBackgroundColor = new(0, 0, 0, 0.3f);
		[SerializeField] private Color _promptColor = new(1f, 0.5f, 0f, 1f);
		[SerializeField] private Color _errorColor = new(1f, 0f, 0f, 1f);
		[SerializeField] private Color _warningColor = new(1f, 1f, 0f, 1f);
		[SerializeField] private Color _separatorColor = new(0f, 1f, 0f, 0.5f);

		[TextArea(3, 10)] [SerializeField] private string _welcomeText;

		public Font Font => _font;
		public int FontSize => _fontSize;
		public float Padding => _padding;
		public string PromptSymbol => _promptSymbol;
		public float InputHeight => _inputHeight;

		public Color BackgroundColor => _backgroundColor;
		public Color LogColor => _logColor;
		public Color CommandColor => _commandColor;
		public Color InputColor => _inputColor;
		public Color InputBackgroundColor => _inputBackgroundColor;
		public Color InputFocusedColor => _inputFocusedColor;
		public Color PromptColor => _promptColor;
		public Color ErrorColor => _errorColor;
		public Color WarningColor => _warningColor;
		public Color SeparatorColor => _separatorColor;

		public string WelcomeText => _welcomeText;
	}
}