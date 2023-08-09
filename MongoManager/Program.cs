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
            //setup the database connections
            Database.Setup(); 
            //start the main loop
            while (true)
            {
                Console.Clear();
                var menu = new Dictionary<string, string>()
                {
                    {"1", "Create document"},
                    {"2", "Read document"},
                    {"3", "Update document"},
                    {"4", "Delete document"},
                    {"5", "Settings"},
                    {"6", "Exit"}
                };
                AnsiConsole.Render(FormatHeader("Mongo Manager"));
                AnsiConsole.Render(FormatMenu(menu));
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
                        UpdateDocument();
                        break;
                    case 4:
                        DeleteDocument();
                        break;
                    case 5:
                        SettingsMenu();
                        break;
                    case 6:
                        Environment.Exit(0);
                        break;
                    
                }
            }


    }

    private static void SettingsMenu()
    {
        throw new NotImplementedException();
    }

    static void CreateDocument()
    {
        var collection = Database.collection;
        Console.Clear();
        AnsiConsole.Render(FormatHeader("Create Document"));

        var json = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the document data (in JSON format):")
        );

        var document = BsonDocument.Parse(json);
        collection.InsertOne(document);

        AnsiConsole.MarkupLine("[green]Document created successfully.[/]");
    }

    static void ReadDocuments()
    {
        var collection = Database.collection;
        Console.Clear();
        AnsiConsole.Render(FormatHeader("Read Documents"));

        var filterJson = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter a filter for the documents (in JSON format):")
        );

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
           
            AnsiConsole.Render(FormatFromDocument(document));

            AnsiConsole.WriteLine(); // Add an empty line between documents
        }

        Console.ReadKey();
    }


    static string EscapeValue(string value)
    {
        return value.Replace("<", "&lt;").Replace(">", "&gt;");
    }


    static void UpdateDocument()
    {
        var collection = Database.collection;
        Console.Clear();
        AnsiConsole.Render(FormatHeader("Update Document"));

        var filterJson = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter a filter for the document to update (in JSON format):")
        );

        var filter = BsonDocument.Parse(filterJson);
        var existingDocument = collection.Find(filter).FirstOrDefault();

        if (existingDocument == null)
        {
            AnsiConsole.MarkupLine("[yellow]Document not found.[/]");
            return;
        }
        //they should be shown a menu and allow them to select the key they want to edit
        //this menu should be the keys and values of the document
       6
        AnsiConsole.Render(FormatFromDocument(existingDocument));
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
        var collection = Database.collection;
        Console.Clear();
        AnsiConsole.Render(FormatHeader("Delete Document"));

        var filterJson = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter a filter for the document to delete (in JSON format):")
        );

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
   
}