// ===============================
// IUberStore.cs
// ===============================
using RLF.GTA.Jobs.Uber.Core;
using RLF.GTA.Jobs.Uber.History;

namespace RLF.GTA.Jobs.Uber.Storage
{
    public interface IUberStore
    {
        void Save(UberAccount account, RideHistory history);
        (UberAccount account, RideHistory history) Load();
    }
}