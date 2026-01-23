using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.IO;

namespace WS_AWS_CSV
{
    public sealed class AWS_Bucket
    {
        IConfiguration _configuration;
        ILogger _logger;
        private static string? _LogSrvFile = "";//"c:\\DataView2\\Logs";
        private string? _bucketName = "";//"romdas";
        private string? _folderAWSName = "";//"cvs";
        private bool _isExternal_FilesSourceFolder = false;// "\\\\192.168.1.3\\Destino\\To be Uploaded";       
        private string? _filesFolderSource = "";// "\\\\192.168.1.3\\Destino\\To be Uploaded";
        private string? _localSourceFolder = "";// @"D:\SurveyAWS\";
        private string? _localDestinationFolder = "";// @"D:\\SurveyAWS\\Uploaded";
        private string? _accessKeyId = "";//"AKIA47CR3KZ5JTPVM4GZ";
        private string? _secretAccessKey = "";//"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";
        private string? _region = "";//"us-east-1";
        private string? InitialPathDirectoryBase = "";
        private string? InitialDirectoryBase = "";
        private static readonly string pathLogFolderServices = "c:\\DataView2\\Logs";
        private AWS_Doc AWSDoc;
        private static List<AWS_Doc> AWSDocs = new List<AWS_Doc>();
        private bool _ProcessResult = true;
        private bool filesExist = false;
        public AWS_Bucket(ILogger<WindowsBackgroundService> logger, IConfiguration configuration)
        {
            try
            {
                _logger = logger;
                _configuration = configuration;
                _LogSrvFile = _configuration["LogSrv"];
                _bucketName = _configuration["BucketName"];
                _folderAWSName = _configuration["FolderAWSName"];
                _isExternal_FilesSourceFolder = ConvertToBool(_configuration["IsExternal_FilesSourceFolder"]);
                _filesFolderSource = _configuration["FilesFolderSource"]; //"D:\\SurveyAWS\\To be UploadedCopy"; //_configuration["FilesFolderSource"];
                _localSourceFolder = _configuration["LocalSourceFolder"];
                _localDestinationFolder = _configuration["LocalDestinationFolder"];
                _accessKeyId = _configuration["AccessKeyId"];
                _secretAccessKey = _configuration["SecretAccessKey"];
                _region = _configuration["Region"];
                AWSDocs = new List<AWS_Doc>();
            }
            catch (Exception ex)
            {
                logger.LogError($"Initializing variables - Error:{ex.Message}%");
                WriteSrvLog(pathLogFolderServices, $"Error :: Initializing variables - Error:{ex.Message}%");
            }
        }

        public async Task<bool> UploadFolderToS3()
        {
            try
            {
                if (IsCompleteData()) { return false; }
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(_folderAWSName, _localSourceFolder));
                InitialDirectoryBase = directoryInfo.Name;
                WriteSrvLog(pathLogFolderServices, " >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>     Process started.");
                //Empty and Fill main directory:
                if (_isExternal_FilesSourceFolder)
                {
                    ClearDirectory(_localSourceFolder);
                    CopyInitialDirectory(_filesFolderSource, _localSourceFolder, true);
                }

                // Upload files to the Bucket:
                await UploadFilesToS3(_bucketName, _folderAWSName, directoryInfo.FullName, _accessKeyId, _secretAccessKey, _region);
                
                foreach (var subDirectory in directoryInfo.GetDirectories())
                {
                    await UploadFilesToS3v2(_bucketName, _folderAWSName, Path.Combine(_localSourceFolder, subDirectory.Name), _accessKeyId, _secretAccessKey, _region);
                }
                if (directoryInfo.GetDirectories().Length == 0)
                {
                    _logger.LogWarning($"There is no file to Upload (0 files found).");
                    WriteSrvLog(pathLogFolderServices, $"There is no file to Upload (0 files found).");
                }
                return _ProcessResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading folder {_localSourceFolder} to Amazon S3: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"Error uploading folder {_localSourceFolder} to Amazon S3: {ex.Message}");
                return _ProcessResult;
            }
        }

        private bool IsCompleteData()
        {
            //_LogSrvFile = "c:\\DataView2\\Logs\\LogSrv_UploadAWSSurveys.log";
            //_bucketName = "romdas";
            //_folderAWSName = "cvs";
            //_isExternal_FilesSourceFolder = false;
            //_filesFolderSource = "\\\\192.168.1.3\\Destino\\To be Uploaded"; 
            //_localSourceFolder = "\\\\192.168.1.3\\Destino\\To be Uploaded";
            //_localDestinationFolder = "\\\\192.168.1.3\\Destino\\Uploaded";
            //_accessKeyId = "AKIA47CR3KZ5JTPVM4GZ";
            //_secretAccessKey = "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";
            //_region = "us-east-1";
            
            bool res = false;
            if (_bucketName == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for Bucket Name");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for Bucket Name");
            }
            if (_folderAWSName == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for Folder Name");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for Folder Name");
            }
            if (_localSourceFolder == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for Directory Path");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for Directory Path");
            }
            if (_accessKeyId == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for AccessKey Id");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for AccessKey Id");
            }
            if (_secretAccessKey == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for Secret Access Key");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for Secret Access Key");
            }
            if (_region == "")
            {
                res = true;
                _logger.LogError($"There is no info defined for Region");
                WriteSrvLog(pathLogFolderServices, $"There is no info defined for Region");
            }

            return res;
        }
        public async Task UploadFilesToS3(string bucketName, string folderName, string directoryPath, string accessKeyId, string secretAccessKey, string region)
        {
            try
            {
                AWSDoc = new AWS_Doc();
                using (TransferUtility transferUtility = new TransferUtility(new AmazonS3Client()))
                {

                    DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                    FileInfo[] files = directoryInfo.GetFiles();

                    AWSDoc.Directory = directoryInfo.FullName;
                    InitialPathDirectoryBase = directoryInfo.FullName;
                    _logger.LogInformation($"Files uploaded of Directory >>>>>>>>>>>>>>>{directoryInfo}:");
                    WriteSrvLog(pathLogFolderServices, $"Files uploaded of Directory >>>>>>>>>>>>>>>{directoryInfo}:");
                    if (_ProcessResult)
                    {
                        foreach (FileInfo file in files)
                        {
                            string key = folderName + "/" + file.Name;
                            //Uncomment when the account in AWS S3 is defined:
                            //await transferUtility.UploadAsync(file.FullName, bucketName, key);
                            AWSDoc.Files.Add(file.FullName);
                            _logger.LogWarning($"File: {file.Name} uploaded.");
                            WriteSrvLog(pathLogFolderServices, $"File: {file.Name} uploaded.");
                        }
                        AWSDocs.Add(AWSDoc);
                    }
                    ////  List<string> filesAWS = await GetBucketFiles(bucketName, folderName);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading files to Amazon S3: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"Error uploading files to Amazon S3: {ex.Message}");
                _ProcessResult = false;
                return;
            }
        }
        public async Task UploadFilesToS3v2(string bucketName, string folderName, string directoryPath, string accessKeyId, string secretAccessKey, string region)
        {

            try
            {
                AWSDoc = new AWS_Doc();
                using (TransferUtility transferUtility = new TransferUtility(new AmazonS3Client()))
                {

                    DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                    FileInfo[] files = directoryInfo.GetFiles();
                    string subDirectory = directoryPath.Replace(_localSourceFolder, "");
                    AWSDoc.Directory = directoryInfo.FullName;
                    _logger.LogInformation($"Files uploaded of Directory >>>>>>>>>>>>>>>{AWSDoc.Directory}:");
                    WriteSrvLog(pathLogFolderServices, $"Files uploaded of Directory >>>>>>>>>>>>>>>{AWSDoc.Directory}:");

                    foreach (FileInfo file in files)
                    {
                        string key = folderName + subDirectory.Replace("\\", "/") + "/" + file.Name;
                        //Uncomment when the account in AWS S3 is defined:
                        //await transferUtility.UploadAsync(file.FullName, bucketName, key);
                        AWSDoc.Files.Add(file.FullName);
                        _logger.LogWarning($"File: {file.Name} uploaded.");
                        WriteSrvLog(pathLogFolderServices, $"File: {file.Name} uploaded.");
                    }
                    AWSDocs.Add(AWSDoc);
                    foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
                    {
                        await UploadFilesToS3v2(bucketName, folderName, subDirectoryInfo.FullName, accessKeyId, secretAccessKey, region);
                    }
                    //  List<string> filesAWS = await GetBucketFiles(bucketName, folderName);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading files to Amazon S3: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"Error uploading files to Amazon S3: {ex.Message}");
            }
        }
        /// <summary>
        /// Function that retrieves all existing files in the folder of the connected bucket::
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private async Task<List<string>> GetBucketFiles(string bucketName, string folderName)
        {
            try
            {
                var client = new AmazonS3Client();

                ListObjectsV2Request request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = folderName
                };

                ListObjectsV2Response response = await client.ListObjectsV2Async(request);

                List<string> fileList = new List<string>();

                foreach (var obj in response.S3Objects)
                {
                    fileList.Add(obj.Key);
                }

                return fileList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading files to Amazon S3: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"Error uploading files to Amazon S3: {ex.Message}");
                return new List<string>();
            }
        }
        public static void WriteSrvLog(string pathDirectory, string txt)
        {
            try
            {
                // Define the path to the log file               
                string logFilePath = _LogSrvFile;

                // Check if the directory exists, if not, create it
                if (!Directory.Exists(pathDirectory))
                {
                    Directory.CreateDirectory(pathDirectory);
                }

                // Check if the log file exists, if not, create it and write the header
                if (!File.Exists(logFilePath))
                {
                    using (StreamWriter sw = File.CreateText(logFilePath))
                    {
                        sw.WriteLine("Log file created on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }

                // Append the text to the log file
                using (StreamWriter sw = File.AppendText(logFilePath))
                {
                    sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}  >> {txt}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");

            }
        }
        public void CopyInitialDirectory(string sourceDir, string destinationDir, bool copySubDirs)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDir);
                DirectoryInfo[] dirs = dir.GetDirectories();
               
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
               
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(tempPath, false);
                }
              
                if (copySubDirs)
                {
                    foreach (DirectoryInfo subdir in dirs)
                    {
                        string tempPath = Path.Combine(destinationDir, subdir.Name);
                        CopyInitialDirectory(subdir.FullName, tempPath, copySubDirs);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error CopyDirectory: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"ErrorCopyDirectory: {ex.Message}");
            }
        }
        public static void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"The directory doesn't exist: {path}");
            }

            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                Directory.Delete(directory, true);
            }
        }
        public void MoveFilesUploaded()
        {
            try
            {
                
                if (!Directory.Exists(_localDestinationFolder))
                {
                    Directory.CreateDirectory(_localDestinationFolder);
                    _logger.LogInformation($"Destination folder created: {_localDestinationFolder}");
                    WriteSrvLog(pathLogFolderServices, $"Destination folder created: {_localDestinationFolder}");
                }
                string folderDestinate = _localDestinationFolder;
                foreach (AWS_Doc awsDoc in AWSDocs)
                {
                    string destination = awsDoc.Directory;
                    string path = Path.GetFileName(destination);
                    string subFolder = folderDestinate + destination.Replace(InitialPathDirectoryBase, "");
                    if (!Directory.Exists(subFolder) && InitialDirectoryBase != path)
                    {
                        Directory.CreateDirectory(subFolder);
                        _logger.LogInformation($"Destination folder created: {subFolder}");
                        WriteSrvLog(pathLogFolderServices, $"Destination folder created: {subFolder}");
                    }

                    foreach (string awsDocfile in awsDoc.Files)
                    {

                        if (File.Exists(awsDocfile))
                        {
                            filesExist = true;
                            string fileName = Path.GetFileName(awsDocfile);
                            string destinationFilePath = Path.Combine(subFolder, fileName);

                            // Move the file:
                            File.Move(awsDocfile, destinationFilePath, true);
                            _logger.LogWarning($"*File moved from {awsDocfile} to {destinationFilePath}");
                            WriteSrvLog(pathLogFolderServices, $"*File moved from {awsDocfile} to {destinationFilePath}");
                        }
                        else
                        {
                            _logger.LogWarning($"**File not found: {awsDocfile}");
                            WriteSrvLog(pathLogFolderServices, $"**File not found: {awsDocfile}");
                        }
                    }
                    if (filesExist) {
                        _logger.LogWarning($"***Files moved to : {_localDestinationFolder} successfully.");
                        WriteSrvLog(pathLogFolderServices, $"Files moved to : {_localDestinationFolder} successfully.");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error moving files: {ex.Message}");
                WriteSrvLog(pathLogFolderServices, $"Error moving files: {ex.Message}");
            }
        }

        public void ClearSubDirectory()
        {
            if (Directory.Exists(_localSourceFolder))
            {
                // Delete all files and subdirectories
                DirectoryInfo directory = new DirectoryInfo(_localSourceFolder);

                //foreach (FileInfo file in directory.GetFiles())
                //{
                //    file.Delete();
                //}

                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    subDirectory.Delete(true);
                }
                if (filesExist)
                {
                    _logger.LogInformation($"All subdirectories and files in {_localSourceFolder} have been deleted.");
                    WriteSrvLog(pathLogFolderServices,$"All subdirectories and files in {_localSourceFolder} have been deleted.");
                }
            }
            else
            {
                _logger.LogInformation($"Directory {_localSourceFolder} does not exist.");
                WriteSrvLog(pathLogFolderServices, $"Directory {_localSourceFolder} does not exist.");
            }
        }
        public static bool ConvertToBool(string text)
        {
            if (text == "1")
            {
                return true;
            }
            else if (text == "0")
            {
                return false;
            }
            else
            {
                bool.TryParse(text, out bool result);
                return result;
            }
        }
    }

    public class AWS_Doc
    {
        public string Directory { get; set; }
        public List<string> Files { get; set; } = new List<string>();
    }
}
