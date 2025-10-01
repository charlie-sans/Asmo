using System;
using System.Net;
using System.Threading;
using System.Text;
using System.IO;

namespace SharpMASM
{
    public class SimpleHttpServer
    {
        private readonly HttpListener _listener;
        private readonly Thread _serverThread;
        private readonly string _rootDirectory;
        private bool _running;
        private readonly int _port;

        public SimpleHttpServer(string rootDirectory, int port = 7080)
        {
            _rootDirectory = rootDirectory;
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{_port}/");
            _serverThread = new Thread(ListenForRequests);
        }

        public void Start()
        {
            if (_running) return;

            try
            {
                Console.WriteLine($"Starting HTTP server at http://localhost:{_port}/");
                _running = true;
                _listener.Start();
                _serverThread.Start();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Failed to start HTTP server: {ex.Message}");
                if (ex.ErrorCode == 5) // Access is denied
                {
                    Console.WriteLine("Please ensure the application is running with sufficient privileges.");
                }
                _running = false;
            }
        }

        public void Stop()
        {
            if (!_running) return;
            
            _running = false;
            _listener.Stop();
            _serverThread.Join();
        }

        private void ListenForRequests()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Server stopped or interrupted
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing request: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string filePath = context.Request.Url.AbsolutePath;
            
            // Default to index.html for root requests
            if (filePath == "/")
                filePath = "/index.html";

            // Map the requested URL to a physical file path
            string physicalFilePath = Path.Combine(_rootDirectory, filePath.TrimStart('/'));

            if (File.Exists(physicalFilePath))
            {
                // Determine MIME type
                string mimeType = GetMimeType(Path.GetExtension(physicalFilePath));
                
                // Read file and send response
                byte[] fileBytes = File.ReadAllBytes(physicalFilePath);
                context.Response.ContentType = mimeType;
                context.Response.ContentLength64 = fileBytes.Length;
                context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                context.Response.OutputStream.Close();
                
                Console.WriteLine($"Served: {filePath} ({mimeType})");
            }
            else
            {
                // Return 404 if file not found
                context.Response.StatusCode = 404;
                string notFoundMessage = $"File not found: {filePath}";
                byte[] notFoundBytes = Encoding.UTF8.GetBytes(notFoundMessage);
                context.Response.OutputStream.Write(notFoundBytes, 0, notFoundBytes.Length);
                context.Response.OutputStream.Close();
                
                Console.WriteLine($"Not Found: {filePath}");
            }
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".ico" => "image/x-icon",
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream",
            };
        }
    }
}
