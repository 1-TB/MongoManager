using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoManager;

public static class Database
{
    //setup the mongo client
    public static MongoClient client;
    public static IMongoDatabase database;
    public static IMongoCollection<BsonDocument> collection;
    private static bool setupDone = false;
    public static void Setup()
    {
        if (setupDone) return;
        client = new MongoClient(Settings._connectionString);
        database = client.GetDatabase(Settings._databaseName);
        collection = database.GetCollection<BsonDocument>(Settings._collectionName);
        setupDone = true;
    }
    
   
}