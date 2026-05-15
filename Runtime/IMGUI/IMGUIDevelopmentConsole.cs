// SPDX-License-Identifier: Apache-2.0
// © 2025-2026 Depra <n.melnikov@depra.org>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
		[SerializeField] private Settings _settings;
		[SerializeField] private IMGUIDevelopmentConsoleTheme _theme;

		private const string INPUT_CONTROL_NAME = "Console_TextInput";
		private const string PREF_KEY_EXPANDED = "Console_IsExpanded";

		private bool _show;
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

		private int _historyIndex = -1;
		private string _currentInput = string.Empty;
		private List<LogEntry> _logEntries;
		private List<string> _commandHistory;
		private Vector2 _logScrollPosition;

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

			_logEntries = new List<LogEntry>();
			_commandHistory = new List<string>();
			_isExpanded = PlayerPrefs.GetInt(PREF_KEY_EXPANDED, 1) == 1;
			_expandAnimationProgress = _isExpanded ? 1f : 0f;

			if (!string.IsNullOrEmpty(_theme.WelcomeText))
			{
				Append(_theme.WelcomeText);
			}
		}

		private void OnDestroy()
		{
			DestroyTexture(ref _backgroundTexture);
			DestroyTexture(ref _inputBackgroundTexture);
			DestroyTexture(ref _inputFocusedTexture);
			DestroyTexture(ref _separatorTexture);
		}

		private void Update()
		{
			if (_isAnimating)
			{
				var speed = _settings.AnimationSpeed;
				var targetProgress = _show ? 1f : 0f;
				_animationProgress = Mathf.MoveTowards(_animationProgress, targetProgress, Time.deltaTime * speed);
				if (Mathf.Approximately(_animationProgress, _show ? 1f : 0f))
				{
					_isAnimating = false;
				}
			}

			if (_isExpandAnimating)
			{
				var speed = _settings.AnimationSpeed * 1.5f;
				var targetProgress = _isExpanded ? 1f : 0f;
				_expandAnimationProgress = Mathf.MoveTowards(_expandAnimationProgress, targetProgress, Time.deltaTime * speed);
				if (Mathf.Approximately(_expandAnimationProgress, targetProgress))
				{
					_isExpandAnimating = false;
				}
			}
		}

		private void OnGUI()
		{
			if (_animationProgress > 0f)
			{
				DrawConsole();
				ProcessInput();
			}
		}

		private void LateUpdate()
		{
			if (!_show && !_isAnimating && AcceptNewInput && _showKeys.Any(Input.GetKeyUp))
			{
				_lastInputTime = Time.time;
				Show = true;
			}
		}

		public bool Show
		{
			get => _show;
			set => SetShow(value);
		}

		public string Value { get; set; }

		private bool AcceptNewInput => Time.time - _lastInputTime > _settings.AcceptNewCommandTime;

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
			InitializeStyles();

			var collapsedHeight = _settings.InputHeight + _theme.Padding * 2f;
			var expandedHeight = Mathf.Lerp(collapsedHeight, Screen.height / 2f, _expandAnimationProgress);
			var consoleHeight = expandedHeight * _animationProgress;
			var consoleRect = new Rect(0, 0, Screen.width, consoleHeight);
			GUI.DrawTexture(consoleRect, _backgroundTexture, ScaleMode.StretchToFill);
			GUILayout.BeginArea(consoleRect);

			var logHeight = consoleHeight - _settings.InputHeight - _theme.Padding * 2;
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

			foreach (var entry in _logEntries)
			{
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
			GUILayout.BeginHorizontal(GUILayout.Height(_settings.InputHeight));
			GUILayout.Space(_theme.Padding);
			GUILayout.Label(_theme.PromptSymbol, _promptStyle, GUILayout.Width(30));

			GUI.SetNextControlName(INPUT_CONTROL_NAME);
			Value = GUILayout.TextField(Value, _inputStyle,
				GUILayout.ExpandWidth(true),
				GUILayout.Height(_settings.InputHeight));

			GUILayout.Space(_theme.Padding);
			GUILayout.EndHorizontal();

			if (_show && _animationProgress >= 0.99f)
			{
				GUI.FocusControl(INPUT_CONTROL_NAME);
			}
		}

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
			else if (@event.keyCode == KeyCode.Escape || _showKeys.Any(x => x == @event.keyCode))
			{
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
		}

		private void ExecuteCommand(string command)
		{
			AddLogEntry(command, LogEntryType.COMMAND);
			AddToHistory(command);
			StateChanged?.Invoke(ConsoleAction.EXECUTE_COMMAND);
		}

		private void AddLogEntry(string message, LogEntryType type)
		{
			_logEntries.Add(new LogEntry
			{
				Type = type,
				Message = message,
				Timestamp = Time.time
			});

			if (_logEntries.Count > _settings.MaxLogEntries)
			{
				_logEntries.RemoveAt(0);
			}
		}

		private void AddToHistory(string command)
		{
			_logScrollPosition.y = Mathf.Infinity;
			if (_commandHistory.Count > 0 && _commandHistory[^1] == command)
			{
				return;
			}

			_commandHistory.Add(command);
			if (_commandHistory.Count > _settings.MaxHistorySize)
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
			if (_show == value)
			{
				return;
			}

			_show = value;
			_isAnimating = true;

			if (value)
			{
				_historyIndex = -1;
				_currentInput = string.Empty;
				Opened?.Invoke();
			}
			else
			{
				StateChanged?.Invoke(ConsoleAction.NONE);
				Closed?.Invoke();
			}
		}

		private void ToggleExpanded()
		{
			_isExpanded = !_isExpanded;
			_isExpandAnimating = true;
			SaveExpandedState();
		}
		
		private void SaveExpandedState()
		{
			PlayerPrefs.SetInt(PREF_KEY_EXPANDED, _isExpanded ? 1 : 0);
			PlayerPrefs.Save();
		}

		private void InitializeStyles()
		{
			_backgroundTexture ??= MakeTexture(2, 2, _theme.BackgroundColor);
			_inputBackgroundTexture ??= MakeTexture(2, 2, _theme.InputBackgroundColor);
			_inputFocusedTexture ??= MakeTexture(2, 2, _theme.InputFocusedColor);
			var font = _theme.Font ?? Font.CreateDynamicFontFromOSFont(
				new[] { "Consolas", "Courier New", "Courier" },
				_theme.FontSize);

			_logStyle ??= new GUIStyle(GUI.skin.label)
			{
				font = font,
				fontSize = _theme.FontSize,
				normal = { textColor = _theme.LogColor },
				wordWrap = false,
				richText = true,
				padding = new RectOffset(10, 10, 2, 2),
				alignment = TextAnchor.LowerLeft
			};

			_inputStyle ??= new GUIStyle(GUI.skin.textField)
			{
				font = font,
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
				font = font,
				fontSize = _theme.FontSize + 2,
				normal = { textColor = _theme.PromptColor },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft
			};

			var fontScale = Mathf.Clamp(Screen.height / 1080f, 0.9f, 1.4f);
			_logStyle.fontSize = Mathf.RoundToInt(_theme.FontSize * fontScale);
			_inputStyle.fontSize = _promptStyle.fontSize = _logStyle.fontSize + 2;
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
			public float Timestamp;
		}

		[Serializable]
		private sealed class Settings
		{
			[field: SerializeField] public float InputHeight { get; private set; } = 40f;
			[field: SerializeField] public int MaxHistorySize { get; private set; } = 50;
			[field: SerializeField] public int MaxLogEntries { get; private set; } = 100;
			[field: SerializeField] public float AnimationSpeed { get; private set; } = 5f;
			[field: SerializeField] public float AcceptNewCommandTime { get; private set; } = 0.1f;
		}
	}
}