using Serilog;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
namespace DataView2.Core
{
    public class SharedMemoryManager
    {
        private const int MemorySize = 1024; 
        private const string MemoryName = "Init_Service_SharedMemory";

        private MemoryMappedFile memoryMappedFile;
        private MemoryMappedViewAccessor accessor;
        private SemaphoreSlim semaphore;

        public SharedMemoryManager()
        {
            memoryMappedFile = MemoryMappedFile.CreateOrOpen(MemoryName, MemorySize);
            accessor = memoryMappedFile.CreateViewAccessor();
            semaphore = new SemaphoreSlim(1, 1);
        }

        public void SetVariable(string name, string value)
        {
            semaphore.Wait();
            try
            {
                Dictionary<string, string> variables = ReadAllVariables();
                variables[name] = value;
                WriteAllVariables(variables);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public string GetVariable(string name)
        {
            semaphore.Wait();
            try
            {
                Dictionary<string, string> variables = ReadAllVariables();
                return variables.ContainsKey(name) ? variables[name] : null;
            }
            catch(Exception ex)
            {
                Log.Error($"Error in Share Memory to start service and main app synchronization: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private Dictionary<string, string> ReadAllVariables()
        {
            byte[] data = new byte[MemorySize];
            accessor.ReadArray(0, data, 0, MemorySize);
            string content = Encoding.UTF8.GetString(data).TrimEnd('\0');
            Dictionary<string, string> variables = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(content))
            {
                string[] pairs = content.Split(';');
                foreach (var pair in pairs)
                {
                    if (string.IsNullOrWhiteSpace(pair)) continue;

                    string[] kv = pair.Split('=');
                    if (kv.Length == 2)
                    {
                        variables[kv[0]] = kv[1];
                    }
                }
            }

            return variables;
        }

        private void WriteAllVariables(Dictionary<string, string> variables)
        {
            StringBuilder contentBuilder = new StringBuilder();
            foreach (var kv in variables)
            {
                contentBuilder.Append($"{kv.Key}={kv.Value};");
            }

            byte[] data = Encoding.UTF8.GetBytes(contentBuilder.ToString());
            accessor.WriteArray(0, data, 0, data.Length);
        }

        ~SharedMemoryManager()
        {
            accessor.Dispose();
            memoryMappedFile.Dispose();
            semaphore.Dispose();
        }
    }

}
