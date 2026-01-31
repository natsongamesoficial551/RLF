using System.Collections.Generic;
using RLF.Core.Identity.Documents;

namespace RLF.Core.Identity.Storage
{
    public interface IIdentityStore
    {
        void Save(IEnumerable<IdentityDocument> documents);
        IEnumerable<IdentityDocument> Load();
    }
}
