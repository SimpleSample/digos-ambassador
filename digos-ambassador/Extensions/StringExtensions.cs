﻿//
//  StringExtensions.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Extension methods for strings.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Determines whether or not a string is null or consists entirely of whitespace characters.
		/// </summary>
		/// <param name="source">The string to check.</param>
		/// <returns>true if the string is null or whitespace; otherwise, false.</returns>
		[Pure]
		[ContractAnnotation("source:null => true")]
		public static bool IsNullOrWhitespace([CanBeNull] this string source)
		{
			return string.IsNullOrWhiteSpace(source);
		}

		/// <summary>
		/// Determines whether or not a string is null or has no characters.
		/// </summary>
		/// <param name="source">The string to check.</param>
		/// <returns>true if the string is null or empty; otherwise, false.</returns>
		[Pure]
		[ContractAnnotation("source:null => true")]
		public static bool IsNullOrEmpty([CanBeNull] this string source)
		{
			return string.IsNullOrEmpty(source);
		}

		/// <summary>
		/// Determines whether or not a string contains another string using the given string comparer.
		/// </summary>
		/// <param name="this">The string to search.</param>
		/// <param name="search">The string to search for.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>true if the string contains the other string; otherwise, false.</returns>
		[Pure]
		public static bool Contains([CanBeNull] this string @this, [CanBeNull] string search, StringComparison comparer)
		{
			return @this != null && search != null && @this.IndexOf(search, comparer) >= 0;
		}
	}
}
