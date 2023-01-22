using Microsoft.Playwright;

namespace AirBnd;

public class ScrapeSite
{
    private readonly string _domain;
    private readonly string _homePage;
    private readonly string _requestStart;
    private readonly HashSet<string> _minimumFilesTypes;
    public ScrapeSite(string domain, string homePage, string requestStart, HashSet<string> minimumFilesTypes)
    {
        _domain = domain;
        _homePage = homePage;
        _requestStart = requestStart;
        _minimumFilesTypes = minimumFilesTypes;
    }

    public async Task Initiate()
    {
        var anchors = new HashSet<string>();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
        });

        var context = await browser.NewContextAsync();
        await context.RouteAsync(r =>true, route =>
        
        {
            
            if (_minimumFilesTypes.Contains(route.Request.ResourceType) || route.Request.Url.StartsWith(_domain + _requestStart))
                route.ContinueAsync();
            else
                route.AbortAsync();
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_domain + _homePage);

        // filter anchorElements
        var anchorElements = await page.QuerySelectorAllAsync("a[target^='listing_']");

        Console.WriteLine(anchorElements.Count);
        var page2 = await context.NewPageAsync();
        foreach (var anchorElement in anchorElements)
        {
            // to avoid multiple Elements with same target
            var target = await anchorElement.EvaluateAsync<string>("(anchor) => anchor.target");
            if (anchors.Contains(target)) continue;
            anchors.Add(target);
            Console.WriteLine(target);

            try
            {
                await page2.GotoAsync(_domain + await anchorElement.GetAttributeAsync("href") ??
                                      throw new InvalidOperationException("anchorElement's URL is not valid"));
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                continue; // metanin loop ek continue wenwada
            }

            var jsonRequest = await page2.WaitForRequestAsync(
                req => req.Url.StartsWith(_domain + _requestStart));
            var jsonResponse = await jsonRequest.ResponseAsync();

            if (jsonResponse != null)
            {
                await SaveDataToFile(jsonResponse, $"dataFile{jsonRequest.GetHashCode()}.json");
            }
            break;
            
            // await Task.Delay(TimeSpan.FromSeconds(5));
        }

        Console.ReadKey();
        await page2.CloseAsync();
    }

    static async Task SaveDataToFile(IResponse jsonResponse, string fileName)
    {
        await using (var stream = new FileStream(fileName, FileMode.Create))
        await using (var writer = new StreamWriter(stream))
        {
            dynamic json = (await jsonResponse.JsonAsync())!;
            await writer.WriteAsync(json.ToString());
        }

        Console.WriteLine("json file saved");
    }
}