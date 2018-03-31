#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Table;

public class FeedEntity : TableEntity
{
    public static string Manual = "manual";

    public FeedEntity(string rowKey, string partitionKey)
    {
        RowKey = rowKey; 
        PartitionKey = partitionKey;
    }

    public FeedEntity() { }

    public string Date { get; set; }

    public string Title { get; set; }

    public string Link { get; set; }

    public string Type { get; set; }
}
