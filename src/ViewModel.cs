﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace ProjectSpotlight
{
	public class ViewModel
	{
		#region Fields
		// Static data.
		private static readonly string rootDirectory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
				"Spotlight");
		#endregion Fields


		#region Properties
		public ObservableCollection<Model> NewItems { get; } = new();
		#endregion Properties


		#region Methods
		public void Scavenge()
		{
			// 0. Locals.
			// 0.1. Paths.
			string source_directory =
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
				+ @"\AppData\Local\Packages\Microsoft.Windows.ContentDeliveryManager_cw5n1h2txyewy\LocalState\Assets";
#if DEBUG
			string historyFilePath = Environment.CurrentDirectory + @"\..\Release\history.txt";
#else
			string historyFilePath = Environment.CurrentDirectory + @"\history.txt";
#endif

			// 0.2. Makes sure the temporary directory exists.
			if (!Directory.Exists(rootDirectory))
				Directory.CreateDirectory(rootDirectory);


			// 1. Opens the history file for reading and writing.
			FileInfo historyFile = new(historyFilePath);
			List<string> history = new();
			// 'historyFile' will be used in section 3 below, so I'm not going to detroy it yet.

			if (historyFile.Exists)
			{
				using StreamReader reader = new(historyFile.OpenRead());
				history.AddRange(reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
				reader.Close();
			}
			else
				historyFile.Create();


			// 2. Copies spotlight images to a temporary folder.
			// 2.1. Gets a list of all files in the directory used by the system to store spotlight images.
			DirectoryInfo SourceDirectory = new(source_directory);
			FileInfo[] files = SourceDirectory.GetFiles();

			// 2.2. Analyzes each file and copies those which are spotlight image.
			foreach (var file in files)
			{
				// 2.2.1. Opens the current file and reads the first 3 characters(bytes) from the file
				//       to determine whether this a JPEG image file.
				using FileStream fs = File.OpenRead(file.FullName);
				using BinaryReader br = new(fs);
				byte[] buffer = br.ReadBytes(3);
				br.Close();
				fs.Close();

				if (buffer[0] != 0xFF || buffer[1] != 0XD8 || buffer[2] != 0XFF)
					continue;

				// 2.2.2. Checks whether the current image is in the history.
				if (history.Contains(file.Name))
					continue;

				// 2.2.3 Also ignores the current image if it is already in the temporary directory.
				string newFilePath = $@"{rootDirectory}\{file.Name}.jpg";

				if (File.Exists(newFilePath))
					continue;

				// 2.2.4. Saves this file to a list and copies it to the temporary directory.
				file.CopyTo(newFilePath);
				NewItems.Add(new(newFilePath));
			}


			// 3. Appends the new files to the file history.
			if (NewItems.Count != 0)
			{
				StringBuilder additional_text = new();

				foreach (Model item in NewItems)
					additional_text.AppendLine(Path.GetFileNameWithoutExtension(item.FilePath));

				using (StreamWriter writer = historyFile.AppendText())
				{
					writer.Write(additional_text.ToString());
					writer.Close();
				}
			}
		}

		public void SimulateScavenge()
		{
			// Simulates Scavenge()
			var fileNames = Directory.GetFiles(rootDirectory, "*.jpg");

			foreach (string fileName in fileNames)
				NewItems.Add(new(fileName));
		}

		public void GroupImagesByAspectRation()
		{
			List<Tuple<Model, Exception>> fails = new();

			Model[] items = new Model[NewItems.Count];
			NewItems.CopyTo(items, 0);
			Random random = new();

			foreach (Model item in items)
			{
				// Skips this file if it no longer exists.
				if (!File.Exists(item.FilePath))
					continue;

				// Computes the new path for the current file.
				string directory = item.ImageOrientation == ImageOrientation.Landscape ? "Landscape" : "Portrait";
				string directoryPath = Path.Combine(rootDirectory, directory);

				if (!Directory.Exists(directoryPath))
					try
					{
						Directory.CreateDirectory(directoryPath);
					}
					catch (Exception ex)
					{
						fails.Add(new(item, ex));
					}

				// Computes the new path for the current file.
				string fileName = Path.GetFileName(item.FilePath);
				string newFilePath = Path.Combine(directoryPath, fileName);

				// Moves the current file to the apropriate directory.
				try
				{
#if DEBUG
					if (random.NextDouble() < 0.7)
						throw new Exception("This file is being used by another program.");
#else
					File.Move(item.FilePath, newFilePath);
#endif
					NewItems.Remove(item);
				}
				catch (Exception ex)
				{
					fails.Add(new(item, ex));
				}
			}


			if (fails.Count != 0)
			{
				Exception exception = new();
				exception.Data.Add("fails", fails);
				throw exception;
			}
		}

		public void RemoveItem(Model item, bool removeFile)
		{
			if (item == null || !NewItems.Contains(item))
				return;

			//
			NewItems.Remove(item);

			//
			if (removeFile)
				FileSystem.DeleteFile(item.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
		}
		#endregion Methods
	}
}