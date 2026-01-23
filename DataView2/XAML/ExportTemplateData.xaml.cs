using CommunityToolkit.Maui.Views;
using DataView2.Engines;
using System.Data;
using System.Data.SQLite;
using Microsoft.Data.Sqlite;
using Serilog;

namespace DataView2;

public partial class ExportTemplateData : Popup
{
    public string connString = string.Empty;
    public ApplicationEngine appEngine;
    static private string _configAppPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    static private string _userFiles = "User_CSV_Files";
    static private string _configPath = $"{_configAppPath}\\Downloads\\{_userFiles}".Replace("\\", "/");

    public ExportTemplateData()
    {
        InitializeComponent();
        appEngine = MauiProgram.AppEngine;
    }

    private async void btnClose_Clicked(object? sender, EventArgs e)
    {
        await CloseAsync();
    }

    public List<string> GetTables()
    {
        List<string> listFormats = new List<string>();
        try
        {
            DataTable formats = GetDataTable("SELECT Name,Format from OutputTemplate");
            if(formats == null) { lblMessage.Text = "No formats found to export!"; }
            else
            foreach (DataRow row in formats.Rows)
            {
                listFormats.Add(string.Concat(row.ItemArray[0].ToString(), ':', row.ItemArray[1].ToString()));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return listFormats;
    }

    public List<string> GetInsertdData(string tableName, string columns, string insertQuerypart)
    {
        List<string> listData = new List<string>();
        try
        {
            DataTable formats = GetDataTable($"SELECT * from {tableName}");
            List<string> dataColumns = columns.Split(',').ToList();

            if (formats != null)
            {
                foreach (DataRow row in formats.Rows)
                {
                    string data = " VALUES (";
                    foreach (string col in dataColumns)
                    {
                        data += string.Concat('\'', row[col] != null ? row[col].ToString() : string.Empty, '\'', ','); ; ;
                    }

                    listData.Add(string.Concat(insertQuerypart, data.TrimEnd(','), ')'));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        return listData;
    }

    public DataTable GetDataTable(string sql)
    {
        try
        {
            DataTable dt = new DataTable();
            using (var c = new SQLiteConnection(connString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        dt.Load(rdr);
                        return dt;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async void CreateDataTable(List<ExportQueries> exportQueries)
    {
        try
        {
            //backup the database i.e. in the local one
            SqliteConnection m_dbConnection = new SqliteConnection(connString);
            //SqliteConnection m_dbConnection = new SqliteConnection("Data Source=:memory:");
            m_dbConnection.Open();

            foreach (ExportQueries eq in exportQueries)
            {
                //table creation
                SqliteCommand command = new SqliteCommand(eq.CreateTable, m_dbConnection);
                int rowsAffected = command.ExecuteNonQuery();

                //data insertion
                if (rowsAffected >= 0)
                {
                    foreach (string insertQuery in eq.InsertData)
                    {
                        command = new SqliteCommand(insertQuery, m_dbConnection);
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }

                //exporting tables
                await DownloadFile(eq.SourceTable, eq.TableName);
            }
            m_dbConnection.Close();

            lblMessage.Text = "Data is exported successfully!";
        }
        catch (Exception e)
        {
            lblMessage.Text = $"Export Failed : {e.Message}";
        }
    }


    

    private async Task DownloadFile(string filename, string tablename)
    {
        string downloadsPath = _configPath + $"\\{filename}.csv".Replace("\\", "/");
        lblMessage.Text = "Downloading File...";
       
        try
        {
            await appEngine.SettingTablesService.SaveTemplatedCSV(tablename, downloadsPath);

        }
        catch (Exception ex)
        {
            // Utils.RegError($"Error when execute query: {ex.Message}");
            Log.Logger.Error(ex, "Error when execute query - ExportData.");
        }
    }

    private void btnExportData_Clicked(object sender, EventArgs e)
    {
        lblMessage.Text = string.Empty;
        List<ExportDatabase> exportDatabases = new List<ExportDatabase>();
        DataTable dtExport = GetDataTable("SELECT \"Table\",\"Column\",\"Source\",Grouped,GroupedBy,Operation from OutputColumnTemplate;");

        if (dtExport == null) { lblMessage.Text = "No details found to export!"; }
        else
        {
            foreach (DataRow row in dtExport.Rows)
            {
                string[] sources = !string.IsNullOrEmpty(row[2].ToString()) ? row[2].ToString().Split('.') : null;
                exportDatabases.Add(new ExportDatabase
                {
                    Table = row[0].ToString(),
                    Column = row[1].ToString(),
                    SourceTable = sources != null ? sources[0] : string.Empty,
                    SourceColumn = sources != null ? sources[1] : string.Empty,
                    Grouped = row[3].ToString(),
                    GroupedBy = row[4].ToString(),
                    Operation = row[5].ToString()
                });
            }

            if (exportDatabases.Count > 0)
            {
                List<List<ExportDatabase>> groupedCustomerList = exportDatabases
                    .GroupBy(e => e.Table)
                    .Select(grp => grp.ToList())
                    .ToList();

                DataTable orgTables = GetDataTable("SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1");
                List<string> orgTablesList = new List<string>();
                foreach (DataRow row in orgTables.Rows)
                {
                    orgTablesList.Add(row[0].ToString());
                }

                List<ExportQueries> exportQueries = new List<ExportQueries>();
                if (groupedCustomerList.Count > 0)
                {
                    foreach (List<ExportDatabase> exports in groupedCustomerList)
                    {
                        string createTable = "CREATE TABLE destTable (", sourceTable = string.Empty, destTable = string.Empty;
                        string insertData = "INSERT INTO destTable (", insertDataPart = string.Empty;
                        foreach (ExportDatabase exportdb in exports)
                        {
                            if (destTable == string.Empty)
                                destTable = exportdb.Table;

                            if (sourceTable == string.Empty)
                                sourceTable = exportdb.SourceTable;

                            createTable += string.Concat(exportdb.Column, " text,");

                            insertData += string.Concat(exportdb.Column, ",");
                            insertDataPart += string.Concat(exportdb.SourceColumn, ",");
                        }

                        //end create query
                        createTable = createTable.Replace("destTable", destTable);
                        createTable = string.Concat(createTable.TrimEnd(','), ')');

                        //end insert query
                        insertData = insertData.Replace("destTable", destTable);
                        insertData = string.Concat(insertData.TrimEnd(','), ')');
                        insertDataPart = insertDataPart.TrimEnd(',');

                        //gettng data from table
                        List<string> orgDatafromDB = GetInsertdData(sourceTable, insertDataPart, insertData);

                        //check source table is exist in the main database, else discard the addition
                        if (orgTablesList.Contains(sourceTable))
                        {
                            exportQueries.Add(new ExportQueries { CreateTable = createTable, InsertData = orgDatafromDB, SourceTable = sourceTable, TableName = destTable });
                        }
                    }
                }

                if (exportQueries.Count > 0)
                {
                    CreateDataTable(exportQueries);
                }
            }
        }
    }

    private void Picker_Loaded(object sender, EventArgs e)
    {
        //get location of refernce database
        string folderpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Data View 2 Projects");
        string[] files = Directory.GetFiles(folderpath, "*.db");

        DbPicker.ItemsSource = files;
        if (files.Length > 0) { DbPicker.SelectedIndex = 0; }
        else lblMessage.Text = "No project database is found";
    }

    private void btnExportDb_Clicked(object sender, EventArgs e)
    {
        string filepath = DbPicker.SelectedItem.ToString();
        connString = $"Data Source={filepath};";

        TemplatePicker.ItemsSource = GetTables();
        TemplatePicker.SelectedIndex = 0;
    }
}

public class ExportDatabase
{
    public string Table { get; set; }
    public string Column { get; set; }
    public string SourceTable { get; set; }
    public string SourceColumn { get; set; }
    public string Grouped { get; set; }
    public string GroupedBy { get; set; }
    public string Operation { get; set; }
}

public class ExportQueries
{
    public string SourceTable { get; set; }
    public string TableName { get; set; }
    public string CreateTable { get; set; }
    public List<string> InsertData { get; set; }
}