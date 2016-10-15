using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Depthcharge.Queue
{
    public class DocumentDbClient
    {
        //private static readonly string EndpointUri = Configuration.GetValue<string>("documentDBConnectionString") ?? Environment.GetEnvironmentVariable("APPSETTING_documentDBconnectionString");

        //private static readonly string PrimaryKey =
        // ConfigurationManager.AppSettings["documentDBPrimaryKey"] ?? Environment.GetEnvironmentVariable("APPSETTING_documentDBPrimaryKey");
        private static DocumentClient _documentClient;
        private const string DbName = "Depthcharge";
        private static Uri _indexQueueCollectionLink;
        private const string QueueCollectionName = "IndexQueue";

        public DocumentDbClient([FromServices] IOptions<DocumentDbSettings> appSettings)
        {
            DocumentDbSettings documentDbSettings = appSettings.Value;
            _documentClient = new DocumentClient(new Uri(documentDbSettings.DocumentDBConnectionString), documentDbSettings.DocumentDBPrimaryKey);
            SetupAsync().Wait();
        }


        public static async Task SetupAsync()
        {
            await CreateDatabaseIfNotExistsAsync(DbName);
            await CreateDocumentCollectionIfNotExistsAsync(DbName, QueueCollectionName);
            _indexQueueCollectionLink = UriFactory.CreateDocumentCollectionUri(DbName, QueueCollectionName);
        }

        private static async Task CreateDatabaseIfNotExistsAsync(string databaseName)
        {
            // Check to verify a database with the id=FamilyDB does not exist
            try
            {
                await _documentClient.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist, create a new database
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await _documentClient.CreateDatabaseAsync(new Database { Id = databaseName });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateDocumentCollectionIfNotExistsAsync(string databaseName, string collectionName)
        {
            try
            {
                await _documentClient.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
            }
            catch (DocumentClientException de)
            {
                // If the document collection does not exist, create a new collection
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    DocumentCollection collectionInfo = new DocumentCollection
                    {
                        Id = collectionName,
                        IndexingPolicy = new IndexingPolicy(new RangeIndex(Microsoft.Azure.Documents.DataType.String) { Precision = -1 })
                    };

                    // Configure collections for maximum query flexibility including string range queries.

                    // Here we create a collection with 400 RU/s.
                    await _documentClient.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseName),
                        collectionInfo,
                        new RequestOptions {});
                }
                else
                {
                    throw;
                }
            }
        }

        private static string ProtocolUrlSplit(string url)
        {
            string[] split;
            if (url.StartsWith("http://"))
            {
                split = url.Split(new string[] {"http://"}, StringSplitOptions.None);
            }
            else if (url.StartsWith("https://"))
            {
                split = url.Split(new string[] {"https://"}, StringSplitOptions.None);
            }
            else
            {
                return url;
            }

            return split[1];
        }

        public async Task CreateQueueDocumentIfNotExistsAsync(QueueItem queueItemToInsert)
        {
            string query = $"Select * FROM {QueueCollectionName} q WHERE q.url = '{ProtocolUrlSplit(queueItemToInsert.Url)}'";

            Document queueItem = _documentClient.CreateDocumentQuery(_indexQueueCollectionLink, query)
                .AsEnumerable()
                .FirstOrDefault();

            if (queueItem == null)
            {
                await
                    _documentClient.CreateDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DbName, QueueCollectionName), queueItemToInsert);
            }
            else
            {
                queueItem.SetPropertyValue("priority", (queueItem.GetPropertyValue<int>("priority") + 1 ));
                //queueItem.Priority = ++queueItem.Priority;
                
                await _documentClient.ReplaceDocumentAsync(queueItem);
            }
        }

        public string GetHighestPriorityTask()
        {
            QueueItem queueItem = _documentClient.CreateDocumentQuery<QueueItem>(_indexQueueCollectionLink)
                .AsEnumerable()
                .Where(x => x.Indexed == false)
                .OrderByDescending(x => x.Priority)
                .AsEnumerable() //have to make Enumerable first due to only collections being selectable at this point in the DocumentDB libraries.
                .FirstOrDefault();

            return (queueItem != null) ? $"{queueItem.Protocol}://{queueItem.Url}" : null;
        }


        public async Task MarkAsIndexed(QueueItem queueItemToUpdate)
        {
            string query = $"Select * FROM {QueueCollectionName} q WHERE q.url = '{ProtocolUrlSplit(queueItemToUpdate.Url)}'";
            Document queueItem = _documentClient.CreateDocumentQuery(_indexQueueCollectionLink, query)
                .AsEnumerable()
                .FirstOrDefault();

            queueItem.SetPropertyValue("indexed", true);
            await _documentClient.ReplaceDocumentAsync(queueItem);
        }
    }
}
