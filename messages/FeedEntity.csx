#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Table;

interface IFeedEntity
{
    string Date { get; set; }

    string Title { get; set; }

    string Link { get; set; }

    string Type { get; set; }
}

public class FeedEntityForSearch : IFeedEntity
{
    //Key --> Key
    //PartitionKey --> Filterable, Facetable?

    public string Date { get; set; }//Retrievable, Filterable, Sortable

    public string Title { get; set; }//Retrievable, Searchable

    public string Link { get; set; }//Retrievable, Searchable

    public string Type { get; set; }//Facetable?
}

public class FeedEntityForTable : TableEntity, IFeedEntity
{
    public static string Manual = "manual";

    public FeedEntity(string rowKey, string partitionKey)
    {
        RowKey = rowKey; 
        PartitionKey = partitionKey;
    }

    public FeedEntityForTable() { }

    public string Date { get; set; }

    public string Title { get; set; }

    public string Link { get; set; }

    public string Type { get; set; }
}
