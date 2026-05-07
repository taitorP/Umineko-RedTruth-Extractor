using HtmlAgilityPack;
using System.Net.Http;
using System.Text.Json;
using System.Xml;

string pageTitle = "Red_Truth";
string wikiUrl = "https://wiki.whentheycry.org/w/api.php";
string requestedEpisode;

// User input for the episode to look up red truths of
Console.WriteLine("What episode do you want to know the red truths of?");
while (true)
{
    string? inputEpisode = Console.ReadLine();

    // Pattern matching to select the right episode, ignoring case and accepting a few variations of the original title, including the episode number
    requestedEpisode = inputEpisode.Trim().ToLower() switch
    {
        "1" or "episode 1" or "legend" or "legend of the golden" or "legend of the witch" or "legend of the golden witch" => "Legend of the Golden Witch",
        "2" or "episode 2" or "turn" or "turn of the golden" or "turn of the witch" or "turn of the golden witch" => "Turn of the Golden Witch",
        "3" or "episode 3" or "banquet" or "banquet of the golden" or "banquet of the witch" or "banquet of the golden witch" => "Banquet of the Golden Witch",
        "4" or "episode 4" or "alliance" or "alliance of the golden" or "alliance of the witch" or "alliance of the golden witch" => "Alliance of the Golden Witch",
        "5" or "episode 5" or "end" or "end of the golden" or "end of the witch" or "end of the golden witch" => "End of the Golden Witch",
        "6" or "episode 6" or "dawn" or "dawn of the golden" or "dawn of the witch" or "dawn of the golden witch" => "Dawn of the Golden Witch",
        "7" or "episode 7" or "requiem" or "requiem of the golden" or "requiem of the witch" or "requiem of the golden witch" => "Requiem of the Golden Witch",
        "8" or "episode 8" or "twilight" or "twilight of the golden" or "twilight of the witch" or "twilight of the golden witch" => "Twilight of the Golden Witch",
        _ => "invalid"

    };

    // Red truths start appearing from episode 2, so requesting episode 1 prints a message as a warning
    if (requestedEpisode == "Legend of the Golden Witch")
    {
        Console.WriteLine("No red truths were uttered in the Legend of the Golden Witch yet, try with a different episode");
        continue;
    }

    if (requestedEpisode == "invalid")
    {
        Console.WriteLine("Invalid input, try again");
        continue;
    }
    break;
}

// Using "using" to get http client open and close it when it's not needed anymore
using HttpClient client = new HttpClient();

// Presenting myself to make requests
string myUserAgent = "RedTruthProject/1.0 (contact: pasqualebressi27@gmail.com)";
client.DefaultRequestHeaders.Add("User-Agent", myUserAgent);

try
{
    // Getting list of all sections
    // First asking for a string then parsing it into a .json
    string sectionsUrl = $"{wikiUrl}?action=parse&page={pageTitle}&prop=sections&format=json&formatversion=2";
    string sectionsJson = await client.GetStringAsync(sectionsUrl);
    using JsonDocument doc = JsonDocument.Parse(sectionsJson);

    // Turning the .json into an JsonElement.ArrayEnumerator to iterate, first selecting the "parse" property and then the "sections" property 
    var sections = doc.RootElement.GetProperty("parse").GetProperty("sections").EnumerateArray();

    // LINQ to iterate the objects s of the array, look at the "line" property
    // and if the text equals requestedEpisode, it stops and it returns the object
    var sectionFound = sections.FirstOrDefault(s =>
        s.GetProperty("line").GetString().Equals(requestedEpisode, StringComparison.OrdinalIgnoreCase));

    // If the section is not found, it returns a message and it stops
    if (sectionFound.ValueKind == JsonValueKind.Undefined)
    {
        Console.WriteLine($"Section '{requestedEpisode}' not found.");
        return;
    }

    // If the section is found, get the index property and print a message
    string sectionIndex = sectionFound.GetProperty("index").GetString();
    Console.WriteLine($"Found section '{requestedEpisode}' with index: {sectionIndex}");

    // Using the index to get the section's content as a string
    string contentUrl = $"{wikiUrl}?action=parse&page={pageTitle}&section={sectionIndex}&prop=text&format=json&formatversion=2";
    string contentJson = await client.GetStringAsync(contentUrl);

    // Turning the string requested into a .json and extracting the text property inside the parse property from the html as a string
    using JsonDocument contentDoc = JsonDocument.Parse(contentJson);
    string htmlContent = contentDoc.RootElement.GetProperty("parse").GetProperty("text").GetString();

    // Loading an html document from the found content as a string
    HtmlDocument htmlDoc = new HtmlDocument();
    htmlDoc.LoadHtml(htmlContent);

    // Removing all [edit] links from the cleaned text
    var editNodes = htmlDoc.DocumentNode.SelectNodes("//span[@class='mw-editsection']");
    if (editNodes != null)
    {
        foreach (var node in editNodes)
        {
            node.Remove();
        }
    }

    // "Cleaning" the content of all tags and keeping the text olny
    string cleanedText = System.Net.WebUtility.HtmlDecode(htmlDoc.DocumentNode.InnerText).Trim();

    // Decoding special symbols 
    cleanedText = System.Net.WebUtility.HtmlDecode(cleanedText);

    // Printing the section's text
    Console.WriteLine();
    Console.WriteLine(cleanedText);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}