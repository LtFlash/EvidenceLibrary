namespace EvidenceLibrary
{
    public interface IEvidence
    {
        string Id { get; }
        string Description { get; }
        bool Checked { get; }
        bool Collected { get; }

        void Dismiss();
    }
}
