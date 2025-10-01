using System;
using System.IO;
using static SharpMASM.Core_Interpreter;


namespace SharpMASM
{
    public class Entry
    { 
        public static CmdArgs cmdArgs { get; private set; } = new CmdArgs();
        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Parse command line arguments first
            cmdArgs = CmdArgs.Parse(args);

            // Explicitly set the instance to make sure it's available
            CmdArgs.Instance = cmdArgs;

            // Print command line arguments if in verbose mode
            if (cmdArgs.Verbose || cmdArgs.VeryVerbose)
            {
                Console.WriteLine($"Command line arguments: UseBuiltInArrays={cmdArgs.UseBuiltInArrays}");
            }

            // Initialize memory only once based on command-line arguments
            Common.Memory = Common.InitializeMemory();
            Functions.Long_memory = Common.Memory;

            if (cmdArgs.StartServer)
            {
                // Use SimpleHttpServer
                StartSimpleServer();
            }
            else
            {
                // Check if a file was specified
                if (string.IsNullOrEmpty(CmdArgs.GetInstance().FileName))
                {
                    Console.WriteLine("Usage: ./masm <filename>");
                    return;
                }

                try
                {
                    // Read the file
                    if (!File.Exists(CmdArgs.GetInstance().FileName))
                    {
                        Console.WriteLine($"File not found: {CmdArgs.GetInstance().FileName}");
                        return;
                    }

                    string[] lines = File.ReadAllLines(CmdArgs.GetInstance().FileName);

                    // Initialize Common and Instructions classes
                    Common.Instance = new Common();
                    Common.InstructionInstance = new Instructions();

                    // Parse the instructions
                    Parsing.ParseInstructions(lines);

                    // Print instructions if in verbose mode
                    if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
                    {
                        Instructions.PrintInstructions_plus_DebuggingInfo();
                    }

                    Common.VerbosePrint("Program parsed successfully. Ready for execution.");

                    // Execute the program - instruction pointer is reset within Interpret method
                    Console.WriteLine("Program loaded successfully.");
                    Interpret(Instructions.GetInstance());
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"File not found: {CmdArgs.GetInstance().FileName}");
                }
                catch (MASMException ex)
                {
                    // Exception handling is already done in the constructor
                    if (CmdArgs.GetInstance().VeryVerbose)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (CmdArgs.GetInstance().VeryVerbose)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        // Replace StartServer with a method that uses SimpleHttpServer
        public static void StartSimpleServer()
        {
            string rootDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "root");
            
            if (!Directory.Exists(rootDirectory))
            {
                Console.WriteLine($"Creating documentation directory: {rootDirectory}");
                Directory.CreateDirectory(rootDirectory);
                
                // Create a simple index.html if no content exists
                string indexPath = Path.Combine(rootDirectory, "index.html");
                if (!File.Exists(indexPath))
                {
                    File.WriteAllText(indexPath, 
                        "<html><body><h1>SharpMASM Documentation</h1>" +
                        "<p>Documentation server is running. Add HTML files to the 'root' directory.</p></body></html>");
                }
            }

            Console.WriteLine("Starting documentation server...");
            var server = new SimpleHttpServer(rootDirectory);

            try
            {
                server.Start();
                Console.WriteLine("Server is running. Press Ctrl+C to stop.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
                return;
            }

            // Handle graceful shutdown
            Console.CancelKeyPress += (sender, e) => {
               // e.Cancel = true; // Prevent the process from terminating immediately
                server.Stop();
                Console.WriteLine("Server stopped.");
            };

            // Keep the application running
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        // Remove the CreateHostBuilder method as it's no longer needed
   }
}