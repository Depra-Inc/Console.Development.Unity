// SPDX-License-Identifier: Apache-2.0
// © 2025 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Depra.Console.Development
{
	/// <summary>
	/// Marks the associated method as a command.
	/// This means it will be usable as a command from a console.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
	public sealed class DevelopmentCommandAttribute : Attribute
	{
		private static readonly char[] BANNED_ALIAS_CHARS = { ' ', '(', ')', '{', '}', '[', ']', '<', '>' };

		public readonly string Alias;
		public readonly string Usage;
		public readonly string Description;
		public readonly bool Valid = true;

		public DevelopmentCommandAttribute(string alias, string usage = "", string description = "") : this(alias)
		{
			Usage = usage;
			Description = description;
		}

		public DevelopmentCommandAttribute([CallerMemberName] string alias = "")
		{
			Alias = alias.ToLower();
			foreach (var symbol in BANNED_ALIAS_CHARS)
			{
				if (Alias.Contains(symbol) == false)
				{
					continue;
				}

				Debug.LogError(
					"Development Processor Error: " +
					$"Command with alias '{Alias}' contains the char '{symbol}' which is banned. " +
					"Unexpected behaviour may occur.");

				Valid = false;
			}
		}
	}
}