#!/usr/bin/env dotnet-script
// Fix common markdown linting issues
// Usage: dotnet script scripts/fix-markdown.csx [file-path] [--dry-run]

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

var args = Args.ToArray();
string? filePath = null;
bool dryRun = false;

// Parse command line arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--dry-run")
    {
        dryRun = true;
    }
    else if (!args[i].StartsWith("--"))
    {
        filePath = args[i];
    }
}

void FixMarkdownFile(string path)
{
    Console.WriteLine($"Fixing markdown file: {path}");

    var lines = File.ReadAllLines(path).ToList();
    var newLines = new List<string>();

    for (int i = 0; i < lines.Count; i++)
    {
        var line = lines[i];
        var nextLine = i + 1 < lines.Count ? lines[i + 1] : null;
        var prevLine = i - 1 >= 0 ? lines[i - 1] : null;

        // Fix trailing spaces (MD009)
        line = Regex.Replace(line, @"\s+$", "");

        // Add blank line before headings if missing (MD022)
        if (Regex.IsMatch(line, @"^#+\s+") && prevLine != null && !string.IsNullOrWhiteSpace(prevLine))
        {
            newLines.Add("");
        }

        newLines.Add(line);

        // Add blank line after headings if missing (MD022)
        if (Regex.IsMatch(line, @"^#+\s+") && nextLine != null && 
            !string.IsNullOrWhiteSpace(nextLine) && !Regex.IsMatch(nextLine, @"^#+\s+"))
        {
            newLines.Add("");
        }

        // Add blank line before lists if missing (MD032)
        if (Regex.IsMatch(line, @"^\s*[-\*\+]\s+") && prevLine != null && 
            !string.IsNullOrWhiteSpace(prevLine) && !Regex.IsMatch(prevLine, @"^#+\s+") && 
            !Regex.IsMatch(prevLine, @"^\s*[-\*\+]\s+"))
        {
            // Insert blank line before current line
            newLines.Insert(newLines.Count - 1, "");
        }

        // Add blank line before fenced code blocks (MD031)
        if (Regex.IsMatch(line, @"^```") && prevLine != null && 
            !string.IsNullOrWhiteSpace(prevLine) && !Regex.IsMatch(prevLine, @"^\s*[-\*\+]\s+"))
        {
            newLines.Insert(newLines.Count - 1, "");
        }

        // Add blank line after fenced code blocks (MD031)
        if (Regex.IsMatch(line, @"^```") && nextLine != null && 
            !string.IsNullOrWhiteSpace(nextLine) && !Regex.IsMatch(nextLine, @"^#+\s+"))
        {
            newLines.Add("");
        }
    }

    // Add final newline if missing
    if (newLines.Count > 0 && !string.IsNullOrEmpty(newLines.Last()))
    {
        newLines.Add("");
    }

    if (!dryRun)
    {
        File.WriteAllLines(path, newLines);
        Console.WriteLine($"✅ Fixed: {path}");
    }
    else
    {
        Console.WriteLine($"🔍 Would fix: {path}");
        Console.WriteLine($"   Original lines: {lines.Count}");
        Console.WriteLine($"   New lines: {newLines.Count}");
    }
}

// Get markdown files
var markdownFiles = new List<string>();

if (!string.IsNullOrEmpty(filePath))
{
    if (File.Exists(filePath))
    {
        markdownFiles.Add(Path.GetFullPath(filePath));
    }
    else
    {
        Console.WriteLine($"❌ File not found: {filePath}");
        Environment.Exit(1);
    }
}
else
{
    // Find all markdown files recursively
    var currentDir = Directory.GetCurrentDirectory();
    var mdFiles = Directory.GetFiles(currentDir, "*.md", SearchOption.AllDirectories)
        .Where(f => !f.Contains("node_modules") && !f.Contains(".git"));
    markdownFiles.AddRange(mdFiles);
}

Console.WriteLine($"🔧 Processing {markdownFiles.Count} markdown file(s)...");

foreach (var file in markdownFiles)
{
    try
    {
        FixMarkdownFile(file);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error processing {file}: {ex.Message}");
    }
}

Console.WriteLine($"\n🎉 Markdown fixing complete!");
