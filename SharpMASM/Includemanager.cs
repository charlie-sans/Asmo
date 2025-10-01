using System;
using System.IO;

namespace SharpMASM
{
    public static class Includemanager
    {
        public static string Include(string path, string originalLine)
        {
            try
            {
                string rootDir = Path.GetDirectoryName(CmdArgs.GetInstance().FileName) ?? ".";
                string fullPath = Path.Combine(rootDir, path);

                // Look in the current directory if not found in the relative path
                if (!File.Exists(fullPath))
                {
                    fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);
                }

                // Look in the root directory structure
                if (!File.Exists(fullPath))
                {
                    string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "root");
                    if (Directory.Exists(rootPath))
                    {
                        fullPath = Path.Combine(rootPath, path);
                    }
                }

                if (!File.Exists(fullPath))
                {
                    throw new MASMException($"Include file not found: {path}");
                }

                Common.VerbosePrint($"Including file: {fullPath}");
                return File.ReadAllText(fullPath);
            }
            catch (Exception ex) when (!(ex is MASMException))
            {
                throw new MASMException($"Error including file {path}: {ex.Message}");
            }
        }
    }
}
