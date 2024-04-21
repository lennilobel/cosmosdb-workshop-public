namespace CosmosDb.WebApp.DataLayer.GremlinApi
{
    public class AirportQueryRequest
    {
        public string ArrivalGate { get; set; }
        public string DepartureGate { get; set; }
        public int MinYelpRating { get; set; }
        public bool FirstSwitchTerminalsThenEat { get; set; }
    }
}
