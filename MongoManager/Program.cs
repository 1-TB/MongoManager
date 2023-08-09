using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Spectre.Console;

namespace MongoManager;

class MongoManager
{
  

    static void Main(string[] args)
    {
        
        if (!File.Exists(Settings._savePath))
        {
            // theres no settings file, so we need to create one
            AnsiConsole.MarkupLine("[bold red]No settings file found, creating one now...[/]");
            AnsiConsole.MarkupLine("[bold red]Please enter your connection info[/]");
            Settings._connectionString = AnsiConsole.Ask<string>("Connection String: ");
            Settings._databaseName = AnsiConsole.Ask<string>("Database Name: ");
            Settings._collectionName = AnsiConsole.Ask<string>("Collection Name: ");
            //save the settings
            Settings.Save();

        }
            AnsiConsole.MarkupLine("[bold red]Settings file found, loading settings...[/]");
            Settings.Load();
            //start the main loop
            while (true)
            {
                Console.Clear();
                var menu = new Dictionary<string, string>()
                {
                    {"1", "Create document"},
                    {"2", "Read document"},
                    {"3","Show all documents"},
                    {"4", "Update document"},
                    {"5", "Delete document"},
                    {"6", "Settings"},
                    {"7", "Exit"}
                };
                AnsiConsole.Write(FormatHeader("Mongo Manager"));
                AnsiConsole.Write(FormatMenu(menu));
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("Choose an option:")
                        .PageSize(menu.Count)
                        .AddChoices(Enumerable.Range(1, menu.Count))
                );
                switch (choice)
                {
                    case 1:
                        CreateDocument();
                        break;
                    case 2:
                        ReadDocuments();
                        break;
                    case 3:
                        ShowAllDocuments();
                        break;
                    case 4:
                        UpdateDocument();
                        break;
                    case 5:
                        DeleteDocument();
                        break;
                    case 6:
                        SettingsMenu();
                        break;
                    case 7:
                        Environment.Exit(0);
                        break;
                    
                }
            }


    }

    private static void ShowAllDocuments()
    {
        //show all documents in the collection
        Console.Clear();
        var collection = Database.Collection;
        var count = collection.CountDocuments(new BsonDocument());
        if (count == 0)
        {
            AnsiConsole.MarkupLine("[bold red]No documents found.[/]");
            Console.ReadKey();
            return;
        }

        if (count > 50)
        {
            AnsiConsole.MarkupLine($"[bold red]There are {count} documents in the collection, are you sure you want to show them all?[/]");
            var confirm = AnsiConsole.Confirm("Show all documents?");
            if (!confirm)
            {
                return;
            }
        }
        var documents = collection.Find(new BsonDocument()).ToList();
        
        foreach (var document in documents)
        {
            AnsiConsole.Write(FormatFromDocument(document));
        }
        Console.ReadKey();
    }

    private static void SettingsMenu()
    {
        //show the settings menu and allow the user to change the settings
        Console.Clear();
        var menu = new Dictionary<string, string>()
        {
            {"1", "Change Connection String"},
            {"2", "Change Database Name"},
            {"3", "Change Collection Name"},
            {"4", "Show Values"},
            {"5", "Back"}
        };
        AnsiConsole.Write(FormatHeader("Settings"));
        AnsiConsole.Write(FormatMenu(menu));
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<int>()
                .Title("Choose an option:")
                .PageSize(menu.Count)
                .AddChoices(Enumerable.Range(1, menu.Count))
        );
        switch (choice)
        {
            case 1:
                Settings._connectionString = AnsiConsole.Ask<string>("Connection String: ");
                Settings.Save();
                break;
            case 2:
                Settings._databaseName = AnsiConsole.Ask<string>("Database Name: ");
                Settings.Save();
                break;
            case 3:
                Settings._collectionName = AnsiConsole.Ask<string>("Collection Name: ");
                Settings.Save();
                break;
            case 4:
                AnsiConsole.MarkupLine($"[yellow]Connection String: {Settings._connectionString}[/]");
                AnsiConsole.MarkupLine($"[yellow]Database Name: {Settings._databaseName}[/]");
                AnsiConsole.MarkupLine($"[yellow]Collection Name: {Settings._collectionName}[/]");
                Console.ReadKey();
                break;
            case 5:
                break;
        }
    }

    static void CreateDocument()
    {
        var collection = Database.Collection;
        Console.Clear();
        AnsiConsole.Write(FormatHeader("Create Document"));

        var filterJson = GetFilter();
        

        var document = BsonDocument.Parse(filterJson);
        collection.InsertOne(document);

        AnsiConsole.MarkupLine("[green]Document created successfully.[/]");
    }

    static void ReadDocuments()
    {
        var collection = Database.Collection;
        Console.Clear();
        AnsiConsole.Write(FormatHeader("Read Documents"));

        var filterJson = GetFilter();
        

        var filter = BsonDocument.Parse(filterJson);
        var documents = collection.Find(filter).ToList();

        if (documents.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No documents found.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]Found {documents.Count} document(s):[/]\n");

        foreach (var document in documents)
        {
           
            AnsiConsole.Write(FormatFromDocument(document));

            AnsiConsole.WriteLine(); // Add an empty line between documents
        }

        Console.ReadKey();
    }
    


    static void UpdateDocument()
    {
        var collection = Database.Collection;
        Console.Clear();
        AnsiConsole.Write(FormatHeader("Update Document"));

        var filterJson = GetFilter();

        var filter = BsonDocument.Parse(filterJson);
        var existingDocument = collection.Find(filter).FirstOrDefault();

        if (existingDocument == null)
        {
            AnsiConsole.MarkupLine("[yellow]Document not found.[/]");
            return;
        }
        //they should be shown a menu and allow them to select the key they want to edit
        //this menu should be the keys and values of the document
       
        AnsiConsole.Write(FormatFromDocument(existingDocument));
        var choices = new List<string>();
        foreach (var element in existingDocument.Elements)
        {
            choices.Add(RemoveMarkupTags(element.Name));
        }

        var keyToEdit = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose a key to edit:")
                .PageSize(existingDocument.Count())
                .AddChoices(choices)
        );
        

        if (!existingDocument.Contains(keyToEdit))
        {
            AnsiConsole.MarkupLine($"[red]Key '{keyToEdit}' does not exist in the document.[/]");
            return;
        }

        var newValue = AnsiConsole.Prompt(
            new TextPrompt<string>($"Enter the new value for '{keyToEdit}':")
        );

        existingDocument[keyToEdit] = BsonValue.Create(newValue);

        var result = collection.ReplaceOne(filter, existingDocument);

        AnsiConsole.MarkupLine($"[yellow]Documents matched: {result.MatchedCount}, Documents modified: {result.ModifiedCount}[/]");
    }


    static void DeleteDocument()
    {
        var collection = Database.Collection;
        Console.Clear();
        AnsiConsole.Write(FormatHeader("Delete Document"));

        var filterJson = GetFilter();

        var filter = BsonDocument.Parse(filterJson);
        var result = collection.DeleteOne(filter);

        AnsiConsole.MarkupLine($"[green]Documents deleted: {result.DeletedCount}[/]");
    }
    static FigletText FormatHeader(string text)
    {
        var header = new FigletText(text)
        {
            Color = Color.Blue,
            Justification = Justify.Center
       
        };

        return header;
    }
    
    static Table FormatFromDocument(BsonDocument document)
    {
        var table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = Style.Parse("blue"),
            Expand = false
        };

        table.AddColumn(new TableColumn("[green]Key[/]"));
        table.AddColumn(new TableColumn("[green]Value[/]"));

        foreach (var element in document.Elements)
        {
            var field = RemoveMarkupTags(element.Name);
            var value = RemoveMarkupTags(element.Value.ToString());
            if (string.IsNullOrWhiteSpace(value))
            {
                value = "N/A"; // Replace empty or null values with "N/A"
            }

            table.AddRow(field, value);
        }

        return table;
    }
    static Table FormatMenu(Dictionary<string, string> options)
    {
        var table = new Table
        {
            Border = TableBorder.Rounded,
            BorderStyle = Style.Parse("blue"),
            Expand = false
        };

        table.AddColumn(new TableColumn("[green]Option[/]"));
        table.AddColumn(new TableColumn("[green]Action[/]"));

        foreach (var option in options)
        {
            table.AddRow(option.Key, option.Value);
        }

        return table;
    }
    
    static string RemoveMarkupTags(string input)
    {
      
        string cleanedString = input.Replace("[", "").Replace("]", "");
        return cleanedString;
    }
    static string GetFilter()
    {
        var filterJson = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter a filter for the documents (in JSON format):")
        );

        if (!ValidJson(filterJson))
        {
            AnsiConsole.MarkupLine("[red]Invalid JSON.[/]");
            filterJson = GetFilter();
        }

        return filterJson;
    }
    static bool ValidJson(string json)
    {
        try
        {
            BsonDocument.Parse(json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
   
}