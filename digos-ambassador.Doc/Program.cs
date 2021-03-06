﻿//
//  Program.cs
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

using System.Reflection;
using System.Threading.Tasks;
using CommandLine;

namespace DIGOS.Ambassador.Doc
{
	/// <summary>
	/// The main program class.
	/// </summary>
	internal static class Program
	{
		private static Options Options;

		private static async Task Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(r => Options = r);

			var assembly = Assembly.LoadFrom(Options.AssemblyPath);

			var generator = new ModuleDocumentationGenerator(assembly, Options.OutputPath);
			await generator.GenerateDocumentationAsync();
		}
	}
}
