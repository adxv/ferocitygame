using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class UMLGenerator
{
    static void Main(string[] args)
    {
        string path = @"C:\dev\ferocitygame\Assets\Scripts";
        string[] csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        string puml = "@startuml\n";

        foreach (string file in csFiles)
        {
            string content = File.ReadAllText(file);
            var classMatches = Regex.Matches(content, @"class\s+(\w+)");
            foreach (Match match in classMatches)
            {
                string className = match.Groups[1].Value;
                puml += $"class {className} {{\n";
                var fieldMatches = Regex.Matches(content, @"(private|public|protected)\s+(\w+)\s+(\w+);");
                foreach (Match field in fieldMatches)
                {
                    string access = field.Groups[1].Value == "public" ? "+" : "-";
                    puml += $"  {access}{field.Groups[3].Value} : {field.Groups[2].Value}\n";
                }
                puml += "}\n";
            }
        }
        puml += "@enduml";
        File.WriteAllText("GameUML.puml", puml);
        Console.WriteLine("UML generated at GameUML.puml");
    }
}