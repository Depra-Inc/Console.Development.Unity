// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Depra.Console.Development.Module;

namespace Depra.Console.Development
{
	[AddComponentMenu(MENU_PATH + nameof(IMGUIDevelopmentConsole))]
	public sealed class IMGUIDevelopmentConsole : MonoBehaviour,
		IDevelopmentConsoleInput,
		IDevelopmentConsoleOutput
	{
		[SerializeField] private KeyCode _showKey = KeyCode.Backslash;
		[SerializeField] private Settings _settings;

		private bool _show;
		private bool _needFocus = true;
		private float _lastInputTime = -1f;

		private GUIStyle _styleTextX;
		private GUIStyle _styleTextArea;
		private Matrix4x4 _originalGUIMatrix;

		private event Action<ConsoleAction> StateChanged;

		event Action<ConsoleAction> IDevelopmentConsoleInput.StateChanged
		{
			add => StateChanged += value;
			remove => StateChanged -= value;
		}

		private void OnGUI()
		{
			if (!Show)
			{
				return;
			}

			PushGUIMatrix();
			DrawGUI();
			ProcessInput();
			PullGUIMatrix();
		}

		private void LateUpdate()
		{
			if (!Show && AcceptNewInput && Input.GetKeyUp(_showKey))
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

		public void Append(string message) => Value += message;

		public void Clear() => Value = string.Empty;

		private void DrawGUI()
		{
			_styleTextArea ??= new GUIStyle(GUI.skin.textArea)
			{
				fontStyle = FontStyle.Italic,
				fontSize = _settings.FontSize,
				alignment = TextAnchor.MiddleLeft,
				normal = { textColor = _settings.FontColor }
			};

			_styleTextX ??= new GUIStyle(GUI.skin.textArea)
			{
				fontStyle = FontStyle.Bold,
				fontSize = _settings.FontSize,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = _settings.FontColor }
			};

			GUILayout.BeginArea(new Rect(_settings.Margin,
				_settings.Margin,
				_settings.DesignScreenWidth -
				_settings.Margin * 2f,
				_settings.Height), GUI.skin.box);

			GUILayout.BeginHorizontal();
			GUI.SetNextControlName(Settings.TEXT_INPUT_NAME);
			Value = GUILayout.TextField(Value, _styleTextArea, GUILayout.ExpandHeight(true));
			var height = _settings.Height - _settings.Margin * 2f;
			if (GUILayout.Button("X", _styleTextX, GUILayout.Width(_settings.Height), GUILayout.Height(height)))
			{
				Show = false;
			}

			GUILayout.EndHorizontal();
			GUILayout.EndArea();

			if (_needFocus)
			{
				GUI.FocusControl(Settings.TEXT_INPUT_NAME);
				_needFocus = false;
			}
		}

		private void PushGUIMatrix()
		{
			var ratioWidth = Screen.width / _settings.DesignScreenWidth;
			var ratioHeight = Screen.height / _settings.DesignScreenHeight;

			_originalGUIMatrix = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(ratioWidth, ratioHeight, 1.0f));
		}

		private void PullGUIMatrix() => GUI.matrix = _originalGUIMatrix;

		private void ProcessInput()
		{
			var @event = Event.current;
			if (@event.type is EventType.Layout or EventType.Repaint)
			{
				return;
			}

			if (@event.keyCode == KeyCode.Return)
			{
				StateChanged?.Invoke(ConsoleAction.EXECUTE_COMMAND);
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.DownArrow)
			{
				StateChanged?.Invoke(ConsoleAction.NEXT_COMMAND_IN_HISTORY);
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.UpArrow)
			{
				StateChanged?.Invoke(ConsoleAction.PREVIOUS_COMMAND_IN_HISTORY);
				@event.Use();
			}
			else if (@event.keyCode == KeyCode.Escape || @event.keyCode == _showKey)
			{
				Show = false;
				@event.Use();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SetShow(bool value)
		{
			switch (value)
			{
				case true when !_show:
					_needFocus = true;
					break;
				case false when _show:
					StateChanged?.Invoke(ConsoleAction.NONE);
					break;
			}

			_show = value;
		}

		[Serializable]
		public sealed class Settings
		{
			internal const string TEXT_INPUT_NAME = "ConsoleTextInput";

			[field: SerializeField] public float AcceptNewCommandTime { get; private set; } = 0.1f;
			[field: SerializeField] public int FontSize { get; private set; } = 30;
			[field: SerializeField] public Color FontColor { get; private set; } = Color.white;
			[field: SerializeField] public float Height { get; private set; } = 48.0f;
			[field: SerializeField] public float Margin { get; private set; } = 5.0f;
			[field: SerializeField] public float DesignScreenWidth { get; private set; } = 1920.0f;
			[field: SerializeField] public float DesignScreenHeight { get; private set; } = 1080.0f;
		}
	}
}