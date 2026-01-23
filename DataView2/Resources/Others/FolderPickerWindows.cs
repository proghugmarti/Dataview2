using FolderPickerWF;

namespace DataView2.Resources.Others
{
    public interface IFolderPicker
    {
        string PickFolder(string initialPath );
        string PickFile(string initialPath, string title, string[] filesType);
    }
    public class FolderPickerWindows: IFolderPicker {
        public string PickFile(string initialPath, string title, string[] filesType)
        {
            try
            {
                return FolderPicker.PickFile(initialPath, title, filesType);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled.");
                return null;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public string PickFolder(string initialPath )
        {
            try
            {                
                return FolderPicker.PickFolder(initialPath );
            }
            catch (OperationCanceledException)
            {               
                Console.WriteLine("Operation cancelled.");
                return null;
            }
            catch (Exception ex)
            {
               
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}
