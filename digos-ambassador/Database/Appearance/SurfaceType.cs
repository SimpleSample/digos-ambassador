﻿//
//  SurfaceType.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
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

namespace DIGOS.Ambassador.Database.Appearance
{
	/// <summary>
	/// Represents different types of appearance surfaces.
	/// </summary>
	public enum SurfaceType
	{
		/// <summary>
		/// Plain skin.
		/// </summary>
		Skin,

		/// <summary>
		/// Furry skin or hide.
		/// </summary>
		Fur,

		/// <summary>
		/// Scaled skin or hide.
		/// </summary>
		Scales
	}
}
