﻿//
//  TransformationService.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Transformations;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles transformations of users and their characters.
	/// </summary>
	public class TransformationService
	{
		private readonly ContentService Content;

		private TransformationDescriptionBuilder DescriptionBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformationService"/> class.
		/// </summary>
		/// <param name="content">The content service.</param>
		public TransformationService(ContentService content)
		{
			this.Content = content;
		}

		/// <summary>
		/// Sets the description builder to use with the service.
		/// </summary>
		/// <param name="descriptionBuilder">The builder.</param>
		/// <returns>The transformation service with the given builder.</returns>
		[NotNull]
		public TransformationService WithDescriptionBuilder(TransformationDescriptionBuilder descriptionBuilder)
		{
			this.DescriptionBuilder = descriptionBuilder;
			return this;
		}

		/// <summary>
		/// Removes the given character's bodypart.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to remove.</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> RemoveBodypartAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.TryGetAppearanceComponent(bodyPart, chirality, out var component))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			character.CurrentAppearance.Components.Remove(component);
			await db.SaveChangesAsync();

			string removeMessage = this.DescriptionBuilder.BuildRemoveMessage(character, component);
			return ShiftBodypartResult.FromSuccess(removeMessage);
		}

		/// <summary>
		/// Adds the given bodypart to the given character.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to add.</param>
		/// <param name="species">The species of the part to add..</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> AddBodypartAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] string species,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (character.HasComponent(bodyPart, chirality))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character already has that bodypart.");
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(db, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(db, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			var transformation = getTFResult.Entity;

			var component = AppearanceComponent.CreateFrom(transformation, chirality);
			character.CurrentAppearance.Components.Add(component);
			await db.SaveChangesAsync();

			string growMessage = this.DescriptionBuilder.BuildGrowMessage(character, component);
			return ShiftBodypartResult.FromSuccess(growMessage);
		}

		/// <summary>
		/// Shifts the given character's bodypart to the given species.
		/// </summary>
		/// <param name="db">The database where characters and transformations are stored.</param>
		/// <param name="context">The context of the command.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="species">The species to shift the bodypart into.</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] string species,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			var getSpeciesResult = await GetSpeciesByNameAsync(db, species);
			if (!getSpeciesResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getSpeciesResult);
			}

			var getTFResult = await GetTransformationByPartAndSpeciesAsync(db, bodyPart, getSpeciesResult.Entity);
			if (!getTFResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(getTFResult);
			}

			string shiftMessage;
			var transformation = getTFResult.Entity;
			if (!character.TryGetAppearanceComponent(bodyPart, chirality, out var currentComponent))
			{
				currentComponent = AppearanceComponent.CreateFrom(transformation, chirality);
				character.CurrentAppearance.Components.Add(currentComponent);

				shiftMessage = this.DescriptionBuilder.BuildGrowMessage(character, currentComponent);
			}
			else
			{
				if (currentComponent.Transformation.Species.Name.Equals(transformation.Species.Name))
				{
					return ShiftBodypartResult.FromError(CommandError.Unsuccessful, "The user's bodypart is already that form.");
				}

				currentComponent.Transformation = transformation;

				shiftMessage = this.DescriptionBuilder.BuildShiftMessage(character, currentComponent);
			}

			await db.SaveChangesAsync();

			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the colour of the given bodypart on the given character to the given colour.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="colour">The colour to shift it into.</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartColourAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] Colour colour,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.TryGetAppearanceComponent(bodyPart, chirality, out var currentComponent))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			if (currentComponent.BaseColour == colour)
			{
				return ShiftBodypartResult.FromError(CommandError.Unsuccessful, "The bodypart is already that colour.");
			}

			var originalColour = currentComponent.BaseColour;
			currentComponent.BaseColour = colour;

			await db.SaveChangesAsync();

			string shiftMessage = this.DescriptionBuilder.BuildColourShiftMessage(character, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the pattern of the given bodypart on the given character to the given pattern with the given colour.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="pattern">The pattern to shift the bodypart into.</param>
		/// <param name="patternColour">The colour to shift it into.</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftBodypartPatternAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			Pattern pattern,
			[NotNull] Colour patternColour,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.TryGetAppearanceComponent(bodyPart, chirality, out var currentComponent))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			if (currentComponent.Pattern == pattern)
			{
				return ShiftBodypartResult.FromError(CommandError.Unsuccessful, "The character already has that pattern.");
			}

			var originalPattern = currentComponent.Pattern;
			var originalColour = currentComponent.BaseColour;

			currentComponent.Pattern = pattern;
			currentComponent.PatternColour = patternColour;

			await db.SaveChangesAsync();

			string shiftMessage = this.DescriptionBuilder.BuildPatternShiftMessage(character, originalPattern, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Shifts the colour of the given bodypart's pattern on the given character to the given colour.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="context">The command context.</param>
		/// <param name="character">The character to shift.</param>
		/// <param name="bodyPart">The bodypart to shift.</param>
		/// <param name="patternColour">The colour to shift it into.</param>
		/// <param name="chirality">The chirality of the bodypart.</param>
		/// <returns>A shifting result which may or may not have succeeded.</returns>
		public async Task<ShiftBodypartResult> ShiftPatternColourAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] ICommandContext context,
			[NotNull] Character character,
			Bodypart bodyPart,
			[NotNull] Colour patternColour,
			Chirality chirality = Chirality.Center
		)
		{
			var discordUser = await context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
			var canTransformResult = await CanUserTransformUserAsync(db, context.Guild, context.User, discordUser);
			if (!canTransformResult.IsSuccess)
			{
				return ShiftBodypartResult.FromError(canTransformResult);
			}

			if (!character.TryGetAppearanceComponent(bodyPart, chirality, out var currentComponent))
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The character doesn't have that bodypart.");
			}

			if (!currentComponent.Pattern.HasValue)
			{
				return ShiftBodypartResult.FromError(CommandError.ObjectNotFound, "The bodypart doesn't have a pattern.");
			}

			if (currentComponent.PatternColour == patternColour)
			{
				return ShiftBodypartResult.FromError(CommandError.Unsuccessful, "The pattern is already that colour.");
			}

			var originalColour = currentComponent.PatternColour;
			currentComponent.PatternColour = patternColour;

			await db.SaveChangesAsync();

			// ReSharper disable once AssignNullToNotNullAttribute - Having a pattern implies having a pattern colour
			string shiftMessage = this.DescriptionBuilder.BuildPatternColourShiftMessage(character, originalColour, currentComponent);
			return ShiftBodypartResult.FromSuccess(shiftMessage);
		}

		/// <summary>
		/// Determines whether or not a user is allowed to transform another user.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordServer">The server the users are on.</param>
		/// <param name="invokingUser">The user trying to transform.</param>
		/// <param name="targetUser">The user being transformed.</param>
		/// <returns>A conditional determination with an attached reason if it failed.</returns>
		[Pure]
		public async Task<DetermineConditionResult> CanUserTransformUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IGuild discordServer,
			[NotNull] IUser invokingUser,
			[NotNull] IUser targetUser
		)
		{
			var localProtection = await GetOrCreateServerUserProtectionAsync(db, targetUser, discordServer);
			if (!localProtection.HasOptedIn)
			{
				return DetermineConditionResult.FromError("The target hasn't opted into transformations.");
			}

			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(db, targetUser);
			switch (localProtection.Type)
			{
				case ProtectionType.Blacklist:
				{
					return globalProtection.Blacklist.All(u => u.DiscordID != (long)invokingUser.Id)
						? DetermineConditionResult.FromSuccess()
						: DetermineConditionResult.FromError("You're on that user's blacklist.");
				}
				case ProtectionType.Whitelist:
				{
					return globalProtection.Whitelist.Any(u => u.DiscordID == (long)invokingUser.Id)
						? DetermineConditionResult.FromSuccess()
						: DetermineConditionResult.FromError("You're not on that user's whitelist.");
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Generate a complete textual description of the given character, and format it into an embed.
		/// </summary>
		/// <param name="context">The context of the generation.</param>
		/// <param name="character">The character to generate the description for.</param>
		/// <returns>An embed with a formatted description.</returns>
		[Pure]
		public async Task<Embed> GenerateCharacterDescriptionAsync
		(
			[NotNull] ICommandContext context,
			[NotNull] Character character
		)
		{
			var eb = new EmbedBuilder();
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle($"{character.Name} {(character.Nickname is null ? string.Empty : $"\"{character.Nickname}\"")}".Trim());

			var user = await context.Client.GetUserAsync((ulong)character.Owner.DiscordID);
			eb.WithAuthor(user);

			eb.WithThumbnailUrl
			(
				!character.AvatarUrl.IsNullOrWhitespace()
					? character.AvatarUrl
					: this.Content.DefaultAvatarUri.ToString()
			);

			eb.AddField("Description", character.Description);

			string visualDescription = this.DescriptionBuilder.BuildVisualDescription(character);
			eb.WithDescription(visualDescription);

			return eb.Build();
		}

		/// <summary>
		/// Gets the available species in transformations.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <returns>A list of the available species.</returns>
		[Pure]
		public async Task<IReadOnlyList<Species>> GetAvailableSpeciesAsync([NotNull] GlobalInfoContext db)
		{
			return await db.Species
				.Include(s => s.Parent)
				.ToListAsync();
		}

		/// <summary>
		/// Gets the available transformations for the given bodypart.
		/// </summary>
		/// <param name="db">The database containing the transformations.</param>
		/// <param name="bodyPart">The bodypart to get the transformations for.</param>
		/// <returns>A list of the available transformations..</returns>
		[Pure]
		public async Task<IReadOnlyList<Transformation>> GetAvailableTransformationsAsync
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodyPart
		)
		{
			return await db.Transformations
				.Include(tf => tf.Species)
				.Where(tf => tf.Part == bodyPart).ToListAsync();
		}

		/// <summary>
		/// Resets the given character's appearance to its default state.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to reset.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> ResetCharacterFormAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character
		)
		{
			if (character.DefaultAppearance is null)
			{
				return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "The character has no default appearance.");
			}

			character.CurrentAppearance = character.DefaultAppearance;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the current appearance of the given character as its default appearance.
		/// </summary>
		/// <param name="db">The database containing the characters.</param>
		/// <param name="character">The character to set the default appearance of.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetCurrentAppearanceAsDefaultForCharacterAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] Character character
		)
		{
			if (character.CurrentAppearance is null)
			{
				return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "The character doesn't have an altered appearance.");
			}

			character.DefaultAppearance = character.CurrentAppearance;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the default protection type that the user has for transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to set the protection for.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetDefaultProtectionTypeAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			ProtectionType protectionType
		)
		{
			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.DefaultType == protectionType)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"{protectionType.Humanize()} is already your default setting.");
			}

			protection.DefaultType = protectionType;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Sets the protection type that the user has for transformations on the given server.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to set the protection for.</param>
		/// <param name="discordServer">The server to set the protection on.</param>
		/// <param name="protectionType">The protection type to set.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> SetServerProtectionTypeAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IGuild discordServer,
			ProtectionType protectionType
		)
		{
			var protection = await GetOrCreateServerUserProtectionAsync(db, discordUser, discordServer);
			if (protection.Type == protectionType)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"{protectionType.Humanize()} is already your current setting.");
			}

			protection.Type = protectionType;
			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Whitelists the given user, allowing them to transform the <paramref name="discordUser"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to modify.</param>
		/// <param name="whitelistedUser">The user to add to the whitelist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> WhitelistUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IUser whitelistedUser
		)
		{
			if (discordUser == whitelistedUser)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You can't whitelist yourself.");
			}

			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Whitelist.Any(u => u.DiscordID == (long)whitelistedUser.Id))
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You've already whitelisted that user.");
			}

			var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == (long)discordUser.Id);
			if (protectionEntry is null)
			{
				var user = await db.GetOrRegisterUserAsync(whitelistedUser);
				protectionEntry = new UserProtectionEntry
				{
					GlobalProtection = protection,
					User = user,
					Type = ListingType.Whitelist
				};

				protection.UserListing.Add(protectionEntry);
			}
			else
			{
				protectionEntry.Type = ListingType.Whitelist;
			}

			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Blacklists the given user, preventing them from transforming the <paramref name="discordUser"/>.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user to modify.</param>
		/// <param name="blacklistedUser">The user to add to the blacklist.</param>
		/// <returns>An entity modification result which may or may not have succeeded.</returns>
		public async Task<ModifyEntityResult> BlacklistUserAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IUser blacklistedUser
		)
		{
			if (discordUser == blacklistedUser)
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You can't blacklist yourself.");
			}

			var protection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			if (protection.Blacklist.Any(u => u.DiscordID == (long)blacklistedUser.Id))
			{
				return ModifyEntityResult.FromError(CommandError.Unsuccessful, "You've already blacklisted that user.");
			}

			var protectionEntry = protection.UserListing.FirstOrDefault(u => u.User.DiscordID == (long)discordUser.Id);
			if (protectionEntry is null)
			{
				var user = await db.GetOrRegisterUserAsync(blacklistedUser);
				protectionEntry = new UserProtectionEntry
				{
					GlobalProtection = protection,
					User = user,
					Type = ListingType.Blacklist
				};

				protection.UserListing.Add(protectionEntry);
			}
			else
			{
				protectionEntry.Type = ListingType.Blacklist;
			}

			await db.SaveChangesAsync();

			return ModifyEntityResult.FromSuccess(ModifyEntityAction.Edited);
		}

		/// <summary>
		/// Gets or creates the global transformation protection data for the given user.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <returns>Global protection data for the given user.</returns>
		[ItemNotNull]
		public async Task<GlobalUserProtection> GetOrCreateGlobalUserProtectionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser
		)
		{
			var protection = await db.GlobalUserProtections
			.Include(p => p.User)
			.Include(p => p.UserListing)
			.FirstOrDefaultAsync(p => p.User.DiscordID == (long)discordUser.Id);

			if (!(protection is null))
			{
				return protection;
			}

			var user = await db.GetOrRegisterUserAsync(discordUser);
			protection = GlobalUserProtection.CreateDefault(user);

			await db.GlobalUserProtections.AddAsync(protection);
			await db.SaveChangesAsync();

			return protection;
		}

		/// <summary>
		/// Gets or creates server-specific transformation protection data for the given user and server.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="discordUser">The user.</param>
		/// <param name="guild">The server.</param>
		/// <returns>Server-specific protection data for the given user.</returns>
		[ItemNotNull]
		public async Task<ServerUserProtection> GetOrCreateServerUserProtectionAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] IUser discordUser,
			[NotNull] IGuild guild
		)
		{
			var protection = await db.ServerUserProtections
			.Include(p => p.Server)
			.Include(p => p.User)
			.FirstOrDefaultAsync
			(
				p =>
					p.User.DiscordID == (long)discordUser.Id && p.Server.DiscordID == (long)guild.Id
			);

			if (!(protection is null))
			{
				return protection;
			}

			var server = await db.GetOrRegisterServerAsync(guild);
			var globalProtection = await GetOrCreateGlobalUserProtectionAsync(db, discordUser);
			protection = ServerUserProtection.CreateDefault(globalProtection, server);

			await db.ServerUserProtections.AddAsync(protection);
			await db.SaveChangesAsync();

			return protection;
		}

		/// <summary>
		/// Updates the database with new or changed transformations.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <returns>An update result which may or may not have succeeded.</returns>
		public async Task<UpdateTransformationsResult> UpdateTransformationDatabaseAsync
		(
			[NotNull] GlobalInfoContext db
		)
		{
			uint addedSpecies = 0;
			uint updatedSpecies = 0;

			var bundledSpeciesResult = await this.Content.DiscoverBundledSpeciesAsync();
			if (!bundledSpeciesResult.IsSuccess)
			{
				return UpdateTransformationsResult.FromError(bundledSpeciesResult);
			}

			foreach (var species in bundledSpeciesResult.Entity.OrderBy(s => s.GetSpeciesDepth()))
			{
				if (await IsSpeciesNameUniqueAsync(db, species.Name))
				{
					// Add a new specices
					db.Species.Add(species);
					++addedSpecies;
				}
				else
				{
					// There's an existing species with this name
					var existingSpecies = (await GetSpeciesByNameAsync(db, species.Name)).Entity;

					int updatedFields = 0;

					// Update its fields with the info in the bundled species
					if (!existingSpecies.Description.Equals(species.Description, StringComparison.OrdinalIgnoreCase))
					{
						existingSpecies.Description = species.Description;
						++updatedFields;
					}

					// The extra reference equality check is due to the fact that the parent can be null
					bool shouldUpdateParent =
						(existingSpecies.Parent is null ^ species.Parent is null)
						|| (!(existingSpecies.Parent is null) && !existingSpecies.Parent.IsSameSpeciesAs(species.Parent));

					if (shouldUpdateParent)
					{
						existingSpecies.Parent = species.Parent;
						++updatedFields;
					}

					if (updatedFields > 0)
					{
						++updatedSpecies;
					}
				}

				await db.SaveChangesAsync();
			}

			uint addedTransformations = 0;
			uint updatedTransformations = 0;

			var availableSpecies = await GetAvailableSpeciesAsync(db);
			foreach (var species in availableSpecies)
			{
				var bundledTransformationsResult = await this.Content.DiscoverBundledTransformationsAsync(db, this, species);
				if (!bundledTransformationsResult.IsSuccess)
				{
					return UpdateTransformationsResult.FromError(bundledTransformationsResult);
				}

				foreach (var transformation in bundledTransformationsResult.Entity)
				{
					if (await IsPartAndSpeciesCombinationUniqueAsync(db, transformation.Part, transformation.Species))
					{
						// Add a new transformation
						db.Transformations.Add(transformation);
						++addedTransformations;
					}
					else
					{
						var existingTransformation = (await GetTransformationByPartAndSpeciesAsync(db, transformation.Part, transformation.Species)).Entity;

						int updatedFields = 0;

						if (!existingTransformation.Description.Equals(transformation.Description, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.Description = transformation.Description;
							++updatedFields;
						}

						if (existingTransformation.IsNSFW != transformation.IsNSFW)
						{
							existingTransformation.IsNSFW = transformation.IsNSFW;
							++updatedFields;
						}

						if (!existingTransformation.ShiftMessage.Equals(transformation.ShiftMessage, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.ShiftMessage = transformation.ShiftMessage;
							++updatedFields;
						}

						if (!existingTransformation.GrowMessage.Equals(transformation.GrowMessage, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.GrowMessage = transformation.GrowMessage;
							++updatedFields;
						}

						if (!existingTransformation.SingleDescription.Equals(transformation.SingleDescription, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.SingleDescription = transformation.SingleDescription;
							++updatedFields;
						}

						if (existingTransformation.UniformDescription != null && !existingTransformation.UniformDescription.Equals(transformation.UniformDescription, StringComparison.OrdinalIgnoreCase))
						{
							existingTransformation.UniformDescription = transformation.UniformDescription;
							++updatedFields;
						}

						if (updatedFields > 0)
						{
							++updatedTransformations;
						}
					}

					await db.SaveChangesAsync();
				}
			}

			return UpdateTransformationsResult.FromSuccess(addedSpecies, addedTransformations, updatedSpecies, updatedTransformations);
		}

		/// <summary>
		/// Gets a transformation from the database by its part and species.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="bodypart">The part.</param>
		/// <param name="species">The species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<Transformation>> GetTransformationByPartAndSpeciesAsync
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodypart,
			[NotNull] Species species
		)
		{
			var transformation = await db.Transformations
				.Include(tf => tf.DefaultBaseColour)
				.Include(tf => tf.DefaultPatternColour)
				.Include(tf => tf.Species)
				.FirstOrDefaultAsync(tf => tf.Part == bodypart && tf.Species.IsSameSpeciesAs(species));

			if (transformation is null)
			{
				return RetrieveEntityResult<Transformation>.FromError(CommandError.ObjectNotFound, "No transformation found for that combination.");
			}

			return RetrieveEntityResult<Transformation>.FromSuccess(transformation);
		}

		/// <summary>
		/// Determines whether a combination of a part and a species is a unique transformation.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="bodypart">The bodypart that is transformed.</param>
		/// <param name="species">The species to transform into.</param>
		/// <returns>true if the combination is unique; otherwise, false.</returns>
		[Pure]
		public async Task<bool> IsPartAndSpeciesCombinationUniqueAsync
		(
			[NotNull] GlobalInfoContext db,
			Bodypart bodypart,
			[NotNull] Species species
		)
		{
			return !await db.Transformations.AnyAsync(tf => tf.Part == bodypart && tf.Species.IsSameSpeciesAs(species));
		}

		/// <summary>
		/// Gets the species from the database with the given name.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public RetrieveEntityResult<Species> GetSpeciesByName
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			return GetSpeciesByNameAsync(db, speciesName).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Gets the species from the database with the given name.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>A retrieval result which may or may not have succeeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<Species>> GetSpeciesByNameAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			var species = await db.Species.FirstOrDefaultAsync(s => string.Equals(s.Name, speciesName, StringComparison.OrdinalIgnoreCase));
			if (species is null)
			{
				return RetrieveEntityResult<Species>.FromError(CommandError.ObjectNotFound, "There is no species with that name in the database.");
			}

			return RetrieveEntityResult<Species>.FromSuccess(species);
		}

		/// <summary>
		/// Determines whether or not the given species name is unique. This method is case-insensitive.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="speciesName">The name of the species.</param>
		/// <returns>true if the name is unique; otherwise, false.</returns>
		[Pure]
		public async Task<bool> IsSpeciesNameUniqueAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] string speciesName
		)
		{
			return !await db.Species.AnyAsync(s => string.Equals(s.Name, speciesName, StringComparison.OrdinalIgnoreCase));
		}
	}
}
