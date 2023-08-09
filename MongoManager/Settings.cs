namespace MongoManager;

public static class Settings
{
    public static string _connectionString ="";
    public static string _databaseName = "";
    public static string _collectionName = "";
    public static string _savePath = "MongoManager.sav";
    
    public static void Save()
    {
        //save the settings in bytes format using byte writer
        using var writer = new BinaryWriter(File.Open(_savePath, FileMode.Create));
        writer.Write(_connectionString);
        writer.Write(_databaseName);
        writer.Write(_collectionName);
    
    }

    public static void Load()
    {
        //load settings using binary reader
        using var reader = new BinaryReader(File.Open(_savePath, FileMode.Open));
        _connectionString = reader.ReadString();
        _databaseName = reader.ReadString();
        _collectionName = reader.ReadString();
        
    }
}