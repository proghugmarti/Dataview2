using DataView2.Core.Models.Database_Tables;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataView2.Core.MultiInstances
{ 
    public static class SharedDVInstanceStore
    {
        // -----------------------------
        // Persistent file path
        // -----------------------------
        private static readonly string FILE_PATH =
            Path.Combine("C:\\DataView2\\Logs", "DV_Instances.json");

        // -----------------------------
        // Inter-process synchronization
        // -----------------------------
        private static readonly Mutex GlobalMutex =
            new Mutex(false, @"Global\DV2_SHARED_INSTANCE_MUTEX");

        private static readonly object fileLock = new();

        private static string CurrentProcessId => Process.GetCurrentProcess().Id.ToString();

        // -----------------------------
        // Instance descriptor
        // -----------------------------
        public sealed class DVInstanceInfo
        {
            public bool MainSession { get; set; }
            public string IdSurvey { get; set; } = string.Empty;
            public string ProjectPath { get; set; } = string.Empty;
            public string WSProcPort { get; set; } = string.Empty;
            public string WSProcStatus { get; set; } = string.Empty;
        }

        // -----------------------------
        // Flag for the current instance
        // -----------------------------
        public static bool IsMainSession { get; private set; } = false;

        // -----------------------------
        // Read dictionary
        // -----------------------------
        public static Dictionary<string, DVInstanceInfo> Read()
        {
            lock (fileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FILE_PATH)!);

                if (!File.Exists(FILE_PATH))
                    return new Dictionary<string, DVInstanceInfo>();

                var json = File.ReadAllText(FILE_PATH);
                if (string.IsNullOrWhiteSpace(json))
                    return new Dictionary<string, DVInstanceInfo>();

                return JsonSerializer.Deserialize<Dictionary<string, DVInstanceInfo>>(json)
                       ?? new Dictionary<string, DVInstanceInfo>();
            }
        }

        // -----------------------------
        // Write dictionary with main/secondary logic
        // -----------------------------
        private static void Write(DVInstanceInfo currentInstance)
        {
            lock (fileLock)
            {
                NormalizeWSFields(currentInstance);

                var data = Read();

                // Check if there is already a MainSession (excluding this PID)
                bool mainExists = false;
                foreach (var kvp in data)
                {
                    if (kvp.Value.MainSession && kvp.Key != CurrentProcessId)
                    {
                        mainExists = true;
                        break;
                    }
                }

                if (currentInstance.MainSession)
                {
                    if (!mainExists)
                    {
                        // ✅ No Main exists → allow this instance to become Main
                        currentInstance.MainSession = true;
                        data[CurrentProcessId] = currentInstance;
                        IsMainSession = true;
                    }
                    else
                    {
                        // ❌ Main already exists → cannot become Main
                        currentInstance.MainSession = false;
                        data[CurrentProcessId] = currentInstance;
                        IsMainSession = false;
                    }
                }
                else
                {
                    // Secondary session
                    // 🔒 If this PID is already Main in file, do NOT downgrade it
                    if (data.TryGetValue(CurrentProcessId, out var existingInstance)
                        && existingInstance.MainSession)
                    {
                        currentInstance.MainSession = true;
                        data[CurrentProcessId] = currentInstance;
                        IsMainSession = true;
                    }
                    else
                    {
                        currentInstance.MainSession = false;
                        data[CurrentProcessId] = currentInstance;
                        IsMainSession = false;
                    }
                }

                var json = JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FILE_PATH, json);

                Log.Information(
                    $"[DV MultiSession] Instance written | PID={CurrentProcessId}, Survey={currentInstance.IdSurvey}, Main={currentInstance.MainSession}");
            }
        }


        // -----------------------------
        // Register or update instance
        // -----------------------------
        public static bool TryRegisterInstance(string surveyExternalId, string projectPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            GlobalMutex.WaitOne();
            try
            {
                string nameProject = string.Empty;
                if (!string.IsNullOrWhiteSpace(projectPath))
                    nameProject = System.IO.Path.GetFileNameWithoutExtension(projectPath);

                // =====================================================
                // RULE 0: If this is the ONLY running DataView2 process,
                // memory is the source of truth.
                // Rebuild the file completely and force MainSession=true
                // =====================================================
                if (CheckIfOnlyInstance())
                {
                    lock (fileLock)
                    {
                        var cleanData = new Dictionary<string, DVInstanceInfo>
                        {
                            [CurrentProcessId] = new DVInstanceInfo
                            {
                                MainSession = true,
                                IdSurvey = surveyExternalId,
                                ProjectPath = projectPath
                            }
                        };

                        var json = JsonSerializer.Serialize(
                            cleanData,
                            new JsonSerializerOptions { WriteIndented = true });

                        File.WriteAllText(FILE_PATH, json);

                        IsMainSession = true;

                        Log.Information(
                            "[DV MultiSession] Single process detected. Storage rebuilt. PID={Pid}, Survey={Survey}",
                            CurrentProcessId,
                            surveyExternalId);
                    }

                    return true;
                }

                // =====================================================
                // RULE 1: More than one process -> file is authoritative
                // =====================================================
                var data = Read();

                bool mainExists = data.Any(kvp => kvp.Value.MainSession);

                // =====================================================
                // RULE 2: Survey uniqueness
                // Only secondaries are restricted
                // =====================================================
                if (mainExists)
                {
                    foreach (var kvp in data)
                    {
                        if (kvp.Value.IdSurvey == surveyExternalId &&
                !string.IsNullOrEmpty(kvp.Value.ProjectPath) &&
                kvp.Value.ProjectPath == projectPath)
                        {
                            errorMessage =
                                $"Survey '{surveyExternalId}' of project '{nameProject}' is already in use by another DV instance (PID {kvp.Key}).";
                            return false;
                        }
                    }
                }

                // =====================================================
                // RULE 3: Decide role
                // - If no Main exists -> become Main
                // - If Main exists -> become Secondary
                // =====================================================
                IsMainSession = !mainExists;

                var currentInstance = new DVInstanceInfo
                {
                    MainSession = IsMainSession,
                    IdSurvey = surveyExternalId,
                    ProjectPath = projectPath
                };

                Write(currentInstance);
                return true;

            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }



        // -----------------------------
        // Remove only current process
        // -----------------------------
        public static void RemoveCurrentInstance()
        {
            GlobalMutex.WaitOne();
            try
            {
                lock (fileLock)
                {
                    var data = Read();
                    string pid = CurrentProcessId;

                    if (data.Remove(pid))
                    {
                        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(FILE_PATH, json);
                        Log.Information($"[DV MultiSession] Removed PID={pid} from shared storage.");
                    }
                }
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        // -----------------------------
        // Check if there is other instance using the same Project path 
        // -----------------------------
        public static bool IsProjectPathInUseByAnotherInstance(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return false;
            GlobalMutex.WaitOne();
            try
            {
                var data = Read();

                foreach (var kvp in data)
                {
                    // Ignore current process
                    if (kvp.Key == CurrentProcessId)
                        continue;

                    if (string.Equals(
                            kvp.Value.ProjectPath,
                            projectPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                GlobalMutex.ReleaseMutex();
            }
        }

        private static void NormalizeWSFields(DVInstanceInfo instance)
        {
            instance.WSProcPort ??= string.Empty;
            instance.WSProcStatus ??= string.Empty;
        }       

        // -----------------------------
        // Check if this is the only DataView process in memory
        // If yes, always treat it as MainSession
        // -----------------------------
        public static bool CheckIfOnlyInstance()
        {
            // Count processes with the same name
            var processes = System.Diagnostics.Process.GetProcessesByName("DataView2");
            if (processes.Length == 1)
            {
                IsMainSession = true;
                return true;
            }
            IsMainSession = false;
            return false;
        }

        // -----------------------------
        // Clean the file in case Dataview2
        // closes inexpectelly.
        // -----------------------------
        private static void CleanupOrphanSecondarySessions()
        {
            lock (fileLock)
            {
                var data = Read();

                if (data.Count == 0)
                    return;

                var keysToRemove = data
                    .Where(kvp => kvp.Value.MainSession == false)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (keysToRemove.Count == 0)
                    return;

                foreach (var key in keysToRemove)
                {
                    data.Remove(key);
                    Log.Information("[DV MultiSession] Cleanup - Removed orphan secondary PID={Pid}", key);
                }

                var json = JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FILE_PATH, json);

                Log.Information("[DV MultiSession] Cleanup - Orphan secondary sessions cleaned");
            }
        }

        // =====================================================
        // Facade handler:  
        // =====================================================
        public static class DVInstances
        {
            public static Dictionary<string, DVInstanceInfo> Read() => SharedDVInstanceStore.Read();

            public static bool TrySetSurvey(string survey, string projectPath, out string errorMessage)
                => SharedDVInstanceStore.TryRegisterInstance(survey, projectPath, out errorMessage);

            public static void RemoveCurrentInstance() => SharedDVInstanceStore.RemoveCurrentInstance();

            public static bool IsProjectPathInUse(string projectPath)
                => SharedDVInstanceStore.IsProjectPathInUseByAnotherInstance(projectPath);

            public static bool IsOnlyInstance() => SharedDVInstanceStore.CheckIfOnlyInstance();
            public static bool IsMainSession()  => SharedDVInstanceStore.IsMainSession;
        }
    }
}
