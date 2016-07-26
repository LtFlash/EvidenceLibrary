using System.Collections.Generic;
using Rage;

namespace EvidenceLibrary
{
    public interface IEvidence
    {
        string Id { get; }
        string Description { get; }
        bool Checked { get; }
        bool Collected { get; }
        bool IsImportant { get; }
        List<ETraces> Traces { get; }
        float DistanceCanBeActivated { get; set; }
        Vector3 EvidencePosition { get; }

        void Dismiss();
    }
}
