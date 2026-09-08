namespace Domain.Common
{
    public interface IAuditable
    {
        string? CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string? LastModifiedBy { get; set; }
        DateTime LastModifiedDate { get; set; }
    }
}

