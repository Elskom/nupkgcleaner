// Copyright (c) 2021, Elskom org.
// https://github.com/Elskom/
// All rights reserved.
// license: MIT, see LICENSE for more details.

namespace Nupkgcleaner
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.CommandLine.IO;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    internal static class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            var cmd = new RootCommand
            {
                new Option("--version", "Shows the version of this command-line program."),
                new Argument<string>("filename", "The nuget package to clean. Looks for the file name recursively within the current directory that the command is invoked from."),
            }.WithHandler(nameof(GlobalCommandHandler));
            return await cmd.InvokeAsync(args);
        }

        internal static int GlobalCommandHandler(bool version, string filename, IConsole console)
        {
            if (version)
            {
                console.Out.WriteLine($"{Assembly.GetEntryAssembly().GetName().Version}");
                return 0;
            }
            else
            {
                if (string.IsNullOrEmpty(filename) || string.IsNullOrWhiteSpace(filename))
                {
                    console.Error.WriteLine("Error: file name was not provided. 😭");
                    return 1;
                }

                try
                {
                    var path = new StringBuilder();
                    Directory.GetFiles(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
                        .Where(p => p.EndsWith(filename, StringComparison.Ordinal)).ToList()
                        .ForEach(p => { _ = path.Append(new FileInfo(p).Directory.FullName); });
                    if (string.IsNullOrEmpty(path.ToString()))
                    {
                        console.Error.WriteLine("Error: File was not found. 😭");
                        return 1;
                    }

                    try
                    {
                        using (var archive = ZipFile.Open(path.ToString(), ZipArchiveMode.Update))
                        {
                            archive.Entries.Where(x => x.FullName.Contains("trash")).ToList().ForEach(y => { archive.GetEntry(y.FullName).Delete(); });
                        }

                        return 0;
                    }
                    catch
                    {
                        console.Error.WriteLine("Error: Zip file could not be opened, or there was an error finding or deleting files to be cleaned. 😭");
                        return 1;
                    }
                }
                catch
                {
                    console.Error.WriteLine("Error: Failed to recursively look for the file. 😭");
                    return 1;
                }
            }
        }

        private static Command WithHandler(this Command command, string name)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            var method = typeof(Program).GetMethod(name, flags);
            if (method != null)
            {
                command.Handler = CommandHandler.Create(method);
            }

            return command;
        }
    }
}
