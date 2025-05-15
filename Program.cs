using System;

using System.Net.Http;

using System.Text;

using System.Threading.Tasks;

using System.Text.Json;
class Program

{
static async Task Main(string[] args)
{
    await CorrectionOrthographeEtTraduction();
}

public static async Task CorrectionOrthographeEtTraduction()
{
    Console.WriteLine("Veuillez entrer une phrase :");
    string phrase = Console.ReadLine();
    if (!string.IsNullOrEmpty(phrase))
    {
       
        string texteCorrige = await CorrigerTexte(phrase);
        Console.WriteLine("\n Texte corrigé :\n" + texteCorrige);
      
        Console.WriteLine("\nSouhaitez-vous traduire en anglais US ou UK ? (tapez 'us' ou 'uk')");
        string choix = Console.ReadLine()?.Trim().ToLower();
        if (choix == "us" || choix == "uk")
        {
            string texteTraduit = await TraduireTexteEnAnglais(texteCorrige, choix);
            Console.WriteLine($"\n Traduction en anglais ({choix.ToUpper()}):\n" + texteTraduit);
        }
        else
        {
            Console.WriteLine(" Langue non reconnue. Traduction ignorée.");
        }
    }
    else
    {
        Console.WriteLine("Aucune phrase saisie.");
    }
}
// Méthode de correction avec LanguageTool
static async Task<string> CorrigerTexte(string texte)
{
    using var client = new HttpClient();
    var content = new StringContent($"language=fr&text={Uri.EscapeDataString(texte)}", Encoding.UTF8, "application/x-www-form-urlencoded");
    var response = await client.PostAsync("https://api.languagetool.org/v2/check", content);
    var json = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(json);
    var matches = doc.RootElement.GetProperty("matches");
    var texteCorrige = new StringBuilder(texte);
    int decalage = 0;
    foreach (var match in matches.EnumerateArray())
    {
        var replacements = match.GetProperty("replacements");
        if (replacements.GetArrayLength() == 0) continue;
        string remplacement = replacements[0].GetProperty("value").GetString();
        int offset = match.GetProperty("offset").GetInt32();
        int length = match.GetProperty("length").GetInt32();
        texteCorrige.Remove(offset + decalage, length);
        texteCorrige.Insert(offset + decalage, remplacement);
        decalage += remplacement.Length - length;
    }
    return texteCorrige.ToString();
}
// Méthode de traduction avec l'API LibreTranslate (ou DeepL si tu as une clé)
static async Task<string> TraduireTexteEnAnglais(string texte, string variante)
{
    using var client = new HttpClient();
    // Exemple avec l'API gratuite LibreTranslate (https://libretranslate.de)
    var content = new StringContent(JsonSerializer.Serialize(new
    {
        q = texte,
        source = "fr",
        target = variante == "us" ? "en" : "en",  // Même code pour US/UK, la différence serait dans le style
        format = "text"
    }), Encoding.UTF8, "application/json");
    var response = await client.PostAsync("https://libretranslate.de/translate", content);
    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.GetProperty("translatedText").GetString();
}
}
 