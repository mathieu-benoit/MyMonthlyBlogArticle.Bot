namespace MyMonthlyBlogArticle.Bot.Model
{
    public class Feed
    {
        //Key --> Key
        //PartitionKey --> Filterable, Facetable?

        public string Date { get; set; }//Retrievable, Filterable, Sortable

        public string Title { get; set; }//Retrievable, Searchable

        public string Link { get; set; }//Retrievable, Searchable

        public string Type { get; set; }//Facetable?
    }
}
