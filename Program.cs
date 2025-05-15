using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {
        await CorrectionOrthographeEtTraduction();
    }

    public static async Task CorrectionOrthographeEtTraduction()
    {
        Console.WriteLine("Veuillez entrer une phrase :");
        string? phrase = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(phrase))
        {
            Console.WriteLine("Aucune phrase saisie.");
            return;
        }

        string texteCorrige = await CorrigerTexte(phrase);
        Console.WriteLine("\nTexte corrigé :\n" + texteCorrige);

        Console.WriteLine("\nChoisissez la version anglaise :");
        Console.WriteLine("1 - US");
        Console.WriteLine("2 - UK");
        Console.Write("Votre choix : ");
        string? choixUtilisateur = Console.ReadLine()?.Trim();
        string variante = choixUtilisateur == "1" ? "us" :
                          choixUtilisateur == "2" ? "uk" : "inconnu";

        string texteTraduit = "";

        if (variante == "us" || variante == "uk")
        {
            try
            {
                texteTraduit = await TraduireTexteEnAnglais(texteCorrige);
                Console.WriteLine($"\nTraduction ({variante.ToUpper()}):\n" + texteTraduit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la traduction : {ex.Message}");
                texteTraduit = "(Erreur de traduction)";
            }
        }
        else
        {
            Console.WriteLine("Choix non reconnu. Traduction ignorée.");
            texteTraduit = "(Traduction non effectuée)";
        }

        string template = File.ReadAllText("template.html"); 
        string htmlContent = template.Replace("{{phrase}}", phrase) 
                                     .Replace("{{texteCorrige}}", texteCorrige)
                                     .Replace("{{texteTraduit}}", texteTraduit);


        string cheminFichier = "template.html";
        File.WriteAllText(cheminFichier, htmlContent);
        Console.WriteLine("\n✅ Fichier HTML créé : template.html");

        try
        {
            var p = new ProcessStartInfo(cheminFichier) { UseShellExecute = true };
            Process.Start(p);
        }
        catch
        {
            Console.WriteLine("Impossible d’ouvrir automatiquement le navigateur.");
        }
    }

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
            string remplacement = replacements[0].GetProperty("value").GetString() ?? "";
            int offset = match.GetProperty("offset").GetInt32();
            int length = match.GetProperty("length").GetInt32();
            texteCorrige.Remove(offset + decalage, length);
            texteCorrige.Insert(offset + decalage, remplacement);
            decalage += remplacement.Length - length;
        }

        return texteCorrige.ToString();
    }


    static async Task<string> TraduireTexteEnAnglais(string texte)
    {
        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            q = texte,
            source = "fr",
            target = "en",
            format = "text"
        }), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("", content);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("translatedText").GetString() ?? "";
    }
}





