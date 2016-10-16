using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace Depthcharge.Queue.Controllers
{
    [Route("api/[controller]")]
    public class QueueController : Controller
    {
        private static IOptions<DocumentDbSettings> _dbSettings;
        private static DocumentDbClient _documentDbClient;

        public QueueController(IOptions<DocumentDbSettings> dbSettings)
        {
            if (_dbSettings == null)
            {
                _dbSettings = dbSettings;
            }

            if (_documentDbClient == null)
            {
                _documentDbClient = new DocumentDbClient(dbSettings);
            }
        }

        // GET: /<controller>/
        [HttpGet]
        public string Get()
        {
            return _documentDbClient.GetHighestPriorityTask();
        }

        [HttpPost]
        public async Task<string> Post([FromBody] List<QueueItem> queueItems)
        {
            foreach (QueueItem queueItem in queueItems)
            {
                await _documentDbClient.CreateQueueDocumentIfNotExistsAsync(queueItem);
            }

            return "Item posted to queue";
        }

        [HttpPatch]
        public async void Patch([FromBody]QueueItem queueItem)
        {
            await _documentDbClient.MarkAsIndexed(queueItem);
        }
    }
}
