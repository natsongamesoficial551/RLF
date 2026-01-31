using RLF.Core.Identity.Enums;
using System;
using System.Collections.Generic;

namespace RLF.Core.Identity.Documents
{
    public class IdentityDocument
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DocumentType Type { get; }
        public DocumentStatus Status { get; internal set; }

        public DateTime IssuedAt { get; internal set; }
        public DateTime? ExpiresAt { get; internal set; }
        public DateTime LastStatusChangeAt { get; internal set; }

        public string Reason { get; internal set; }

        public Dictionary<string, string> Metadata { get; }

        public IdentityDocument(DocumentType type)
        {
            Type = type;
            Status = DocumentStatus.Missing;
            IssuedAt = DateTime.UtcNow;
            LastStatusChangeAt = IssuedAt;
            Metadata = new Dictionary<string, string>();
        }
    }
}
