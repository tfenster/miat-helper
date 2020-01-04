﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace client
{
    class Program
    {

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().AddCommandLine(args);
            var config = builder.Build();

            var path = config["folder"];
            if (path == null) {
                Console.WriteLine("Don't know which directory to monitor, please provide configuration param --folder");
                Environment.Exit(1);
            }
            if (! Directory.Exists(path)) {
                Console.WriteLine($"Path {path} doesn't exist");
                Environment.Exit(1);
            }

            var guid = Guid.NewGuid();
            var requestPath = $"{path}\\{guid.ToString()}.request";
            using (FileSystemWatcher watcher = new FileSystemWatcher()) {
                watcher.Path = path;
                watcher.Filter = $"{guid.ToString()}.response";
                watcher.Created += OnCreated;
                watcher.EnableRaisingEvents = true;
                File.WriteAllText(requestPath, "");

                while (Console.Read() != 'q') ;
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(100);
            var tokenBytes = File.ReadAllBytes(e.FullPath);
            File.Delete(e.FullPath);
            var accessToken = Encoding.UTF8.GetString(tokenBytes);
            Console.WriteLine("{ \"AccessToken\":\"" + accessToken + "\" }");
            Environment.Exit(0);
        }
    }
}