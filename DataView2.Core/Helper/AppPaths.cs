using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    public static class AppPaths
    {
        public static string DocumentsFolder => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static string DefaultProjectFolder => Path.Combine(DocumentsFolder, "Data View 2 Projects");

        public static string OfflineMapFolder => Path.Combine(DefaultProjectFolder, "OfflineMap");
        public static string IconFolder => Path.Combine(DefaultProjectFolder, "Icons");
    }

}
