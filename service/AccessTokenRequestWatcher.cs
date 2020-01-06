using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Security.Principal;
using System.Security.AccessControl;

namespace service
{
    public class AccessTokenRequestWatcher : BackgroundService
    {
        private readonly ILogger<AccessTokenRequestWatcher> _logger;
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;
        private readonly static Random random = new Random();

        public AccessTokenRequestWatcher(ILogger<AccessTokenRequestWatcher> logger, IConfiguration config, IHostEnvironment env)
        {
            _logger = logger;
            _config = config;
            _env = env;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FileSystemWatcher started at: {time}", DateTimeOffset.Now);
            var path = _config.GetValue<string>("folder", "unset");
            if (path == "unset")
            {
                _logger.LogError("Don't know which directory to monitor, please provide configuration param --folder");
                Environment.Exit(1);
            }
            if (!Directory.Exists(path))
            {
                _logger.LogError($"Path {path} doesn't exist");
                Environment.Exit(1);
            }

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = path;
                watcher.Filter = "*.request";
                watcher.Created += OnCreated;
                watcher.EnableRaisingEvents = true;
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }

        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            var requestedResource = "";
            var retryCount = 0;
            while (retryCount < 3) {
                try {
                    requestedResource = File.ReadAllText(e.FullPath);
                    _logger.LogInformation("Got a request for " + requestedResource);
                    File.Delete(e.FullPath);
                } catch (IOException) {
                    // might be blocked by the client or something else
                    Thread.Sleep(1000);
                } finally {
                    retryCount++;
                }
            }

            if (requestedResource != "") {
                var accessToken = GetToken(requestedResource);
                if (accessToken != null)
                {
                    _logger.LogInformation("Got a token");
                    var tokenBytes = Encoding.UTF8.GetBytes(accessToken);
                    var inProgressPath = e.FullPath.Substring(0, e.FullPath.LastIndexOf(".")) + ".inprogress";
                    var resultPath = e.FullPath.Substring(0, e.FullPath.LastIndexOf(".")) + ".response";
                    File.WriteAllBytes(inProgressPath, tokenBytes);
                    File.Copy(inProgressPath, resultPath);
                    GrantAccess(resultPath);
                    File.Delete(inProgressPath);
                }
            }
        }

        private string GetToken(string requestedResource)
        {
            _logger.LogInformation($"token for resource {requestedResource} requested");
            if (_env.IsProduction())
            {
                var request = (HttpWebRequest)WebRequest.Create($"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={requestedResource}");
                request.Headers["Metadata"] = "true";
                request.Method = "GET";

                try
                {
                    var response = (HttpWebResponse)request.GetResponse();

                    var streamResponse = new StreamReader(response.GetResponseStream());
                    var stringResponse = streamResponse.ReadToEnd();
                    var list = (Dictionary<string, string>)JsonSerializer.Deserialize(stringResponse, typeof(Dictionary<string, string>));
                    return list["access_token"];
                }
                catch (Exception e)
                {
                    string errorText = String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : "Acquire token failed");
                    _logger.LogError(errorText);
                    return null;
                }
            }
            else
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,-+#?";
                var token = new string(Enumerable.Repeat(chars, 1400).Select(s => s[random.Next(s.Length)]).ToArray());
                _logger.LogInformation($"token: {token}");
                return token;
            }
        }

        private void GrantAccess(string fullPath)
        {
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null).Translate(typeof(NTAccount));
            var rule = new FileSystemAccessRule(users, FileSystemRights.FullControl, AccessControlType.Allow);
            
            FileInfo fileInfo = new FileInfo(fullPath);
            var sec = fileInfo.GetAccessControl();
            sec.AddAccessRule(rule);
            fileInfo.SetAccessControl(sec);
        }
    }
}
