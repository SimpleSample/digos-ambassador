﻿//
//  DiscordService.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using DIGOS.Ambassador.Services.Entity;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Discord
{
	/// <summary>
	/// Handles integration with Discord.
	/// </summary>
	public class DiscordService
	{
		private static readonly HttpClient Client = new HttpClient();

		static DiscordService()
		{
			Client.Timeout = TimeSpan.FromSeconds(4);
		}

		/// <summary>
		/// Gets the byte stream from an attachment.
		/// </summary>
		/// <param name="attachment">The attachment to get.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Stream>> GetAttachmentStreamAsync([NotNull] Attachment attachment)
		{
			try
			{
				var stream = await Client.GetStreamAsync(attachment.Url);
				return RetrieveEntityResult<Stream>.FromSuccess(stream);
			}
			catch (HttpRequestException hex)
			{
				return RetrieveEntityResult<Stream>.FromError(CommandError.Exception, hex.ToString());
			}
			catch (TaskCanceledException)
			{
				return RetrieveEntityResult<Stream>.FromError(CommandError.Unsuccessful, "The download operation timed out.");
			}
		}
	}
}
