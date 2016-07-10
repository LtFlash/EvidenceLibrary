namespace EvidenceLibrary
{
    public interface IEvidence
    {
        string Id { get; }
        bool Checked { get; }
        bool Collected { get; }
    }

    public interface IMaterialEvidence
    {

    }
}
