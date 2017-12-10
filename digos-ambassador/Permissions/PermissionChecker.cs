﻿//
//  PermissionChecker.cs
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

using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Users;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Permissions
{
	/// <summary>
	/// Holds utility methods for checking permissions.
	/// </summary>
	public static class PermissionChecker
	{
		/// <summary>
		/// Determines whether or not the user has the given permission.
		/// </summary>
		/// <param name="discordServer">The Discord server that the command was executed on.</param>
		/// <param name="user">The user.</param>
		/// <param name="requiredPermission">The permission.</param>
		/// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
		[Pure]
		public static async Task<bool> HasPermissionAsync
		(
			[CanBeNull] IGuild discordServer,
			[NotNull] User user,
			[NotNull] RequiredPermission requiredPermission
		)
		{
			using (var db = new GlobalInfoContext())
			{
				return await HasPermissionAsync(db, discordServer, user, requiredPermission);
			}
		}

		/// <summary>
		/// Determines whether or not the user has the given permission.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The Discord server that the command was executed on.</param>
		/// <param name="user">The user.</param>
		/// <param name="requiredPermission">The permission.</param>
		/// <returns><value>true</value> if the user has the permission; otherwise, <value>false</value>.</returns>
		[Pure]
		public static async Task<bool> HasPermissionAsync
		(
			[NotNull] GlobalInfoContext db,
			[CanBeNull] IGuild discordServer,
			[NotNull] User user,
			[NotNull] RequiredPermission requiredPermission
		)
		{
			if (discordServer is null)
			{
				return DefaultPermissions.DefaultPermissionSet.Any
				(
					dp =>
						dp.Permission == requiredPermission.Permission &&
						dp.Target.HasFlag(requiredPermission.Target)
				);
			}

			// The server owner always has all permissions by default
			if (discordServer.OwnerId == user.Identifier)
			{
				return true;
			}

			// First, check if the user has the permission on a global level
			var hasGlobalPermission = await db.GlobalPermissions.AnyAsync
			(
				gp =>
					gp.User.Identifier.DiscordID == user.Identifier.DiscordID &&
					gp.Permission == requiredPermission.Permission &&
					gp.Target.HasFlag(requiredPermission.Target)
			);

			if (hasGlobalPermission)
			{
				return true;
			}

			// Then, check the user's local permissions
			return user.LocalPermissions.Any
			(
				lp =>
					lp.Permission == requiredPermission.Permission &&
					lp.Target.HasFlag(requiredPermission.Target) &&
					lp.Server.DiscordID == discordServer.Id
			);
		}
	}
}
