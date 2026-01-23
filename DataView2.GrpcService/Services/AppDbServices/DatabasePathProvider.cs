namespace DataView2.GrpcService.Services.AppDbServices
{
    public class DatabasePathProvider
    {
        private string _databasePath;
        private string _metadataDatabasePath;
        private string _datasetDatabasePath;
        public DatabasePathProvider()
        {
            // Default constructor
        }
        public DatabasePathProvider(string initialPath, string initialMetadataDbPath, string dataSetDatabasePath)
        {
            _databasePath = initialPath;
            _metadataDatabasePath = initialMetadataDbPath;
            _datasetDatabasePath = dataSetDatabasePath;
        }
        public string GetDatabasePath() => _databasePath;

        public void SetDatabasePath(string newPath)
        {
            _databasePath = newPath;
        }

        public void SetDatabaseFileName(string newFileName)
        {
            string directory = Path.GetDirectoryName(_databasePath);
            _databasePath = Path.Combine(directory, newFileName);
        }

        public string GetMetadataDatabasePath() => _metadataDatabasePath;

        public void SetMetadataDatabasePath(string newPath) => _metadataDatabasePath = newPath;

        public void SetMetadataDatabaseFileName(string newFileName)
        {
            string directory = Path.GetDirectoryName(_metadataDatabasePath);
            _metadataDatabasePath = Path.Combine(directory, newFileName);
        }

        public string GetDatasetDatabasePath() => _datasetDatabasePath;

        public void SetDatasetDatabasePath(string newPath) => _datasetDatabasePath = newPath;

        public void SetDatasetDatabaseFileName(string newFileName)
        {
            string directory = Path.GetDirectoryName(_datasetDatabasePath);
            _datasetDatabasePath = Path.Combine(directory, newFileName);
        }

    }
}
