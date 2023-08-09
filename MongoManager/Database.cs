using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoManager
{
    public static class Database
    {
        private static bool setupDone = false;

        private static MongoClient client;
        public static MongoClient Client
        {
            get
            {
                Setup();
                return client;
            }
        }

        private static IMongoDatabase database;
        public static IMongoDatabase DatabaseInstance
        {
            get
            {
                Setup();
                return database;
            }
        }

        private static IMongoCollection<BsonDocument> collection;
        public static IMongoCollection<BsonDocument> Collection
        {
            get
            {
                Setup();
                return collection;
            }
        }

        private static void Setup()
        {
            if (setupDone) return;

            try
            {
                client = new MongoClient(Settings._connectionString);
                database = client.GetDatabase(Settings._databaseName);
                collection = database.GetCollection<BsonDocument>(Settings._collectionName);
                setupDone = true;
            }
            catch (Exception ex)
            {
                setupDone = false;
                throw new Exception("Error setting up MongoDB.", ex);
            }
        }
    }
}