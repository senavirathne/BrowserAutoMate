namespace AirBnd;

class Program
{
    static async Task Main(string[] args)
    {
        var scrapeAirBnB = new ScrapeSite(
            "https://www.airbnb.com",
            "/s/United-Kingdom/homes?tab_id=home_tab",
            "/api/v3/StaysPdpSections",
            new HashSet<string>()
            {
                "document", "script"
            }
        );
        await scrapeAirBnB.Initiate();
    }
}