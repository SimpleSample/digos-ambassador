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

using System;
using System.IO;
using CommandLine;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;

using Discord.Commands;
using YamlDotNet.Core;
using Parser = CommandLine.Parser;

namespace DIGOS.Ambassador.Tools
{
	/// <summary>
	/// The main class.
	/// </summary>
	internal static class Program
	{
		private static CommandLineOptions Options;

		private static int Main(string[] args)
		{
			Parser.Default.ParseArguments<CommandLineOptions>(args)
			.WithParsed(r => Options = r);

			var verifier = new TransformationFileVerifier();

			var path = Path.GetFullPath(Options.VerifyPath);
			DetermineConditionResult verifyResult;
			if (File.Exists(path))
			{
				// run file verification
				verifyResult = verifier.VerifyFile<Transformation>(path);
				if (!verifyResult.IsSuccess)
				{
					verifyResult = verifier.VerifyFile<Species>(path);
				}
			}
			else if (Directory.Exists(path))
			{
				// run directory verification
				verifyResult = verifier.VerifyFilesInDirectory(path);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"Input not found.");
				return -2;
			}

			if (!verifyResult.IsSuccess)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				if (verifyResult.Error == CommandError.Exception)
				{
					Console.WriteLine($"File \"{verifyResult.ErrorReason}\" failed verification.");

					var yamlException = (YamlException)verifyResult.Exception ?? throw new ArgumentNullException();
					var errorMessage = (yamlException.InnerException is null)
						? yamlException.Message
						: yamlException.InnerException.Message;

					Console.WriteLine($"Error at {yamlException.Start}: {errorMessage}");

					return -1;
				}

				Console.WriteLine(verifyResult.ErrorReason);
				return 3;
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"Target file(s) look fine.");
			return 0;
		}
	}
}
