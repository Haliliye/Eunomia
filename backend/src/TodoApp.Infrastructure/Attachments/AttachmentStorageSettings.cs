namespace TodoApp.Infrastructure.Attachments;

public class AttachmentStorageSettings
{
    public const string SectionName = "AttachmentStorage";

    /// <summary>Relative or absolute path where uploaded files are written. Defaults to a
    /// folder next to the API's working directory — fine for a single-instance dev/demo
    /// deployment, but doesn't survive a container being recreated without a mounted volume,
    /// and doesn't work across multiple API instances behind a load balancer (see README).</summary>
    public string RootPath { get; set; } = "App_Data/attachments";
}
