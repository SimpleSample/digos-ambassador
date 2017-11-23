﻿//
//  CharacterService.cs
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
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Image = DIGOS.Ambassador.Database.Data.Image;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Acts as an interface for accessing and modifying user characters.
	/// </summary>
	public class CharacterService
	{
		private readonly CommandService Commands;

		private readonly OwnedEntityService OwnedEntities;

		private readonly ContentService Content;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterService"/> class.
		/// </summary>
		/// <param name="commands">The application's command service.</param>
		/// <param name="entityService">The application's owned entity service.</param>
		/// <param name="content">The content service.</param>
		public CharacterService(CommandService commands, OwnedEntityService entityService, ContentService content)
		{
			this.Commands = commands;
			this.OwnedEntities = entityService;
			this.Content = content;
		}

		/// <summary>
		/// This method searches for the best matching character given an owner and a name. If no owner is provided, then
		/// the global list is searched for a unique name. If no match can be found, a failed result is returned.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The command context.</param>
		/// <param name="characterOwner">The owner of the character, if any.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Character>> GetBestMatchingCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[CanBeNull] IUser characterOwner,
			[CanBeNull] string characterName
		)
		{
			if (characterOwner is null && characterName is null)
			{
				return await GetCurrentCharacterAsync(db, context, context.Message.Author);
			}

			if (characterOwner is null)
			{
				return await GetNamedCharacterAsync(db, characterName);
			}

			if (characterName.IsNullOrWhitespace())
			{
				return await GetCurrentCharacterAsync(db, context, characterOwner);
			}

			return await GetUserCharacterByNameAsync(db, context, characterOwner, characterName);
		}

		/// <summary>
		/// Gets the current character a user has assumed the form of.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="discordUser">The user to get the current character of.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Character>> GetCurrentCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] IUser discordUser
		)
		{
			if (!await HasActiveCharacterOnServerAsync(db, discordUser, context.Guild))
			{
				var isCurrentUser = context.Message.Author.Id == discordUser.Id;
				var errorMessage = isCurrentUser
					? "You haven't assumed a character."
					: "The user hasn't assumed a character.";

				return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, errorMessage);
			}

			var currentCharacter = await GetUserCharacters(db, discordUser)
			.FirstOrDefaultAsync
			(
				ch => ch.CurrentServers.Any(s => s.DiscordGuildID == context.Guild.Id)
			);

			if (currentCharacter is null)
			{
				return RetrieveEntityResult<Character>.FromError(CommandError.Unsuccessful, "Failed to retrieve a current character.");
			}

			return RetrieveEntityResult<Character>.FromSuccess(currentCharacter);
		}

		/// <summary>
		/// Gets a character by its given name.
		/// </summary>
		/// <param name="db">The database context where the data is stored.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Character>> GetNamedCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string characterName
		)
		{
			if (await db.Characters.CountAsync(ch => ch.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase)) > 1)
			{
				return RetrieveEntityResult<Character>.FromError
				(
					CommandError.MultipleMatches,
					"There's more than one character with that name. Please specify which user it belongs to."
				);
			}

			var character = db.Characters
				.Include(c => c.Owner)
				.Include(c => c.CurrentServers)
				.Include(c => c.DefaultAppearance)
				.Include(c => c.TransformedAppearance)
				.FirstOrDefault(rp => rp.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));

			if (character is null)
			{
				return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, "No character with that name found.");
			}

			return RetrieveEntityResult<Character>.FromSuccess(character);
		}

		/// <summary>
		/// Gets a character belonging to a given user by a given name.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="characterOwner">The user to get the character from.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		public async Task<RetrieveEntityResult<Character>> GetUserCharacterByNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] IUser characterOwner,
			[NotNull] string characterName
		)
		{
			var character = await GetUserCharacters(db, characterOwner)
			.FirstOrDefaultAsync
			(
				ch => ch.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase)
			);

			if (character is null)
			{
				var isCurrentUser = context.Message.Author.Id == characterOwner.Id;
				var errorMessage = isCurrentUser
					? "You don't own a character with that name."
					: "The user doesn't own a character with that name.";

				return RetrieveEntityResult<Character>.FromError(CommandError.ObjectNotFound, errorMessage);
			}

			return RetrieveEntityResult<Character>.FromSuccess(character);
		}

		/// <summary>
		/// Makes the given character current on the given server.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the user.</param>
		/// <param name="discordServer">The server to make the character current on.</param>
		/// <param name="character">The character to make current.</param>
		/// <returns>A task that must be awaited.</returns>
		public async Task MakeCharacterCurrentOnServerAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] SocketCommandContext context,
			[NotNull] IGuild discordServer,
			[NotNull] Character character
		)
		{
			var server = await db.GetOrRegisterServerAsync(discordServer);
			var user = context.Guild.GetUser(character.Owner.DiscordID);

			await ClearCurrentCharacterOnServerAsync(db, user, discordServer);

			character.CurrentServers.Add(server);

			await db.SaveChangesAsync();
		}

		/// <summary>
		/// Clears any current characters in the server from the given user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to clear the characters from.</param>
		/// <param name="discordServer">The server to clear the characters on.</param>
		/// <returns>A task that must be awaited.</returns>
		public async Task ClearCurrentCharacterOnServerAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IGuild discordServer
		)
		{
			if (!await HasActiveCharacterOnServerAsync(db, discordUser, discordServer))
			{
				return;
			}

			var currentCharactersOnServer = GetUserCharacters(db, discordUser)
				.Where(ch => ch.CurrentServers.Any(s => s.DiscordGuildID == discordServer.Id));

			await currentCharactersOnServer.ForEachAsync
			(
				ch => ch.CurrentServers
					.RemoveAll(s => s.DiscordGuildID == discordServer.Id)
			);

			await db.SaveChangesAsync();
		}

		/// <summary>
		/// Determines whether or not the given user has an active character on the given server.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to check.</param>
		/// <param name="discordServer">The server to check.</param>
		/// <returns>true if the user has an active character on the server; otherwise, false.</returns>
		public async Task<bool> HasActiveCharacterOnServerAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IGuild discordServer
		)
		{
			var userCharacters = GetUserCharacters(db, discordUser);

			return await userCharacters
				.Where(ch => ch.CurrentServers.Any())
				.AnyAsync
				(
					c => c.CurrentServers
						.Any(s => s.DiscordGuildID == discordServer.Id)
				);
		}

		/// <summary>
		/// Creates a character with the given name and default settings.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <returns>A creation result which may or may not have been successful.</returns>
		public async Task<CreateEntityResult<Character>> CreateCharacterAsync(GlobalInfoContext db, ICommandContext context, string characterName)
		{
			return await CreateCharacterAsync(db, context, characterName, this.Content.DefaultAvatarUri.ToString(), null, null, null);
		}

		/// <summary>
		/// Creates a character with the given parameters.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="characterName">The name of the character.</param>
		/// <param name="characterAvatarUrl">The character's avatar url.</param>
		/// <param name="characterNickname">The nicknme that should be applied to the user when the character is active.</param>
		/// <param name="characterSummary">The summary of the character.</param>
		/// <param name="characterDescription">The full description of the character.</param>
		/// <returns>A creation result which may or may not have been successful.</returns>
		public async Task<CreateEntityResult<Character>> CreateCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] string characterName,
			[NotNull] string characterAvatarUrl,
			[CanBeNull] string characterNickname,
			[CanBeNull] string characterSummary,
			[CanBeNull] string characterDescription
		)
		{
			var owner = await db.GetOrRegisterUserAsync(context.Message.Author);
			var character = new Character
			{
				Owner = owner,
			};

			var modifyEntityResult = await SetCharacterNameAsync(db, context, character, characterName);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			modifyEntityResult = await SetCharacterAvatarAsync(db, character, characterAvatarUrl);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			if (!(characterNickname is null))
			{
				modifyEntityResult = await SetCharacterNicknameAsync(db, character, characterNickname);
				if (!modifyEntityResult.IsSuccess)
				{
					return CreateEntityResult<Character>.FromError(modifyEntityResult);
				}
			}

			characterSummary = characterSummary ?? "No summary set.";
			modifyEntityResult = await SetCharacterSummaryAsync(db, character, characterSummary);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			characterDescription = characterDescription ?? "No description set.";
			modifyEntityResult = await SetCharacterDescriptionAsync(db, character, characterDescription);
			if (!modifyEntityResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(modifyEntityResult);
			}

			owner.Characters.Add(character);
			await db.Characters.AddAsync(character);
			await db.SaveChangesAsync();

			var getCharacterResult = await GetUserCharacterByNameAsync(db, context, context.Message.Author, characterName);
			if (!getCharacterResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(getCharacterResult);
			}

			return CreateEntityResult<Character>.FromSuccess(getCharacterResult.Entity);
		}

		/// <summary>
		/// Sets the name of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="context">The context of the operation.</param>
		/// <param name="character">The character to set the name of.</param>
		/// <param name="newCharacterName">The new name.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			[NotNull] string newCharacterName
		)
		{
			var isCurrentUser = context.Message.Author.Id == character.Owner.DiscordID;
			if (string.IsNullOrWhiteSpace(newCharacterName))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a name.");
			}

			if (!await IsCharacterNameUniqueForUserAsync(db, context.Message.Author, newCharacterName))
			{
				var errorMessage = isCurrentUser
					? "You already have a character with that name."
					: "The user already has a character with that name.";

				return ModifyEntityResult.FromError(CommandError.MultipleMatches, errorMessage);
			}

			var commandModule = this.Commands.Modules.First(m => m.Name == "character");
			var validNameResult = this.OwnedEntities.IsEntityNameValid(commandModule, newCharacterName);
			if (!validNameResult.IsSuccess)
			{
				return ModifyEntityResult.FromError(validNameResult);
			}

			character.Name = newCharacterName;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the avatar of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the avatar of.</param>
		/// <param name="newCharacterAvatarUrl">The new avatar.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterAvatarAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterAvatarUrl
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterAvatarUrl))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new nickname.");
			}

			character.AvatarUrl = newCharacterAvatarUrl;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the nickname of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the nickname of.</param>
		/// <param name="newCharacterNickname">The new nickname.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterNicknameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterNickname
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterNickname))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new nickname.");
			}

			if (newCharacterNickname.Length > 32)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The summary is too long. Nicknames can be at most 32 characters.");
			}

			character.Nickname = newCharacterNickname;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the summary of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the summary of.</param>
		/// <param name="newCharacterSummary">The new summary.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterSummaryAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterSummary
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterSummary))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new summary.");
			}

			if (newCharacterSummary.Length > 240)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The summary is too long.");
			}

			character.Summary = newCharacterSummary;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the description of the given character.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the description of.</param>
		/// <param name="newCharacterDescription">The new description.</param>
		/// <returns>A modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCharacterDescriptionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string newCharacterDescription
		)
		{
			if (string.IsNullOrWhiteSpace(newCharacterDescription))
			{
				return ModifyEntityResult.FromError(CommandError.BadArgCount, "You need to provide a new description.");
			}

			character.Description = newCharacterDescription;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets whether or not a character is NSFW.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="character">The character to edit.</param>
		/// <param name="isNSFW">Whether or not the character is NSFW</param>
		/// <returns>A task that must be awaited.</returns>
		public async Task SetCharacterIsNSFWAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			bool isNSFW
		)
		{
			character.IsNSFW = isNSFW;
			await db.SaveChangesAsync();
		}

		/// <summary>
		/// Transfers ownership of the named character to the specified user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="character">The character to transfer.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> TransferCharacterOwnershipAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser newOwner,
			[NotNull] Character character
		)
		{
			var newOwnerCharacters = GetUserCharacters(db, newOwner);
			return await this.OwnedEntities.TransferEntityOwnershipAsync
			(
				db,
				newOwner,
				newOwnerCharacters,
				character
			);
		}

		/// <summary>
		/// Get the characters owned by the given user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to get the characters of.</param>
		/// <returns>A queryable list of characters belonging to the user.</returns>
		[NotNull]
		[ItemNotNull]
		public IQueryable<Character> GetUserCharacters([NotNull]GlobalInfoContext db, [NotNull]IUser discordUser)
		{
			var characters = db.Characters
				.Include(ch => ch.Owner)
				.Include(ch => ch.Images)
				.Include(ch => ch.CurrentServers)
				.Include(ch => ch.DefaultAppearance)
				.Include(ch => ch.TransformedAppearance)
				.Where(ch => ch.Owner.DiscordID == discordUser.Id);

			return characters;
		}

		/// <summary>
		/// Determines whether or not the given character name is unique for a given user.
		/// </summary>
		/// <param name="db">The database where the characters are stored.</param>
		/// <param name="discordUser">The user to check.</param>
		/// <param name="characterName">The character name to check.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		public async Task<bool> IsCharacterNameUniqueForUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] string characterName
		)
		{
			var userCharacters = GetUserCharacters(db, discordUser);
			return await this.OwnedEntities.IsEntityNameUniqueForUserAsync(userCharacters, characterName);
		}

		/// <summary>
		/// Adds the given image with the given metadata to the given character.
		/// </summary>
		/// <param name="db">The database where the characters and images are stored.</param>
		/// <param name="character">The character to add the image to.</param>
		/// <param name="imageName">The name of the image.</param>
		/// <param name="imageUrl">The url of the image.</param>
		/// <param name="imageCaption">The caption of the image.</param>
		/// <param name="isNSFW">Whether or not the image is NSFW</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> AddImageToCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string imageName,
			[NotNull] string imageUrl,
			[CanBeNull] string imageCaption = null,
			bool isNSFW = false
		)
		{
			bool isImageNameUnique = !character.Images.Any(i => i.Name.Equals(imageName, StringComparison.OrdinalIgnoreCase));
			if (!isImageNameUnique)
			{
				return ModifyEntityResult.FromError(CommandError.MultipleMatches, "The character already has an image with that name.");
			}

			var image = new Image
			{
				Name = imageName,
				Caption = imageCaption,
				Url = imageUrl,
				IsNSFW = isNSFW
			};

			character.Images.Add(image);
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Added);
		}

		/// <summary>
		/// Removes the named image from the given character.
		/// </summary>
		/// <param name="db">The database where the characters and images are stored.</param>
		/// <param name="character">The character to remove the image from.</param>
		/// <param name="imageName">The name of the image.</param>
		/// <returns>An execution result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> RemoveImageFromCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character,
			[NotNull] string imageName
		)
		{
			bool hasNamedImage = character.Images.Any(i => i.Name.Equals(imageName, StringComparison.OrdinalIgnoreCase));
			if (!hasNamedImage)
			{
				return ModifyEntityResult.FromError(CommandError.MultipleMatches, "The character has no image with that name.");
			}

			character.Images.RemoveAll(i => i.Name.Equals(imageName, StringComparison.OrdinalIgnoreCase));
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Added);
		}

		/// <summary>
		/// Creates a new template character with a given appearance.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="characterName">The name of the new character.</param>
		/// <param name="appearance">The appearance that the new character should have.</param>
		/// <returns>A creation result which may or may not have succeeded.</returns>
		public async Task<CreateEntityResult<Character>> CreateCharacterFromAppearanceAsync(GlobalInfoContext db, ICommandContext context, string characterName, Appearance appearance)
		{
			var createCharacterResult = await CreateCharacterAsync(db, context, characterName);
			if (!createCharacterResult.IsSuccess)
			{
				return createCharacterResult;
			}

			var newCharacter = createCharacterResult.Entity;
			newCharacter.DefaultAppearance = appearance;

			await db.SaveChangesAsync();

			var getCharacterResult = await GetUserCharacterByNameAsync(db, context, context.Message.Author, characterName);
			if (!getCharacterResult.IsSuccess)
			{
				return CreateEntityResult<Character>.FromError(getCharacterResult);
			}

			return CreateEntityResult<Character>.FromSuccess(getCharacterResult.Entity);
		}
	}
}