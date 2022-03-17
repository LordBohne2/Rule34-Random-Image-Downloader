using AngleSharp;
using AngleSharp.Dom;
using System.Net;

HttpResponseMessage response;
HttpClient client = new();
Random rnd = new();

IDocument doc;
IElement src;
var config = Configuration.Default;
var context = BrowsingContext.New(config);

int randomImageNum = 0;

// Folder
string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
string fileTyp;

// User Input
int select;
int howManyDownloads = 0;
string tag = "";

bool repeat;

// Web
string html;
string url;
int counter = 0;

// User Input
try
{
    Console.WriteLine("[1] Enter Tag\n[2] All\n[other] Exit");
    select = Convert.ToInt16(Console.ReadLine());
}
catch (Exception)
{
    return;
}

if (select != 1 && select != 2)
    return;

do
{
    repeat = false;

    try
    {
        Console.WriteLine("How Many Downloads [Max 20] (Per Image about 3 Request to Rule34)");
        howManyDownloads = Convert.ToInt16(Console.ReadLine());

        if (howManyDownloads > 20)
        {
            Console.WriteLine("No more than 20");
            repeat = true;
        }
            
        if (howManyDownloads <= 0)
        {
            Console.WriteLine("No less than 1");
            repeat = true;
        }       
    }
    catch (FormatException fo)
    {
        Console.WriteLine("No Letters allowed!\n" + fo);
        repeat = true;
    }
} while (repeat);

Console.WriteLine();

// Search and Downloader Loop
while (counter < howManyDownloads)
{
    counter++;
    Console.WriteLine("Image: " + counter);

    if (select == 1) // Tag
    {
        do
        {
            if (counter == 1) // Only one time enter
            {
                Console.WriteLine("Tag Name: ");
                tag = Console.ReadLine();
            }

            if (tag.Contains(' '))
                tag = tag.Replace(' ', '_');

        } while (string.IsNullOrEmpty(tag));

        url = $"https://rule34.xxx/index.php?page=post&s=list&tags={tag}&pid=0"; // For Max Pics

        response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        html = await response.Content.ReadAsStringAsync();
        doc = await context.OpenAsync(req => req.Content(html));

        int foundImages = GetMaxImages(doc);
        if (foundImages > 200000)
            foundImages = 200000;

        randomImageNum = rnd.Next(foundImages);

        if (randomImageNum != 0)
        {
            url = $"https://rule34.xxx/index.php?page=post&s=list&tags={tag}&pid={randomImageNum}"; // Random Pics

            response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync();
            doc = await context.OpenAsync(req => req.Content(html));
        }
    }
    else // Without Tag
    {   // Not more than 200000 supportet *blank site*
        url = $"https://rule34.xxx/index.php?page=post&s=list&tags=all&pid={rnd.Next(200000)}"; // Ramdom All

        response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        html = await response.Content.ReadAsStringAsync();
        doc = await context.OpenAsync(req => req.Content(html));
    }

    // Get Page Link
    var a = doc.QuerySelectorAll("div.content > div.image-list > span.thumb > a"); // Get all Images Links
    Console.WriteLine($"Found {a.Length} Images on the URL: {url}");
    string href = a[rnd.Next(a.Length)].GetAttribute("href"); // Get random Page
    Console.WriteLine($"Go to: {href}");

    // Enter Image Page to Get Image Src
    doc = await context.OpenAsync(req => req.Content(GetHTML($"https://rule34.xxx/{href}")));

    src = doc.QuerySelector("div.flexi > div > img"); // Search for Image (or gif)
    Console.WriteLine("<img> Tag Found");

    if (src == null)
    {
        src = doc.QuerySelector("div.flexi > div > div > video > source"); // Search for Video
        Console.WriteLine("<video> Tag Found");

        if (src == null)
        {
            Console.WriteLine("!!!NO SRC FOUND!!!");
            return;
        }
    }

    string finalImageSRC = src.GetAttribute("src");

    if (finalImageSRC.Contains(".gif"))
        fileTyp = ".gif";
    else if (finalImageSRC.Contains(".mp4"))
        fileTyp = ".mp4";
    else
        fileTyp = ".jpg";

    Console.WriteLine("Final File Results:\n------------------------------");
    Console.WriteLine("Src: " + finalImageSRC);
    Console.WriteLine("Tags: " + src.GetAttribute("alt"));
    Console.WriteLine("File Typ: " + fileTyp);
    

    // Folder
    string title = finalImageSRC;
    string[] titleArr = title.Split("?");
    title = titleArr[titleArr.Length - 1];

    if (title.Contains(':') || title.Contains('/') || title.Contains(@"\")) // Remove Symboles Folder/File dont support
    {
        title = title.Replace(':', ' '); title.Replace('/', ' '); title.Replace(@"\", " ");
    }

    Console.WriteLine("Title: " + title);
    string fullPath = desktopPath + @"\Rule34";

    if (!File.Exists(fullPath))
        Directory.CreateDirectory(fullPath);

    Console.WriteLine("Full Path: " + fullPath);
    Console.WriteLine("------------------------------\n");

    try // Downlaod File
    {
        byte[] imageBytes = await client.GetByteArrayAsync(src.GetAttribute("src"));
        File.WriteAllBytes(fullPath + "/" + title + fileTyp, imageBytes);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
Console.WriteLine("Thanks for using my bad programm\nCreator: LordBohne2\nPress any key to close");
Console.ReadKey();

string GetHTML(string url)
{
    try
    {
        var myUri = new Uri(url);
        // Create a 'HttpWebRequest' object for the specified url. 
        var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(myUri);
        // Set the user agent as if we were a web browser
        myHttpWebRequest.UserAgent = @"Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.4) Gecko/20060508 Firefox/1.5.0.4";

        var myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
        var stream = myHttpWebResponse.GetResponseStream();
        var reader = new StreamReader(stream);
        html = reader.ReadToEnd();
        // Release resources of response object.
        myHttpWebResponse.Close();
    }
    catch (WebException ex)
    {
        using (var sr = new StreamReader(ex.Response.GetResponseStream()))
            html = sr.ReadToEnd();
    }

    return html;
}

int GetMaxImages(IDocument doc)
{
    var aArr = doc.QuerySelectorAll("div.pagination > a"); // Get href Array from Buttons

    if (aArr == null || aArr.Length == 0) // If not found / not exists
        return 0;

    Console.WriteLine("<a> Array Length: " + aArr.Length);
    string href = aArr[aArr.Length - 1].GetAttribute("href"); // Get Last Button For Max Images
    string[] stringSplitArr = href.Split("="); // Split to Array

    Console.WriteLine("Found Images: " + stringSplitArr[stringSplitArr.Length - 1]);

    return int.Parse(stringSplitArr[stringSplitArr.Length - 1]); // Get Final Number
}
