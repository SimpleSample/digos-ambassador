﻿//
//  AppearanceComponent.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Transformations;
using Humanizer;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Transformations.Chirality;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents a distinct part of a character's appearance.
	/// </summary>
	public class AppearanceComponent : IEFEntity
	{
		/// <inheritdoc />
		public long ID { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AppearanceComponent"/> class.
		/// </summary>
		private AppearanceComponent()
		{
			this.BaseColour = new Colour();
		}

		/// <summary>
		/// Gets the bodypart that the component is.
		/// </summary>
		[NotMapped]
		public Bodypart Bodypart => this.Transformation.Part;

		/// <summary>
		/// Gets or sets the component's current transformation.
		/// </summary>
		public Transformation Transformation { get; set; }

		/// <summary>
		/// Gets or sets the chirality of the component.
		/// </summary>
		public Chirality Chirality { get; set; }

		/// <summary>
		/// Gets or sets the base colour of the component.
		/// </summary>
		[NotNull]
		public Colour BaseColour { get; set; }

		/// <summary>
		/// Gets or sets the pattern of the component's secondary colour (if any)
		/// </summary>
		[CanBeNull]
		public Pattern? Pattern { get; set; }

		/// <summary>
		/// Gets or sets the component's pattern colour.
		/// </summary>
		[CanBeNull]
		public Colour PatternColour { get; set; }

		/// <summary>
		/// Gets or sets the size of the component. This is, by default, a unitless value and is only contextually relevant.
		/// </summary>
		public int Size { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return
				$"{(this.Chirality == Center ? string.Empty : $"{this.Chirality.Humanize()} ")}{this.Bodypart} ({this.Transformation.Species.Name})";
		}

		/// <summary>
		/// Creates a new <see cref="AppearanceComponent"/> from a transformation of a bodypart.
		/// </summary>
		/// <param name="transformation">The transformation.</param>
		/// <param name="chirality">The chirality of the transformation, if any.</param>
		/// <returns>A new component.</returns>
		[Pure]
		[NotNull]
		public static AppearanceComponent CreateFrom([NotNull] Transformation transformation, Chirality chirality = Center)
		{
			if (transformation.Part.IsChiral() && chirality == Center)
			{
				throw new ArgumentException("A chiral transformation requires you to specify the chirality.", nameof(transformation));
			}

			if (!transformation.Part.IsChiral() && chirality != Center)
			{
				throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(transformation));
			}

			return new AppearanceComponent
			{
				Transformation = transformation,
				Chirality = chirality,
				BaseColour = transformation.DefaultBaseColour,
				Pattern = transformation.DefaultPattern,
				PatternColour = transformation.DefaultPatternColour
			};
		}

		/// <summary>
		/// Creates a set of chiral appearance components from a chiral transformation.
		/// </summary>
		/// <param name="transformation">The transformation.</param>
		/// <returns>A set of appearance components.</returns>
		[ItemNotNull]
		[Pure]
		[NotNull]
		public static IEnumerable<AppearanceComponent> CreateFromChiral([NotNull] Transformation transformation)
		{
			if (!transformation.Part.IsChiral())
			{
				throw new ArgumentException("The transformation was not chiral.", nameof(transformation));
			}

			var chiralities = new[] { Left, Right };

			foreach (var chirality in chiralities)
			{
				yield return new AppearanceComponent
				{
					Transformation = transformation,
					Chirality = chirality,
					BaseColour = transformation.DefaultBaseColour,
					Pattern = transformation.DefaultPattern,
					PatternColour = transformation.DefaultPatternColour
				};
			}
		}
	}
}
