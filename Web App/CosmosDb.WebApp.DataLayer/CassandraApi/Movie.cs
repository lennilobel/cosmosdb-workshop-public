namespace CosmosDb.WebApp.DataLayer.CassandraApi
{
    public class Movie
    {
        public Movie(string genre, string movieTitle)
        {
            this.Genre = genre;
            this.Title = movieTitle;
        }

        public Movie() { }

        public string Genre { get; set; }  // part key

        public string Title { get; set; }  // id

        public int Year { get; set; }

        public string Length { get; set; }

        public string Description { get; set; }

    }

}
