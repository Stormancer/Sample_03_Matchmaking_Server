using Newtonsoft.Json.Linq;
using Server.Database;
using Stormancer.Server.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Serials
{
    class SerialsService : ISerialsService
    {
        private readonly IESClientFactory _factory;
        private readonly string _index;
        public SerialsService(IESClientFactory esFactory, IEnvironment environment)
        {
            _factory = esFactory;
            _index = (string)(environment.Configuration.index);
        }

        /// <summary>
        /// Creates a serial key and associate it with 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<string> CreateKey(JObject content, string creatorId, string comment)
        {
            var client = await GetClient();
            var key = CodeGenerator.GetNext(4, 4, '-');
            var found = false;
            do
            {
                var keyResult = await client.DocumentExistsAsync<SerialKey>(key);
                if (keyResult.Exists)
                {
                    key = CodeGenerator.GetNext(4, 4, '-');
                    found = true;
                }
            }
            while (found);
            var serial = new SerialKey { Status = KeyStatus.Available, Id = key, Content = content };
            serial.AuditTrack.Add(new AuditEntry { Action = "key created", Author = creatorId, Comment = comment, Date = DateTime.UtcNow });
            var result = await client.IndexAsync(serial);

            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Failed to create key : {result.ServerError.Error.Reason}");
            }
            return key;
        }

        public async Task ChangeKeyStatus(string key, KeyStatus status, string adminId, string comment)
        {
            var client = await GetClient();

            var serialResult = await client.GetAsync<SerialKey>(key);
            if (!serialResult.Found)
            {
                throw new InvalidOperationException("Serial key not found in the database.");
            }

            var serial = serialResult.Source;

            if (serial.Status == KeyStatus.Revoked)
            {
                throw new InvalidOperationException("Serial key already revoked");

            }
            var oldStatus = serial.Status;
            serial.Status = status;
            serial.AuditTrack.Add(new AuditEntry { Action = $"Changed status from {oldStatus} to {status}", Author = adminId, Comment = comment, Date = DateTime.UtcNow });

            var result = await client.IndexAsync(serial);

            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Failed to change key status : {result.ServerError.Error.Reason}");
            }
        }

        public async Task<IEnumerable<SerialKey>> QueryKeys(string querytext, int skip, int count)
        {
            var client = await GetClient();

            var results = await client.SearchAsync<SerialKey>(s => s.Query(m=>m.QueryString(q=>q.Query(querytext))).Skip(skip).Size(count).IgnoreUnavailable());


            if (!results.IsValid)
            {
                throw new InvalidOperationException($"Failed query  keys : {results.ServerError.Error.Reason}");
            }
            return results.Documents;

        }

        public async Task ActivateKey(string key, string ownerId, string activatorId = "user", string comment = "")
        {
            var client = await GetClient();

            var serialResult = await client.GetAsync<SerialKey>(key);
            if (!serialResult.Found)
            {
                await Task.Delay(2000);
                throw new InvalidOperationException("Serial key not found in the database.");
            }

            var serial = serialResult.Source;
            if (serial.Status != KeyStatus.Available)
            {
                await Task.Delay(2000);
                throw new InvalidOperationException("Serial key revoked or already activated.");
            }

            serial.Status = KeyStatus.Activated;
            serial.Owner = ownerId;
            serial.AuditTrack.Add(new AuditEntry { Action = "key activated", Author = activatorId, Comment = comment, Date = DateTime.UtcNow });

            await client.IndexAsync(serial);
        }


        private Task<Nest.IElasticClient> GetClient()
        {
            return _factory.CreateClient(_index);


        }

        public async Task<SerialKey> Get(string id)
        {
            var client = await GetClient();
            var serialResult = await client.GetAsync<SerialKey>(id);
            if(!serialResult.Found)
            {
                return null;
            }
            if (!serialResult.IsValid)
            {
                throw new InvalidOperationException($"An unexepected error occured while getting the serial : {serialResult.ServerError.Error.Reason}");
            }
            return serialResult.Source;
        }
    }
    public class SerialKeySummary
    {
        public string Key { get; set; }
        public string Status { get; set; }
    }
    public class SerialKey
    {
        public SerialKey()
        {
            AuditTrack = new List<AuditEntry>();
        }
        public string Id { get; set; }

        public List<AuditEntry> AuditTrack { get; set; }

        public string Owner { get; set; }
        public KeyStatus Status { get; set; }
        public JObject Content { get; set; }
    }

    public enum KeyStatus
    {
        Available,
        Activated,
        Revoked,
    }
    public class AuditEntry
    {
        public string Action { get; set; }
        public string Comment { get; set; }

        public string Author { get; set; }

        public DateTime Date { get; set; }
    }

    public static class CodeGenerator
    {
        // Similar sets have been omitted, like 1,I; Q,O,0; 
        private static readonly char[] alphabet = "ABCDEFGHJKLMNPRSTUVWXYZ23456789".ToCharArray();
        private static readonly Random rand = new Random();
        public static string GetNext(int segmentLength, int segments, char separatorChar)
        {
            var codeLength = segmentLength * segments;
            char[] randChars = randomAlphabetChars(codeLength);
            char[] formattedChars = new char[codeLength + (codeLength / segmentLength) - 1];
            int numberOfSeparators = 0;
            for (int i = 0; i < randChars.Length; i++)
            {
                if (i % segmentLength == 0 && i != 0)
                    formattedChars[i + numberOfSeparators++] = separatorChar;
                formattedChars[i + numberOfSeparators] = randChars[i];
            }
            return new string(formattedChars);
        }
        private static char[] randomAlphabetChars(int length)
        {
            char[] newChars = new char[length];
            for (int i = 0; i < length; i++)
                newChars[i] = alphabet[(int)Math.Truncate(rand.NextDouble() * 1000) % alphabet.Length];
            return newChars;
        }
    }
}
