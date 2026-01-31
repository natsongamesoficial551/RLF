using RLF.Core.Identity.Documents;
using System.Collections.Generic;

namespace RLF.Core.Identity.Storage
{
    public interface IIdentityStore
    {
        void Save(IEnumerable<IdentityDocument> documents);
        IEnumerable<IdentityDocument> Load();
    }
}
