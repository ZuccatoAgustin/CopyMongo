using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace copymongo
{
    internal class CopyDB
    {
        private readonly CopyConfig config;
        private MongoClient sourceClient;
        private MongoClient targetClient;

        public CopyDB(CopyConfig config)
        {
            this.config = config;
            sourceClient = new MongoClient(config.FromConnectionString);
            targetClient = new MongoClient(config.ToConnectionString);
        }

        public async Task Copy()
        {
            Console.WriteLine("Start Copy");

            foreach (var c in config.CopyDbs.Where(e => !e.Ignore))
            {
                await Copy(c.From, c.To);
            }

            Console.WriteLine("Ready");
        }

        private async Task Copy(string source, string target)
        {
            Console.WriteLine($"-------------------- Copying {source} to {target}");
            var copyFromDb = sourceClient.GetDatabase(source);
            var targetMongoDb = targetClient.GetDatabase(target);

            var collectionNames = await (await copyFromDb.ListCollectionNamesAsync()).ToListAsync();

            foreach (var name in collectionNames)
            {
                Console.WriteLine($"copying {name}");
                var copyCollection = copyFromDb.GetCollection<BsonDocument>(name).AsQueryable(); // or use the c# class in the collection

                var targetCollection = targetMongoDb.GetCollection<BsonDocument>(name);

                //await targetMongoDb.DropCollectionAsync(name);

                targetCollection.InsertMany(copyCollection);
            }
        }
    }

    public static class MongoDbConventions
    {
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;

#pragma warning disable 618
            // https://jira.mongodb.org/browse/CSHARP-3179
            MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;
#pragma warning restore 618

            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(
                typeof(decimal?),
                new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128))
            );

            var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String)
        };

            ConventionRegistry.Register("Custom Conventions", pack, _ => true);

            initialized = true;
        }
    }

    public class CopyConfig
    {
        public string FromConnectionString { get; set; }

        public string ToConnectionString { get; set; }

        public CopyDbConfig[] CopyDbs { get; set; }

        public CopyConfig(string fromConnectionString, string toConnectionString, CopyDbConfig[] copyDbs)
        {
            CopyDbs = copyDbs;
            FromConnectionString = fromConnectionString;
            ToConnectionString = toConnectionString;
        }
    }

    public class CopyDbConfig
    {
        public CopyDbConfig(string from, string to, bool ignore = false)
        {
            From = from;
            To = to;
            Ignore = ignore;
        }

        public string From { get; set; }
        public string To { get; set; }
        public bool Ignore { get; }
    }
}