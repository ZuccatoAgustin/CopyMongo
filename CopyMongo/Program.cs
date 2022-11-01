// See https://aka.ms/new-console-template for more information
using copymongo;

MongoDbConventions.Initialize();

var fromConnectionString = "mongodb://localhost:27017";
var toConnectionString = "mongodb://localhost:27017";
var copyDbs = new[]  {
new CopyDbConfig("x1", "x2")
};

var c = new CopyConfig(fromConnectionString, toConnectionString, copyDbs);

await new CopyDB(c).Copy();