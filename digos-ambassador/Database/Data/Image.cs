﻿//
//  Image.cs
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

namespace DIGOS.Ambassador.Database.Data
{
	/// <summary>
	/// Represents an image.
	/// </summary>
	public class Image
	{
		/// <summary>
		/// Gets or sets the unique ID of the image.
		/// </summary>
		public uint ImageID { get; set; }

		/// <summary>
		/// Gets or sets the caption of the image.
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the image is NSFW.
		/// </summary>
		public bool IsNSFW { get; set; }

		/// <summary>
		/// Gets or sets the path to the image on disk.
		/// </summary>
		public string Path { get; set; }
	}
}
