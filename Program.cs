using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace CoreProjectGenerator
{
	class Program
	{
		private static string _projectName;
		private static string _destinyFolder;
		private static string AbsoluteProjectPath => $"{_destinyFolder}/{_projectName}";

		private static string ZipProjectName => "master.zip";
		private static string UnzipedProjectName => "DefaultProject-master";
		private static string DefaultProjectName => "DefaultProject";

		public static void Main(string[] args)
		{
			var app = new CommandLineApplication();

			app.Command("generate", cfg =>
			{
				cfg.Description = "--[projectName] --[destinyFolder]";
				var name = cfg.Option("--name", "Project name without spaces", CommandOptionType.SingleValue);
				var path = cfg.Option("--path", "Absolute path to generate application", CommandOptionType.SingleValue);			
				
					cfg.OnExecute(() =>
					{
						_projectName = name.Value();
						_destinyFolder = path.Value();
						return GenerateProject(app);
					});
			});
			app.HelpOption("-? | -h | --help");
			app.Execute(args);
		}

		private static async Task<int> GenerateProject(CommandLineApplication app)
		{
			try
			{
				await DownloadTemplate();
				await ConfigureProject();
				return 1;
			}
			catch (Exception e)
			{
				app.ShowHelp();
				return 0;
			}
		}
		
		private static async Task DownloadTemplate()
		{
			var templateUrl = "https://github.com/luizanac/DefaultProject/archive/master.zip";
			using (var httpClient = new HttpClient())
			{
				var result = await httpClient.GetAsync(templateUrl);
				var destinyZipFolder = $"{_destinyFolder}/{ZipProjectName}";
				using (var fs = new FileStream(destinyZipFolder, FileMode.CreateNew))
				{
					await result.Content.ReadAsStreamAsync().Result.CopyToAsync(fs);
					ZipFile.ExtractToDirectory(destinyZipFolder, _destinyFolder);
				}
			}
		}

		private static async Task ConfigureProject()
		{
			var directoryInfo = new DirectoryInfo(_destinyFolder);
			foreach (var file in directoryInfo.GetFiles())
			{
				if(file.Name.Equals(ZipProjectName))
					file.Delete();
			}

			Directory.Move($"{_destinyFolder}/{UnzipedProjectName}", $"{_destinyFolder}/{_projectName}");

			Directory.GetDirectories(AbsoluteProjectPath);
			await ScanAndModifyProjectFolders();
			await ScanAndModifyProjectFiles();

		}

		private static async Task ScanAndModifyProjectFolders(string path = null)
		{
			if (path == null)
				path = AbsoluteProjectPath;
			
			foreach (var subDirectory in Directory.GetDirectories(path))
			{
				if (Directory.GetDirectories(subDirectory).GetEnumerator() != null)
				{
					await ScanAndModifyProjectFolders(subDirectory);
					if(subDirectory.Split("/").Last().Contains(DefaultProjectName))
						Directory.Move(subDirectory, subDirectory.Replace(DefaultProjectName, _projectName));
				}	
			}
		}
		
		private static async Task ScanAndModifyProjectFiles(string path = null)
		{
			if (path == null)
				path = AbsoluteProjectPath;

			foreach (var file in Directory.GetFiles(path))
			{
				await File.WriteAllTextAsync(file, File.ReadAllTextAsync(file).Result.Replace(DefaultProjectName, _projectName));
				if(file.Split("/").Last().Contains(DefaultProjectName))
					File.Move(file, file.Replace(DefaultProjectName, _projectName));
			}
			
			foreach (var subDirectory in Directory.GetDirectories(path))
			{
				await ScanAndModifyProjectFiles(subDirectory);
			}
		}
	}
}