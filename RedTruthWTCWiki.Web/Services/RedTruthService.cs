using HtmlAgilityPack;
using System.Text.Json;

namespace RedTruthWeb.Services;

// Service that replicates the logic of the console program
// The part regarding the input is moved to the UI component in razor
public class RedTruthService
{
    private readonly HttpClient _client;

    // CORS anywhere proxy attached to the wiki URL
    private const string WikiUrl = "https://cors-anywhere.herokuapp.com/https://wiki.whentheycry.org/w/api.php";
    private const string PageTitle = "Red_Truth";

    public RedTruthService(HttpClient client)
    {
        _client = client;

        if (!_client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "RedTruth-Extractor/1.0 (https://github.com/taitorP/Umineko-RedTruth-Extractor)");
        }
    }

    // Output is a List<string> this time to cycle with @foreach
    public async Task<List<string>> GetRedTruthsAsync(string requestedEpisode)
    {
        var redTruthsList = new List<string>();

        try
        {
            string sectionsUrl = $"{WikiUrl}?action=parse&page={PageTitle}&prop=sections&format=json&formatversion=2";
            string sectionsJson = await _client.GetStringAsync(sectionsUrl);
            using JsonDocument doc = JsonDocument.Parse(sectionsJson);

            var sections = doc.RootElement.GetProperty("parse").GetProperty("sections").EnumerateArray();
            var sectionFound = sections.FirstOrDefault(s =>
                s.GetProperty("line").GetString().Equals(requestedEpisode, StringComparison.OrdinalIgnoreCase));

            if (sectionFound.ValueKind == JsonValueKind.Undefined)
            {
                return new List<string>();
            }

            string sectionIndex = sectionFound.GetProperty("index").GetString();

            string contentUrl = $"{WikiUrl}?action=parse&page={PageTitle}&section={sectionIndex}&prop=text&format=json&formatversion=2";
            string contentJson = await _client.GetStringAsync(contentUrl);
            using JsonDocument contentDoc = JsonDocument.Parse(contentJson);
            string htmlContent = contentDoc.RootElement.GetProperty("parse").GetProperty("text").GetString();

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

            // Looking for span containing "color" in their style
            var allSpans = htmlDoc.DocumentNode.SelectNodes("//span[contains(@style, 'color')]");

            if (allSpans != null)
            {
                foreach (var span in allSpans)
                {
                    string style = span.GetAttributeValue("style", "").ToLower();

                    // Checking whether it's the wiki's red (rgb(224, 49, 49)) or the standard red
                    if (style.Contains("rgb(224,49,49)") || style.Contains("red") || style.Contains("#f00"))
                    {
                        // Extracting its parent, to eventually take the leading part
                        var container = span.ParentNode;
                        // If the parent is a span, we keep extracting its parent
                        if (container.Name == "span") container = container.ParentNode;

                        // Initializing the three components
                        string leadingPart = "";
                        string redPart = "";
                        string trailingPart = "";

                        // Cycling every child node of the container
                        foreach (var node in container.ChildNodes)
                        {
                            // If it's the red span we're initializing
                            if (node == span)
                            {
                                redPart = System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
                            }
                            // If it's simple text
                            else if (node.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
                            {
                                string text = System.Net.WebUtility.HtmlDecode(node.InnerText);

                                // If the red part is yet to be found, it's the speaker
                                if (string.IsNullOrEmpty(redPart))
                                {
                                    leadingPart += text;
                                }
                                // If the red part has been found, it's text that follows
                                else
                                {
                                    trailingPart += text;
                                }
                            }
                        }

                        // What the leading part can be made of
                        string speaker = "";
                        string prefixText = "";

                        // If leadingPart contains ":", there must be a speaker
                        if (leadingPart.Contains(":"))
                        {
                            var split = leadingPart.Split(':', 2);
                            speaker = split[0].Trim() + ":";
                            prefixText = split[1].Trim();
                        }
                        else
                        {
                            // If there is no ":", the whole preceding text is a prefix of the sentence, not a name
                            prefixText = leadingPart.Trim();
                        }

                        // Removing the prefix if it's the same as the red part
                        if (!string.IsNullOrEmpty(prefixText) && redPart.StartsWith(prefixText))
                        {
                            prefixText = "";
                        }

                        // Removing what's inside parenthesis, both in the red part and the simple text that follows, using Regex
                        redPart = System.Text.RegularExpressions.Regex.Replace(redPart, @"\s*\(.*?\)\s*", "").Trim();
                        trailingPart = System.Text.RegularExpressions.Regex.Replace(trailingPart, @"\s*\(.*?\)\s*", "").Trim();


                        if (!string.IsNullOrWhiteSpace(redPart))
                        {
                            // Checking for punctuation at the start, if not present add space before trailing part
                            string finalTrailing = trailingPart;

                            if (!string.IsNullOrEmpty(finalTrailing))
                            {
                                char firstChar = finalTrailing.TrimStart()[0];
                                char[] noSpaceChars = { ',', '.', '!', '?', ';', ':' };

                                if (!noSpaceChars.Contains(firstChar))
                                {
                                    finalTrailing = " " + finalTrailing.TrimStart();
                                }
                                else
                                {
                                    finalTrailing = finalTrailing.TrimStart();
                                }
                            }
                            
                            // Joining parts, separated by | for Razor, as Speaker | PrefixText | RedTruth | FinalText
                            string formattedEntry = $"{speaker}|{prefixText} |{redPart}|{trailingPart.Trim()}";

                            if (!redTruthsList.Contains(formattedEntry))
                            {
                                redTruthsList.Add(formattedEntry);
                            }
                        }
                    }
                }
            }

            // Fallback
            if (redTruthsList.Count == 0)
            {
                redTruthsList.Add("No Red Truth was found, the page's style might be different.");
            }

            return redTruthsList;
        }
        catch (Exception ex)
        {
            redTruthsList.Add($"Error: {ex.Message}");
        }

        return redTruthsList;
    }
}