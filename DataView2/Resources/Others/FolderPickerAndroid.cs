using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Resources.Others
{   
    public class FolderPickerAndroid : IFolderPicker
    {
        public string PickFile(string initialPath, string title, string[] filesType)
        {
            var path = "";

            //Set the Android implementation code here...

            return path;
        }

        public string PickFolder(string initialPath)
        {
            var path = "";

            //Set the Android implementation code here...

            return path;
        }
    }
}
