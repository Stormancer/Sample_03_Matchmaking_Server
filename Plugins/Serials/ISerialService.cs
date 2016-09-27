using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Plugins.Serials
{
    public interface ISerialsService
    {
        Task<string> CreateKey(JObject content, string creatorId, string comment);

        Task ChangeKeyStatus(string key, KeyStatus status, string adminId, string comment);

        Task<IEnumerable<SerialKey>> QueryKeys(string querytext, int skip, int count);

        /// <summary>
        /// Activates a key and associate it with an user
        /// </summary>
        /// <param name="key">The key to activate.</param>
        /// <param name="ownerId">The user to which the key is to be associated.</param>
        /// <param name="activatorId">The admin user who is activating the key (optional, is null if the player activates the key himself)</param>
        /// <param name="comment">Optional comment</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The key does not exist in the db, or is revoked.</exception>
        Task ActivateKey(string key, string ownerId, string activatorId = null, string comment = "");

        Task<SerialKey> Get(string id);
    }
}
