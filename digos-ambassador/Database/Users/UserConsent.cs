﻿//
//  UserConsent.cs
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

using DIGOS.Ambassador.Database.Interfaces;

namespace DIGOS.Ambassador.Database.Users
{
	/// <summary>
	/// Holds information about whether or not a user has granted consent to store user data.
	/// </summary>
	public class UserConsent : IEFEntity
	{
		/// <inheritdoc />
		public long ID { get; set; }

		/// <summary>
		/// Gets or sets the Discord ID of the user.
		/// </summary>
		public long DiscordID { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has consented.
		/// </summary>
		public bool HasConsented { get; set; }
	}
}
