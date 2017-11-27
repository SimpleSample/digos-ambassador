﻿//
//  ThonPronounProvider.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Provides thon pronouns.
	/// </summary>
	[UsedImplicitly]
	public class ThonPronounProvider : IPronounProvider
	{
		/// <inheritdoc />
		public string Family => "Thon";

		/// <inheritdoc />
		public string GetSubjectForm(bool plural = false, bool withVerb = false) => "thon";

		/// <inheritdoc />
		public string GetObjectForm(bool plural = false) => "thon";

		/// <inheritdoc />
		public string GetPossessiveAdjectiveForm(bool plural = false) => "thons";

		/// <inheritdoc />
		public string GetPossessiveForm(bool plural = false) => "thon's";

		/// <inheritdoc />
		public string GetReflexiveForm(bool plural = false) => "thonself";
	}
}