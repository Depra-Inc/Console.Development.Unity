// SPDX-License-Identifier: Apache-2.0
// © 2025-2026 Depra <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static Depra.Console.Development.Module;

namespace Depra.Console.Development.IMGUI
{
	[AddComponentMenu(MENU_PATH + "IMGUI Development Console")]
	internal sealed class IMGUIDevelopmentConsole : MonoBehaviour,
		IDevelopmentConsoleView,
		IDevelopmentConsoleInput,
		IDevelopmentConsoleOutput
	{
		[SerializeField] private KeyCode[] _showKeys = { KeyCode.Backslash };
		[SerializeField] private KeyCode _toggleExpandKey = KeyCode.Tab;
		[Min(0)] [SerializeField] private int _maxHistorySize = 50;
		[Min(0)] [SerializeField] private int _maxLogEntries = 100;
		[Min(0)] [SerializeField] private float _acceptNewCommandTime = 0.1f;
		[SerializeField] private bool _lazyStylesInitialization;
		[SerializeField] private IMGUIDevelopmentConsoleTheme _theme;

		private const string INPUT_CONTROL_NAME = "Console_TextInput";
		private const string PREF_KEY_EXPANDED = "Console_IsExpanded";

		private bool _isVisible;
		private bool _isAnimating;
		private float _animationProgress;
		private float _lastInputTime = -1f;

		private bool _isExpanded = true;
		private bool _isExpandAnimating;
		private float _expandAnimationProgress = 1f;

		private GUIStyle _logStyle;
		private GUIStyle _inputStyle;
		private GUIStyle _promptStyle;
		private Texture2D _backgroundTexture;
		private Matrix4x4 _originalGUIMatrix;
		private Texture2D _inputBackgroundTexture;
		private Texture2D _inputFocusedTexture;
		private Texture2D _separatorTexture;
		private float _scaledInputHeight;
		private bool _stylesInitialized;

		private int _historyIndex = -1;
		private string _currentInput = string.Empty;
		private List<LogEntry> _logEntries;
		private List<string> _commandHistory;
		private Vector2 _logScrollPosition;
		private CursorLockMode _previousCursorState;

		public event Action Opened;
		public event Action Closed;

		private event Action<ConsoleAction> StateChanged;

		event Action<ConsoleAction> IDevelopmentConsoleInput.StateChanged
		{
			add => StateChanged += value;
			remove => StateChanged -= value;
		}

		private void Start()
		{
			if (_theme == null)
			{
				Debug.LogError("[IMGUIDevelopmentConsole] Theme is not assigned!");
				enabled = false;
				return;
			}

			Value = string.Empty;
			_logEntries = new List<LogEntry>();
			_commandHistory = new List<string>();
			_isExpanded = PlayerPrefs.GetInt(PREF_KEY_EXPANDED, 1) == 1;
			_expandAnimationProgress = _isExpanded ? 1f : 0f;

			if (!string.IsNullOrEmpty(_theme.WelcomeText))
			{
				Append(_theme.WelcomeText);
			}

			Application.logMessageReceived += CaptureLog;
		}

		private void OnDestroy()
		{
			Application.logMessageReceived -= CaptureLog;
			DestroyTexture(ref _backgroundTexture);
			DestroyTexture(ref _inputBackgroundTexture);
			DestroyTexture(ref _inputFocusedTexture);
			DestroyTexture(ref _separatorTexture);
		}

		private void Update()
		{
			if (_isAnimating)
			{
				var speed = _theme.AnimationSpeed;
				var targetProgress = _isVisible ? 1f : 0f;
				var maxDelta = Time.unscaledDeltaTime * speed;
				_animationProgress = Mathf.MoveTowards(_animationProgress, targetProgress, maxDelta);
				if (Mathf.Approximately(_animationProgress, targetProgress))
				{
					_isAnimating = false;
				}
			}

			if (_isExpandAnimating)
			{
				var speed = _theme.AnimationSpeed * 1.5f;
				var targetProgress = _isExpanded ? 1f : 0f;
				var maxDelta = Time.unscaledDeltaTime * speed;
				_expandAnimationProgress = Mathf.MoveTowards(_expandAnimationProgress, targetProgress, maxDelta);
				if (Mathf.Approximately(_expandAnimationProgress, targetProgress))
				{
					_isExpandAnimating = false;
				}
			}
		}

		private void OnGUI()
		{
			if (!_stylesInitialized && !_lazyStylesInitialization)
			{
				InitializeStyles();
			}

			if (_animationProgress > 0f)
			{
				ProcessInput();
				if (!_stylesInitialized && _lazyStylesInitialization)
				{
					InitializeStyles();
				}

				DrawConsole();
			}
		}

		private void LateUpdate()
		{
			if (!_isVisible && !_isAnimating && AcceptNewInput && _showKeys.Any(Input.GetKeyUp))
			{
				_lastInputTime = Time.unscaledTime;
				Show = true;
			}
		}

		public bool Show
		{
			get => _isVisible;
			set => SetShow(value);
		}

		public string Value { get; set; }

		private bool AcceptNewInput => Time.unscaledTime - _lastInputTime > _acceptNewCommandTime;

		public void Append(string message) => AddLogEntry(message, LogEntryType.OUTPUT);

		public void Clear()
		{
			_logEntries.Clear();
			Value = string.Empty;
			if (!string.IsNullOrEmpty(_theme.WelcomeText))
			{
				Append(_theme.WelcomeText);
			}
		}

		private void DrawConsole()
		{
			var collapsedHeight = _scaledInputHeight + _theme.Padding * 2f;
			var expandedHeight = Mathf.Lerp(collapsedHeight, Screen.height / 2f, _expandAnimationProgress);
			var consoleHeight = expandedHeight * _animationProgress;
			var consoleRect = new Rect(0, 0, Screen.width, consoleHeight);
			GUI.DrawTexture(consoleRect, _backgroundTexture, ScaleMode.StretchToFill);
			GUILayout.BeginArea(consoleRect);

			var logHeight = consoleHeight - _scaledInputHeight - _theme.Padding * 2;
			if (_expandAnimationProgress > 0.1f)
			{
				DrawLog(logHeight);
			}

			DrawSeparator();
			DrawInput();

			GUILayout.EndArea();
		}

		private void DrawLog(float height)
		{
			_logScrollPosition = GUILayout.BeginScrollView(
				_logScrollPosition,
				false,
				true,
				GUIStyle.none,
				GUI.skin.verticalScrollbar,
				GUIStyle.none,
				GUILayout.Height(height));

			for (var index = 0; index < _logEntries.Count; index++)
			{
				var entry = _logEntries[index];
				var color = entry.Type switch
				{
					LogEntryType.COMMAND => _theme.CommandColor,
					LogEntryType.OUTPUT => _theme.LogColor,
					LogEntryType.ERROR => _theme.ErrorColor,
					LogEntryType.WARNING => _theme.WarningColor,
					_ => _theme.LogColor
				};

				var prefix = entry.Type switch
				{
					LogEntryType.COMMAND => _theme.PromptSymbol + " ",
					LogEntryType.ERROR => "[ERROR] ",
					LogEntryType.WARNING => "[WARN] ",
					_ => ""
				};

				var colorHex = ColorUtility.ToHtmlStringRGB(color);
				var text = $"<color=#{colorHex}>{prefix}{entry.Message}</color>";
				GUILayout.Label(text, _logStyle);
			}

			GUILayout.EndScrollView();
		}

		private void DrawSeparator()
		{
			_separatorTexture ??= MakeTexture(2, 2, _theme.SeparatorColor);
			GUI.DrawTexture(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
				GUILayout.Height(2), GUILayout.ExpandWidth(true)), _separatorTexture);
		}

		private void DrawInput()
		{
			GUILayout.BeginHorizontal(GUILayout.Height(_scaledInputHeight));
			GUILayout.Space(_theme.Padding);
			GUILayout.Label(_theme.PromptSymbol, _promptStyle,
				GUILayout.Width(30), GUILayout.Height(_scaledInputHeight));

			GUI.SetNextControlName(INPUT_CONTROL_NAME);
			var newValue = GUILayout.TextField(Value, _inputStyle,
				GUILayout.ExpandWidth(true), GUILayout.Height(_scaledInputHeight));

			if (newValue != Value && newValue.Length > Value.Length)
			{
				var addedChar = newValue[^1];
				Value = ShouldFilterCharacter(addedChar) ? Value : newValue;
			}
			else
			{
				Value = newValue;
			}

			GUILayout.Space(_theme.Padding);
			GUILayout.EndHorizontal();

			if (_isVisible && _animationProgress >= 0.99f)
			{
				GUI.FocusControl(INPUT_CONTROL_NAME);
			}
		}

		private bool ShouldFilterCharacter(char c)
		{
			foreach (var keyCode in _showKeys)
			{
				var keyChar = KeyCodeToChar(keyCode);
				if (keyChar != '\0' && c == keyChar)
				{
					return true;
				}
			}

			return false;
		}

		private static char KeyCodeToChar(KeyCode keyCode) => keyCode switch
		{
			KeyCode.Backslash => '\\',
			KeyCode.BackQuote => '`',
			KeyCode.Quote => '\'',
			KeyCode.Slash => '/',
			KeyCode.Tilde => '~',
			_ => '\0'
		};

		private void ProcessInput()
		{
			var @event = Event.current;
			if (@event.type is EventType.Layout or EventType.Repaint)
			{
				return;
			}

			if (@event.type != EventType.KeyUp)
			{
				return;
			}

			if (@event.keyCode == KeyCode.Return)
			{
				ExecuteCommand(Value);
				Value = string.Empty;
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.DownArrow)
			{
				NavigateHistory(1);
				StateChanged?.Invoke(ConsoleAction.NEXT_COMMAND_IN_HISTORY);
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.UpArrow)
			{
				NavigateHistory(-1);
				StateChanged?.Invoke(ConsoleAction.PREVIOUS_COMMAND_IN_HISTORY);
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.Escape)
			{
				Show = false;
				@event.Use();
			}
			else if (_showKeys.Any(x => x == @event.keyCode))
			{
				_lastInputTime = Time.unscaledTime;
				Show = false;
				@event.Use();
			}
			else if (@event.keyCode == _toggleExpandKey)
			{
				ToggleExpanded();
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.PageUp)
			{
				_logScrollPosition.y -= 100f;
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.PageDown)
			{
				_logScrollPosition.y += 100f;
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.C && @event.control)
			{
				CopyLogToClipboard();
				@event.Use();
			}
		}

		private void ExecuteCommand(string command)
		{
			AddLogEntry(command, LogEntryType.COMMAND);
			AddToHistory(command);
			_logScrollPosition.y = Mathf.Infinity;
			StateChanged?.Invoke(ConsoleAction.EXECUTE_COMMAND);
		}

		private void CaptureLog(string condition, string stackTrace, LogType type)
		{
			LogEntryType entryType;
			switch (type)
			{
				case LogType.Warning:
					entryType = LogEntryType.WARNING;
					break;
				case LogType.Error:
				case LogType.Assert:
				case LogType.Exception:
					entryType = LogEntryType.ERROR;
					break;
				case LogType.Log:
				default:
					entryType = LogEntryType.OUTPUT;
					break;
			}

			AddLogEntry(condition, entryType);
			_logScrollPosition.y = Mathf.Infinity;
		}

		private void AddLogEntry(string message, LogEntryType type)
		{
			_logEntries.Add(new LogEntry { Type = type, Message = message });
			if (_logEntries.Count > _maxLogEntries)
			{
				_logEntries.RemoveAt(0);
			}
		}

		private void AddToHistory(string command)
		{
			if (_commandHistory.Count > 0 && _commandHistory[^1] == command)
			{
				return;
			}

			_commandHistory.Add(command);
			if (_commandHistory.Count > _maxHistorySize)
			{
				_commandHistory.RemoveAt(0);
			}

			_historyIndex = -1;
		}

		private void NavigateHistory(int direction)
		{
			if (_commandHistory.Count == 0)
			{
				return;
			}

			if (_historyIndex == -1 && direction < 0)
			{
				_currentInput = Value;
			}

			_historyIndex += direction;
			if (_historyIndex < -1)
			{
				_historyIndex = -1;
			}
			else if (_historyIndex >= _commandHistory.Count)
			{
				_historyIndex = _commandHistory.Count - 1;
			}

			Value = _historyIndex == -1 ? _currentInput : _commandHistory[_commandHistory.Count - 1 - _historyIndex];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SetShow(bool value)
		{
			if (_isVisible == value)
			{
				return;
			}

			_isVisible = value;
			_isAnimating = true;

			if (value)
			{
				_historyIndex = -1;
				_currentInput = string.Empty;
				_previousCursorState = Cursor.lockState;
				if (_isExpanded)
				{
					Cursor.lockState = CursorLockMode.None;
				}

				Opened?.Invoke();
			}
			else
			{
				Cursor.lockState = _previousCursorState;
				StateChanged?.Invoke(ConsoleAction.NONE);
				Closed?.Invoke();
			}
		}

		private void ToggleExpanded()
		{
			_isExpanded = !_isExpanded;
			_isExpandAnimating = true;
			SaveExpandedState();
			Cursor.lockState = _isExpanded ? CursorLockMode.None : _previousCursorState;
		}

		private void SaveExpandedState()
		{
			PlayerPrefs.SetInt(PREF_KEY_EXPANDED, _isExpanded ? 1 : 0);
			PlayerPrefs.Save();
		}

		private void CopyLogToClipboard()
		{
			var builder = new StringBuilder();
			foreach (var entry in _logEntries)
			{
				if (entry.Type == LogEntryType.OUTPUT && entry.Message == _theme.WelcomeText)
				{
					continue;
				}

				var prefix = entry.Type switch
				{
					LogEntryType.COMMAND => _theme.PromptSymbol + " ",
					LogEntryType.ERROR => "[ERROR] ",
					LogEntryType.WARNING => "[WARN] ",
					_ => ""
				};
				builder.AppendLine(prefix + entry.Message);
			}

			GUIUtility.systemCopyBuffer = builder.ToString();
		}

		private void InitializeStyles()
		{
			_backgroundTexture ??= MakeTexture(2, 2, _theme.BackgroundColor);
			_inputBackgroundTexture ??= MakeTexture(2, 2, _theme.InputBackgroundColor);
			_inputFocusedTexture ??= MakeTexture(2, 2, _theme.InputFocusedColor);

			_logStyle ??= new GUIStyle(GUI.skin.label)
			{
				font = _theme.Font ?? Font.CreateDynamicFontFromOSFont(
					new[] { "Consolas", "Courier New", "Courier" },
					_theme.FontSize),
				fontSize = _theme.FontSize,
				normal = { textColor = _theme.LogColor },
				wordWrap = false,
				richText = true,
				padding = new RectOffset(10, 10, 2, 2),
				alignment = TextAnchor.LowerLeft
			};

			_inputStyle ??= new GUIStyle(GUI.skin.textField)
			{
				font = _theme.Font ?? Font.CreateDynamicFontFromOSFont(
					new[] { "Consolas", "Courier New", "Courier" },
					_theme.FontSize),
				fontSize = _theme.FontSize + 2,
				normal =
				{
					textColor = _theme.InputColor,
					background = _inputBackgroundTexture
				},
				focused =
				{
					textColor = _theme.InputColor,
					background = _inputFocusedTexture
				},
				padding = new RectOffset(5, 5, 5, 5),
				border = new RectOffset(0, 0, 0, 0),
				fontStyle = FontStyle.Bold
			};

			_promptStyle ??= new GUIStyle(GUI.skin.label)
			{
				font = _theme.Font ?? Font.CreateDynamicFontFromOSFont(
					new[] { "Consolas", "Courier New", "Courier" },
					_theme.FontSize),
				fontSize = _theme.FontSize + 2,
				normal = { textColor = _theme.PromptColor },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft
			};

			var fontScale = Mathf.Clamp(Screen.height / 1080f, 0.9f, 1.4f);
			_logStyle.fontSize = Mathf.RoundToInt(_theme.FontSize * fontScale);
			_inputStyle.fontSize = _promptStyle.fontSize = Mathf.RoundToInt((_theme.FontSize + 2) * fontScale);
			_scaledInputHeight = _theme.InputHeight * fontScale;

			_stylesInitialized = true;
		}

		private static Texture2D MakeTexture(int width, int height, Color color)
		{
			var pixels = new Color[width * height];
			for (var index = 0; index < pixels.Length; index++)
			{
				pixels[index] = color;
			}

			var texture = new Texture2D(width, height);
			texture.SetPixels(pixels);
			texture.Apply();

			return texture;
		}

		private static void DestroyTexture(ref Texture2D texture)
		{
			if (texture == null)
			{
				return;
			}

			Destroy(texture);
			texture = null;
		}

		private enum LogEntryType
		{
			COMMAND,
			OUTPUT,
			ERROR,
			WARNING
		}

		private struct LogEntry
		{
			public LogEntryType Type;
			public string Message;
		}
	}
}