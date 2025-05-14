using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("👉 Entrez un texte à corriger automatiquement :");
        string originalText = Console.ReadLine();

        string url = "https://api.languagetoolplus.com/v2/check";
        string data = $"language=fr&text={Uri.EscapeDataString(originalText)}";

        var content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

        using var client = new HttpClient();
        var response = await client.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();

        var toolResponse = JsonConvert.DeserializeObject<LanguageToolResponse>(result);

        var matches = toolResponse.matches
            .Where(m => m.replacements != null && m.replacements.Count > 0)
            .OrderByDescending(m => m.offset) 
            .ToList();

        var correctedText = originalText;

        foreach (var match in matches)
        {
            int start = match.offset;
            int length = match.length;
            string replacement = match.replacements[0].value;

            // Remplacer la portion du texte avec la suggestion
            correctedText = correctedText.Remove(start, length).Insert(start, replacement);
        }

        Console.WriteLine("\n✅ Texte corrigé automatiquement :");
        Console.WriteLine(correctedText);
    }
}


public class Replacement
{
    public string value { get; set; }
}

public class Match
{
    public int offset { get; set; }
    public int length { get; set; }
    public string message { get; set; }
    public List<Replacement> replacements { get; set; }
}

public class LanguageToolResponse
{
    public List<Match> matches { get; set; }
}

