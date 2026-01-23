using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core
{
    public class FileStorageManager
    {
        private readonly string filePath;
        private readonly string mutexName;

        public FileStorageManager(string directoryPath, string mutexName)
        {
            filePath = Path.Combine(directoryPath, $"SM_Service-DataView2.log");
            this.mutexName = mutexName;

            // Crear el directorio si no existe
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void SetVariable(string name, string value)
        {
            using (Mutex mutex = new Mutex(false, mutexName))
            {
                mutex.WaitOne();
                try
                {
                    var variables = ReadAllVariables();
                    variables[name] = value;
                    WriteAllVariables(variables);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public string GetVariable(string name)
        {
            using (Mutex mutex = new Mutex(false, mutexName))
            {
                mutex.WaitOne();
                try
                {
                    var variables = ReadAllVariables();
                    return variables.ContainsKey(name) ? variables[name] : null;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private Dictionary<string, string> ReadAllVariables()
        {
            var variables = new Dictionary<string, string>();

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        variables[parts[0]] = parts[1];
                    }
                }
            }

            return variables;
        }

        private void WriteAllVariables(Dictionary<string, string> variables)
        {
            var lines = new List<string>();
            foreach (var kvp in variables)
            {
                lines.Add($"{kvp.Key}={kvp.Value}");
            }
            File.WriteAllLines(filePath, lines);
        }
    }

    public static class FileUpdater
    {
        public static void UpdateIniFile(string filePath)
        {
            try
            {
                string targetFileName = "ini.ps1";
                string contentFilePath = filePath;

                if (!File.Exists(contentFilePath))
                {
                    Console.WriteLine($"The file {contentFilePath} does not exist.");
                    return;
                }

                string newContent = File.ReadAllText(contentFilePath);

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in allDrives)
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        SearchDirectory(drive.RootDirectory.FullName, targetFileName, newContent, 3); // Limit search depth to 3
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating INI file: {ex.Message}");
            }
        }

        private static void SearchDirectory(string rootDirectory, string targetFileName, string newContent, int maxDepth)
        {
            try
            {
                var directories = Directory.EnumerateDirectories(rootDirectory, "*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var directory in directories)
                {
                    try
                    {
                        var files = Directory.EnumerateFiles(directory, targetFileName, SearchOption.TopDirectoryOnly).ToList();
                        foreach (string file in files)
                        {
                            File.WriteAllText(file, newContent);
                            Console.WriteLine($"The file {file} has been updated.");
                        }

                        if (maxDepth > 1)
                        {
                            SearchDirectory(directory, targetFileName, newContent, maxDepth - 1); // Recursive call with reduced depth
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"Unauthorized access to directory {directory}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error searching directory {directory}: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Unauthorized access to directory {rootDirectory}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching directory {rootDirectory}: {ex.Message}");
            }
        }
    }

}
