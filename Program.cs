using System;
using System.IO;
using System.IO.Compression;

internal class Program
{
    private static async Task Main()
    {
        string configpath = string.Empty;
        bool firstStart = false;
        bool autostart = false;
        int compressionLevel = 0;
        bool printstartup = true;
        int checkinginterval = 5; // in minutes
        bool clearlogfile = true;

        string sampletext = "# <-- This is a comment\n" +
        "first-start = true\n" +
        "autostart = false\n" +
        "print-startup = false\n" +
        "checking-interval = 15\n" +
        "compression-level = 0\n" +
        "clear-logfile = true\n" +
        "#backup = sourcepath, destinationpath,keepstructure,lastaccessdate, lastbackupdate, backupreasons, isfile, backupintervalH, backupintervalD, dozip, dounzip, ID, dodeletion";

        List<Backup> clone = new List<Backup>();
        Console.CursorVisible = false;


        //string os = Environment.OSVersion.Platform.ToString();
        var os = Environment.OSVersion.Platform;
        consolelog($"Operating System: {os}", true);
        if (os == PlatformID.Unix)
        {
            Console.WriteLine("🐧😎");
        }
        // finding the location where the application is running
        string runpath = AppContext.BaseDirectory;
        consolelog($"Application Run Path: {runpath}", true);

        string slash = Path.DirectorySeparatorChar.ToString();
        consolelog($"Using Slash: {slash}", true);

        if (File.Exists("path.conf"))
        {
            configpath = File.ReadLines("path.conf").First();
        }


        while (!File.Exists(configpath))
        {
            consolelog("Configuration file not found.", false, ConsoleColor.Yellow);
            consolelog("Enter the correct configuration file path or leave blank to generate a new one:");
            configpath = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(configpath))
            {
                try
                {
                    configpath = Path.Combine(runpath, "FileManager.conf");
                    consolelog($"Generating new configuration file at: {configpath}", true);
                    File.WriteAllText(configpath, sampletext);
                    File.WriteAllText("path.conf", configpath);
                    break;
                }
                catch (Exception ex)
                {
                    consolelog($"Error creating configuration file: {ex.Message}", true, ConsoleColor.Red);
                    configpath = string.Empty;
                }
            }


        }
        if (File.Exists(configpath))
        {
            consolelog($"Configuration file found at: {configpath}", true);

            consolelog("  1.-> Reading configuration file... ", true);
            string[] configlines = File.ReadAllLines(configpath);
            consolelog("Done.", true);
            consolelog("  2.-> Applying configurations... ", true);
            foreach (string line in configlines)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip comments and empty lines
                }

                string[] parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    consolelog($"       - Setting '{key}' to '{value}'", true, ConsoleColor.White);

                    switch (key)
                    {
                        case "first-start":
                            firstStart = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "clear-logfile":
                            clearlogfile = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "compression-level":
                            if (int.TryParse(value, out int level))
                            {
                                compressionLevel = level;
                            }
                            else
                            {
                                consolelog($"     - Invalid compression level: '{value}', using default {compressionLevel}", true, ConsoleColor.Yellow);
                            }
                            break;
                        case "autostart":
                            autostart = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "checking-interval":
                            if (int.TryParse(value, out int interval))
                            {
                                checkinginterval = interval;
                            }
                            else
                            {
                                consolelog($"     - Invalid checking interval: '{value}', using default: {checkinginterval} minutes", true, ConsoleColor.Yellow);
                            }
                            break;
                        case "print-startup":
                            printstartup = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "backup":

                            string[] paths = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (paths.Length < 12)
                            {
                                consolelog($"     - Invalid backup configuration: '{value}', skipping...", true, ConsoleColor.Yellow);
                                break;
                            }
                            clone.Add(new Backup());
                            clone[^1].sourcepath = paths[0];
                            clone[^1].destpath = paths[1];
                            clone[^1].keepstructure = paths[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                            clone[^1].lastaccessdate = DateTime.TryParse(paths[3], out DateTime lad) ? lad : DateTime.MinValue;
                            clone[^1].lastbackupdate = DateTime.TryParse(paths[4], out DateTime lbu) ? lbu : DateTime.MinValue;
                            clone[^1].backupreasons = Array.ConvertAll(paths[5].Split('|', StringSplitOptions.RemoveEmptyEntries), int.Parse);
                            clone[^1].backupintervalH = int.TryParse(paths[6], out int bih) ? bih : 0;
                            clone[^1].backupintervalD = int.TryParse(paths[7], out int bid) ? bid : 0;
                            clone[^1].dozip = paths[8].Equals("true", StringComparison.OrdinalIgnoreCase);
                            clone[^1].dounzip = paths[9].Equals("true", StringComparison.OrdinalIgnoreCase);
                            clone[^1].ID = int.TryParse(paths[10], out int id) ? id : 0;
                            clone[^1].dodeletion = paths[11].Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;


                        default:
                            consolelog($"     - Unknown configuration key: '{key}' skipping...", true, ConsoleColor.Yellow);
                            break;

                    }
                }

                consolelog("Done.", true, ConsoleColor.Green);






            }

        }
        if (clearlogfile)
        {
            try
            {
                File.Delete("FileManager_log.log");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while deleting the log file: {ex.Message}");
                Console.ResetColor();
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: An error occurred while deleting the log file: {ex.Message}{Environment.NewLine}");
            }
        }
        if (firstStart)
        {
            welcomeMessage();
            firstStart = false;
            changesetting("first-start", "false", configpath);
        }
        if (printstartup)
        {
            consolelog("Startup complete. Application is running.", true);
            Thread.Sleep(3000);
            if (!autostart)
            {
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }
        bool b = false;
        int a = 0;
        if (autostart)
        {
            consolelog("Autostart is enabled. Starting backups...", true);
            b = true;
        }
        while (true)
        {
            a = 2;
            if (!b)
            {
                a = menu(new string[] { $"Autostart = {autostart.ToString()}", "backups management", "start backups", "Settings", "Exit" });
            }

            switch (a)
            {

                case 0:
                    logmessage("Changing autostart to " + (!autostart).ToString());
                    autostart = !autostart;
                    changesetting("autostart", autostart.ToString().ToLower(), configpath);
                    break;
                case 1:
                    switch (menu(new string[] { "Add Backup", "Remove Backup", "Edit Backups", "List Backups", "Return to Main Menu" }))
                    {
                        case 0:
                            consolelog("Adding a new backup... ", true);
                            string sourcepath = "your_source_path_here";
                            string destinationpath = "your_destination_path_here";
                            bool keepstructure = false;
                            DateTime lastaccessdate = DateTime.MinValue;
                            DateTime lastbackupdate = DateTime.MinValue;
                            List<string> backupreasons = new List<string> { "0", "0", "0" };
                            int backupintervalH = 0;
                            int backupintervalD = 0;
                            bool dozip = false;
                            bool dounzip = false;
                            int id = clone.Count + 1;
                            bool dodeletion = false;

                            //backup = sourcepath, destinationpath,keepstructure,lastaccessdate, lastbackupdate, backupreasons, backupintervalH, backupintervalD, dozip, dounzip
                            bool loop = true;
                            while (loop)
                            {

                                switch (menu(new string[] { $"source path: {sourcepath}", $"destination path: {destinationpath}", $"keep structure: {keepstructure}", $"backup reasons: {string.Join("|", backupreasons)}", $"backup interval hours: {backupintervalH}", $"backup interval days: {backupintervalD}", $"do zip: {dozip}", $"do unzip: {dounzip}", $"do deletion: {dodeletion}", "Confirm and Add Backup", "Cancel" }))
                                {
                                    case 0:
                                        // get source path
                                        consolelog("Enter source path: ", false);
                                        sourcepath = Console.ReadLine() ?? string.Empty;
                                        break;
                                    case 1:
                                        // get destination path
                                        consolelog("Enter destination path: ", false);
                                        destinationpath = Console.ReadLine() ?? string.Empty;
                                        break;
                                    case 2:
                                        // get keep structure
                                        consolelog("do you want to keep the folder structure? (y/n): ", false);
                                        keepstructure = Console.ReadLine()?.ToLower() == "y";
                                        break;

                                    case 3:
                                        // get backup reasons
                                        consolelog("Enter backup reasons (comma separated): ", false);
                                        consolelog("Options: 1 = last access date, 2 = backup interval hours, 3 = backup interval days", false);
                                        backupreasons = Console.ReadLine()?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
                                        break;
                                    case 4:
                                        // get backup interval hours
                                        consolelog("Enter backup interval hours: ", false);
                                        if (int.TryParse(Console.ReadLine(), out int hours))
                                            backupintervalH = hours;
                                        break;
                                    case 5:
                                        // get backup interval days
                                        consolelog("Enter backup interval days: ", false);
                                        if (int.TryParse(Console.ReadLine(), out int days))
                                            backupintervalD = days;
                                        break;
                                    case 6:
                                        // get do zip
                                        consolelog("Backup as zip file? (y/n): ", false);
                                        dozip = Console.ReadLine()?.ToLower() == "y";
                                        break;
                                    case 7:
                                        // get do unzip
                                        consolelog("Extract zip file? (y/n): ", false);
                                        dounzip = Console.ReadLine()?.ToLower() == "y";
                                        break;
                                    case 8:
                                        dodeletion = !dodeletion;
                                        break;
                                    case 9:
                                        // confirm and add backup
                                        clone.Add(new Backup
                                        {
                                            sourcepath = sourcepath,
                                            destpath = destinationpath,
                                            keepstructure = keepstructure,
                                            lastaccessdate = lastaccessdate,
                                            lastbackupdate = lastbackupdate,
                                            backupreasons = backupreasons.Select(s => int.TryParse(s, out int r) ? r : 0).ToArray(),
                                            backupintervalH = backupintervalH,
                                            backupintervalD = backupintervalD,
                                            dozip = dozip,
                                            dounzip = dounzip,
                                            ID = clone.Count + 1,
                                            dodeletion = dodeletion
                                        });
                                        File.AppendAllLines(configpath, new[] { $"backup={sourcepath},{destinationpath},{keepstructure},{lastaccessdate},{lastbackupdate},{string.Join("|", backupreasons)},{backupintervalH},{backupintervalD},{dozip},{dounzip},{id}" });
                                        consolelog($"Added new backup: {sourcepath} to {destinationpath}", true);
                                        loop = false;
                                        break;
                                    case 10:
                                        // cancel adding a new backup
                                        consolelog("Cancelled.", true);
                                        loop = false;
                                        break;
                                }
                            }
                            break;
                        case 1:
                            if (clone.Count == 0)
                            {
                                break;
                            }
                            consolelog("Removing a backup...", true);
                            Console.WriteLine("wich backup do you want to remove?");
                            var removeOptions = clone.Select((b, i) => $"{i + 1}. {b.sourcepath} -> {b.destpath}").ToList();
                            removeOptions.Add("Cancel");
                            int removeChoice = menu(removeOptions.ToArray());
                            switch (removeChoice)
                            {
                                case 0:
                                    if (clone.Count > 0)
                                    {
                                        consolelog($"Removing backup: {clone[0].sourcepath} to {clone[0].destpath}", true);
                                        clone.RemoveAt(0);
                                        string[] lines = File.ReadAllLines(configpath);
                                        for (int i = 0; i < lines.Length; i++)
                                        {
                                            if (lines[i].TrimStart().StartsWith("backup") && lines[i].Contains(clone[0].sourcepath) && lines[i].Contains(clone[0].destpath))
                                            {
                                                lines[i] = string.Empty;
                                                break;
                                            }
                                        }
                                        File.WriteAllLines(configpath, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
                                        consolelog("Backup removed.", true);
                                    }
                                    break;
                                case int n when n >= 1 && n < clone.Count:
                                    consolelog($"Removing backup: {clone[n].sourcepath} to {clone[n].destpath}", true);

                                    string[] lines2 = File.ReadAllLines(configpath);

                                    for (int i = 0; i < lines2.Length; i++)
                                    {
                                        if (lines2[i].TrimStart().StartsWith("backup") && lines2[i].Contains(clone[n].sourcepath) && lines2[i].Contains(clone[n].destpath))
                                        {
                                            lines2[i] = string.Empty;
                                            break;
                                        }
                                    }
                                    clone.RemoveAt(n);
                                    File.WriteAllLines(configpath, lines2.Where(line => !string.IsNullOrWhiteSpace(line)));
                                    consolelog("Backup removed.", true);
                                    break;
                                default:
                                    consolelog("Cancelled", true);
                                    break;
                            }
                            break;
                        case 2:
                            if (clone.Count == 0)
                            {
                                break;
                            }
                            consolelog("Editing backups...", true);
                            for (int i = 0; i < clone.Count; i++)
                            {
                                var f = clone[i];
                                consolelog($"{i}. Source: {f.sourcepath}, Destination: {f.destpath}, Backup Reasons: {string.Join("|", f.backupreasons)}, Do Zip: {f.dozip}, Do Unzip: {f.dounzip},ID: {f.ID}", true);
                            }
                            consolelog("Which backup do you want to edit?: ", false);
                            bool validInput = false;
                            int editChoice = 0;
                            Backup temp = new Backup();
                            while (!validInput)
                            {
                                string? input = Console.ReadLine();
                                if (input == String.Empty)
                                {
                                    editChoice = -1;
                                    break;
                                }
                                if (int.TryParse(input, out int choice) && choice >= 0 && choice < clone.Count && clone.Count > 0)
                                {
                                    editChoice = choice;
                                    validInput = true;
                                    temp = clone[editChoice];
                                }
                                else
                                {
                                    consolelog("Invalid input. Please enter a valid number.", true, ConsoleColor.Red);
                                }
                            }
                            if (editChoice == -1)
                            {
                                break;
                            }

                            consolelog("Editing backup...", true);
                            string sourcepath2 = clone[editChoice].sourcepath;
                            string destinationpath2 = clone[editChoice].destpath;
                            bool keepstructure2 = clone[editChoice].keepstructure;
                            DateTime lastaccessdate2 = clone[editChoice].lastaccessdate;
                            DateTime lastbackupdate2 = clone[editChoice].lastbackupdate;
                            List<string> backupreasons2 = clone[editChoice].backupreasons.Select(r => r.ToString()).ToList();
                            int backupintervalH2 = clone[editChoice].backupintervalH;
                            int backupintervalD2 = clone[editChoice].backupintervalD;
                            bool dozip2 = clone[editChoice].dozip;
                            bool dounzip2 = clone[editChoice].dounzip;
                            int ID2 = clone[editChoice].ID;
                            bool dodeletion2 = clone[editChoice].dodeletion;
                            //backup = sourcepath, destinationpath,keepstructure,lastaccessdate, lastbackupdate, backupreasons, backupintervalH, backupintervalD, dozip, dounzip, ID
                            bool loop2 = true;
                            while (loop2)
                            {
                                switch (menu(new string[] { $"source path: {sourcepath2}", $"destination path: {destinationpath2}", $"keep structure: {keepstructure2}", $"backup reasons: {string.Join("|", backupreasons2)}", $"backup interval hours: {backupintervalH2}", $"backup interval days: {backupintervalD2}", $"do zip: {dozip2}", $"do unzip: {dounzip2}", $"ID: {ID2}", $"Delete deleted files: {dodeletion2}", "Confirm and Save Backup settings", "cancel" }))
                                {
                                    case 0:
                                        // get source path
                                        consolelog("Enter source path: ", false);
                                        sourcepath2 = Console.ReadLine() ?? string.Empty;
                                        break;
                                    case 1:
                                        // get destination path
                                        consolelog("Enter destination path: ", false);
                                        destinationpath2 = Console.ReadLine() ?? string.Empty;
                                        break;
                                    case 2:
                                        // get keep structure
                                        consolelog("do you want to keep the folder structure? (y/n): ", false);
                                        keepstructure2 = Console.ReadLine()?.ToLower() == "y";
                                        break;

                                    case 3:
                                        // get backup reasons
                                        consolelog("Enter backup reasons (comma separated): ", false);
                                        consolelog("Options: 1 = last access date, 2 = backup interval hours, 3 = backup interval days", false);
                                        backupreasons2 = Console.ReadLine()?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

                                        while (backupreasons2.Count < 3)
                                        {
                                            backupreasons2.Add("0");
                                        }

                                        break;
                                    case 4:
                                        // get backup interval hours
                                        consolelog("Enter backup interval hours: ", false);
                                        if (int.TryParse(Console.ReadLine(), out int hours))
                                            backupintervalH2 = hours;
                                        break;
                                    case 5:
                                        // get backup interval days
                                        consolelog("Enter backup interval days: ", false);
                                        if (int.TryParse(Console.ReadLine(), out int days))
                                            backupintervalD2 = days;
                                        break;
                                    case 6:
                                        // get do zip
                                        consolelog("Backup as zip file? (y/n): ", false);
                                        dozip2 = Console.ReadLine()?.ToLower() == "y";
                                        break;
                                    case 7:
                                        // get do unzip
                                        consolelog("Extract zip file? (y/n): ", false);
                                        dounzip2 = Console.ReadLine()?.ToLower() == "y";
                                        break;
                                    case 8:
                                        // get ID
                                        consolelog("Enter backup ID (number): ", false);
                                        if (int.TryParse(Console.ReadLine(), out int newid))
                                        { ID2 = newid; }
                                        else
                                        {
                                            consolelog("Invalid input. ID not changed.", false, ConsoleColor.Yellow);
                                        }
                                        break;
                                    case 9:
                                        dodeletion2 = !dodeletion2;
                                        break;
                                    case 10:
                                        // confirm and save backup (replace the selected backup entry)
                                        clone.RemoveAt(editChoice);
                                        clone.Add(new Backup
                                        {
                                            sourcepath = sourcepath2,
                                            destpath = destinationpath2,
                                            keepstructure = keepstructure2,
                                            lastaccessdate = lastaccessdate2,
                                            lastbackupdate = lastbackupdate2,
                                            backupreasons = backupreasons2.Select(s => int.TryParse(s, out int r) ? r : 0).ToArray(),
                                            backupintervalH = backupintervalH2,
                                            backupintervalD = backupintervalD2,
                                            dozip = dozip2,
                                            dounzip = dounzip2,
                                            ID = ID2,
                                            dodeletion = dodeletion2
                                        });
                                        consolelog($"saving backup: {sourcepath2} to {destinationpath2}", true);

                                        if (clone.Count > 0)
                                        {

                                            string[] lines = File.ReadAllLines(configpath);
                                            for (int i = 0; i < lines.Length; i++)
                                            {
                                                if (lines[i].Contains(temp.sourcepath) && lines[i].Contains(temp.destpath) && lines[i].Contains(temp.ID.ToString()))
                                                {
                                                    lines[i] = $"backup={sourcepath2},{destinationpath2},{keepstructure2},{lastaccessdate2},{lastbackupdate2},{string.Join("|", backupreasons2)},{backupintervalH2},{backupintervalD2},{dozip2},{dounzip2},{ID2},{dodeletion2}";
                                                    break;
                                                }
                                            }
                                            File.WriteAllLines(configpath, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
                                            consolelog("Backup saved.", true);
                                        }

                                        loop2 = false;
                                        break;
                                    case 11:
                                        loop2 = false;
                                        break;

                                }
                            }
                            break;

                        case 3:
                            consolelog("Listing all backups...", true);
                            if (clone.Count == 0)
                            {
                                consolelog("No backups configured.", true, ConsoleColor.Yellow);
                            }
                            else
                            {
                                for (int i = 0; i < clone.Count; i++)
                                {
                                    var f = clone[i];
                                    consolelog($"{i + 1}. Source: {f.sourcepath}, Destination: {f.destpath}, Keep Structure: {f.keepstructure}, Backup Reasons: {string.Join("|", f.backupreasons)}, Backup Interval Hours: {f.backupintervalH}, Backup Interval Days: {f.backupintervalD}, Do Zip: {f.dozip}, Do Unzip: {f.dounzip}, ID: {f.ID}", true);
                                }

                            }
                            Console.WriteLine("Press Enter to continue...");
                            Console.ReadLine();
                            break;
                        case 4:
                            consolelog("Returning to main menu...", true);
                            break;
                    }
                    break;
                case 2:
                    b = false;
                    consolelog("starting backups...", true);
                    //File.GetLastWriteTime();
                    if (clone.Count == 0)
                    {
                        consolelog("No backups configured. Returning to main menu.", true, ConsoleColor.Yellow);
                        break;
                    }
                    var cts = new CancellationTokenSource();
                    // CTRL+C sofort abfangen
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        consolelog("backups stopped.", true);
                        cts.Cancel();
                    };
                    // Optional: ESC ebenfalls
                    var _ = Task.Run(() =>
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            if (Console.KeyAvailable &&
                                Console.ReadKey(true).Key == ConsoleKey.Escape)
                            {
                                consolelog("backups stopped.", true);
                                cts.Cancel();
                            }
                        }
                    });
                    List<FileSystemWatcher> watcherPool = new();
                    foreach (var item in clone)
                    {
                        var watcher = new FileSystemWatcher
                        {
                            Path = item.sourcepath,
                            Filter = "*",
                            EnableRaisingEvents = true
                        };

                        watcher.Changed += (s, e) =>
                        {
                            item.FileAccessed();
                            consolelog($"File accessed: {e.FullPath}", true);
                        };

                        watcher.Created += (s, e) =>
                        {
                            item.FileAccessed();
                            consolelog($"File created: {e.FullPath}", true);
                        };

                        watcher.Deleted += (s, e) =>
                        {
                            item.FileAccessed();
                            consolelog($"File deleted: {e.FullPath}", true);
                        };

                        watcher.Renamed += (s, e) =>
                        {
                            item.FileAccessed();
                            consolelog($"File renamed: {e.OldFullPath} to {e.FullPath}", true);
                        };

                        watcher.Error += (s, e) =>
                        {
                            consolelog($"File system watcher error: {e.GetException().Message}", true, ConsoleColor.Red);
                            consolelog("Soft-Reset...", true);
                            watcher.EnableRaisingEvents = false;
                            watcher.EnableRaisingEvents = true;
                        };
                        watcherPool.Add(watcher); // Lifetime sichern
                    }


                    while (true)
                    {

                        foreach (var item in clone)
                        {
                            // safe checks for backupreasons length
                            bool hasReasonAccess = item.backupreasons.Length > 0 && item.backupreasons[0] == 1;
                            bool hasReasonHours = item.backupreasons.Length > 1 && item.backupreasons[1] == 1;
                            bool hasReasonDays = item.backupreasons.Length > 2 && item.backupreasons[2] == 1;

                            // compute elapsed times
                            var sinceLastBackup = DateTime.Now - item.lastbackupdate;
                            bool expiredHours = hasReasonHours && item.backupintervalH > 0 && sinceLastBackup.TotalHours >= item.backupintervalH;
                            bool expiredDays = hasReasonDays && item.backupintervalD > 0 && sinceLastBackup.TotalDays >= item.backupintervalD;
                            bool accessedAfterBackup = hasReasonAccess && item.lastaccessdate > item.lastbackupdate;
                            consolelog($"Checking backup ID {item.ID}: AccessedAfterBackup={accessedAfterBackup}, ExpiredHours={expiredHours}, ExpiredDays={expiredDays}", true);
                            consolelog($"  - Last Access Date: {item.lastaccessdate}, Last Backup Date: {item.lastbackupdate}", true);


                            if (accessedAfterBackup || expiredHours || expiredDays)
                            {
                                consolelog($"Cloning from '{item.sourcepath}' to '{item.destpath}' (Keep Structure: {item.keepstructure})", true);
                                try
                                {
                                    item.clone(compressionLevel);
                                    item.lastbackupdate = DateTime.Now;
                                }
                                catch (Exception ex)
                                {
                                    consolelog($"Error during cloning: {ex.Message}", true, ConsoleColor.Red);
                                }
                            }
                        }
                        try
                        { await Task.Delay(TimeSpan.FromMinutes(checkinginterval), cts.Token); }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                    break;

                case 3:
                    /*
            string configpath = string.Empty;
            bool firstStart = false;
            bool autostart = false;
            int compressionLevel = 5;
            bool printstartup = true;
            int checkinginterval = 5000; // in milliseconds
            bool clearlogfile = false;
            */
                    bool x = true;
                    while (x)
                    {
                        switch (menu(new string[] { $"first start:{firstStart}", $"autostart:{autostart}", $"compression level:{compressionLevel}", $"print startup messages:{printstartup}", $"checking interval:{checkinginterval}", $"clear logfile on start:{clearlogfile}", "Return to Main Menu" }))
                        {
                            case 0:
                                firstStart = !firstStart;
                                logmessage("Setting firstStart to " + firstStart.ToString());
                                changesetting("first-start", firstStart.ToString().ToLower(), configpath);
                                break;
                            case 1:
                                autostart = !autostart;
                                logmessage("Setting autostart to " + autostart.ToString());
                                changesetting("autostart", autostart.ToString().ToLower(), configpath);
                                break;
                            case 2:
                                consolelog("Enter new compression level (0-3): ", false);
                                string? clevel = Console.ReadLine();
                                if (int.TryParse(clevel, out int newlevel))
                                {
                                    compressionLevel = newlevel;
                                    logmessage("Setting compressionLevel to " + compressionLevel.ToString());
                                    changesetting("compression-level", compressionLevel.ToString(), configpath);
                                }
                                else
                                {
                                    consolelog("Invalid input. Compression level not changed.", false, ConsoleColor.Yellow);
                                }
                                break;
                            case 3:
                                printstartup = !printstartup;
                                logmessage("Setting printstartup to " + printstartup.ToString());
                                changesetting("print-startup", printstartup.ToString().ToLower(), configpath);
                                break;
                            case 4:
                                consolelog("Enter new checking interval in minutes: ", false);
                                string? cinterval = Console.ReadLine();
                                if (int.TryParse(cinterval, out int newinterval))
                                {
                                    checkinginterval = newinterval;
                                    logmessage("Setting checkinginterval to " + checkinginterval.ToString());
                                    changesetting("checking-interval", checkinginterval.ToString(), configpath);
                                }
                                else
                                {
                                    consolelog("Invalid input. Checking interval not changed.", false, ConsoleColor.Yellow);
                                }
                                break;
                            case 5:
                                clearlogfile = !clearlogfile;
                                logmessage("Setting clearlogfile to " + clearlogfile.ToString());
                                changesetting("clear-logfile", clearlogfile.ToString().ToLower(), configpath);
                                break;
                            case 6:
                                consolelog("Returning to main menu...", true);
                                x = false;
                                break;


                        }
                    }
                    break;
                case 4:
                    consolelog("Exiting application.", true);
                    Thread.Sleep(1000);
                    Environment.Exit(0);
                    return;
            }



        }


        static void consolelog(string message, bool writetolog = false, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();

            if (writetolog)
            {

                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: {message}{Environment.NewLine}");

            }
        }
        static void logmessage(string message)
        {
            File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        static void welcomeMessage()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("      Welcome to FileManager        ");
            Console.WriteLine("=====================================");


        }

        static void changesetting(string key, string value, string configpath)
        {
            var lines = File.ReadAllLines(configpath);
            bool exists = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={value}";
                    File.WriteAllLines(configpath, lines);
                    exists = true;
                    return;
                }
            }
            if (!exists)
            {
                File.AppendAllText(configpath, $"{Environment.NewLine}{key}={value}");
            }
        }

        static int menu(string[] options)
        {
            int index = 0;
            ConsoleKeyInfo keyInfo;
            do
            {
                Console.Clear();
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"> {options[i]}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  {options[i]}");
                    }
                }

                keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    index = index == 0 ? options.Length - 1 : index - 1;
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    index = index == options.Length - 1 ? 0 : index + 1;
                }

            } while (keyInfo.Key != ConsoleKey.Enter);
            return index;
        }

    }

}



class Backup
{
    public string sourcepath { get; set; } = string.Empty;
    public string destpath { get; set; } = string.Empty;
    public bool keepstructure { get; set; } = false;
    public DateTime lastbackupdate { get; set; } = DateTime.MinValue;
    public DateTime lastaccessdate { get; set; } = DateTime.MinValue;
    public int[] backupreasons { get; set; } = new int[0];

    public int backupintervalH { get; set; } = 0;
    public int backupintervalD { get; set; } = 0;
    public bool dozip { get; set; } = false;
    public bool dounzip { get; set; } = false;
    public int ID { get; set; } = 0;
    public bool dodeletion { get; set; } = true;


    static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();

        if (!dir.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Source directory does not exist or could not be found: {sourceDirName}");
            Console.ResetColor();
            File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Source directory does not exist or could not be found: {sourceDirName}{Environment.NewLine}");
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
        }

        if (!Directory.Exists(destDirName))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Destination not found. Creating new directory: {destDirName}");
            Console.ResetColor();
            Directory.CreateDirectory(destDirName);
            File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Destination not found. Creating new directory: {destDirName}{Environment.NewLine}");
        }

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, true);
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }


    public void unzip(string sourceDirName, string destDirName, bool copySubDirs)
    {
        if (File.Exists(sourceDirName) && sourceDirName.EndsWith(".zip"))
        {
            if (!Directory.Exists(destDirName))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Destination not found. Creating new directory: {destDirName}");
                Console.ResetColor();
                Directory.CreateDirectory(destDirName);
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Destination not found. Creating new directory: {destDirName}{Environment.NewLine}");
            }
            if (!destDirName.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                destDirName += Path.DirectorySeparatorChar;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Appending directory separator to destination path: {destDirName}");
                Console.ResetColor();
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Appending directory separator to destination path: {destDirName}{Environment.NewLine}");
            }

            try
            {
                ZipFile.ExtractToDirectory(sourceDirName, destDirName);
                Console.WriteLine("Files extracted successfully.");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("The specified ZIP file was not found.");
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: The specified ZIP file was not found.{Environment.NewLine}");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("The specified directory does not exist.");
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: The specified directory does not exist.{Environment.NewLine}");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("The ZIP file is invalid or corrupted.");
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: The ZIP file is invalid or corrupted.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: An unexpected error occurred: {ex.Message}{Environment.NewLine}");
            }

        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Source path '{sourcepath}' does not exist or is not a zip file. Skipping...");
            Console.ResetColor();
            File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Source path '{sourcepath}' does not exist or is not a zip file. Skipping...{Environment.NewLine}");
        }

    }
    public void Zip(int compressionLevel = 0)
    {
        if (Directory.Exists(sourcepath))
        {
            string zipPath = destpath;
            if (!zipPath.EndsWith(".zip"))
            {
                zipPath += ".zip";
            }

            if (File.Exists(zipPath))
            {
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Zip file '{zipPath}' already exists. Deleting existing file.{Environment.NewLine}");
                File.Delete(zipPath);
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Existing zip file '{zipPath}' deleted.{Environment.NewLine}");
            }

            try
            {
                ZipFile.CreateFromDirectory(sourcepath, zipPath, (CompressionLevel)compressionLevel, keepstructure);
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Successfully created zip file '{zipPath}' from directory '{sourcepath}'.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occurred while creating the zip file: {ex.Message}");
                Console.ResetColor();
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Fatal error occurred. couldn't create backup.{Environment.NewLine}");
                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: An error occurred while creating the zip file: {ex.Message}{Environment.NewLine}");

            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Source path '{sourcepath}' does not exist or is not a directory. Skipping...");
            Console.ResetColor();
            File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Source path '{sourcepath}' does not exist or is not a directory. Skipping...{Environment.NewLine}");
        }
    }


    public void clone(int compressionLevel)
    {

        if (dodeletion)
        {
            string[] temp = Directory.GetFiles(destpath);
            foreach (var file in temp)
            {
                if (!Directory.GetFiles(sourcepath).Contains(file))
                {
                    Directory.Delete(Path.Combine(destpath, file));
                }
            }
        }
        if (dozip)
        {
            Zip(compressionLevel);
        }

        else if (File.Exists(sourcepath))
        {
            try
            {
                File.Copy(sourcepath, destpath, true);

            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Access denied copying '{sourcepath}' to '{destpath}': {ex.Message}");
                Console.ResetColor();

                File.AppendAllText("FileManager_log.log", $"{DateTime.Now}: Access denied copying '{sourcepath}' to '{destpath}': {ex.Message}{Environment.NewLine}");


            }
        }
        else
        {
            DirectoryCopy(sourcepath, destpath, keepstructure);
        }

        if (dounzip)
        {
            string source = sourcepath;
            if (!sourcepath.EndsWith(".zip"))
            {

                foreach (string file in Directory.GetFiles(sourcepath, "*.zip"))
                {
                    source = file;
                    unzip(source, destpath, keepstructure);
                }
            }
            else
            {
                unzip(sourcepath, destpath, keepstructure);
            }
        }

    }
    public void FileAccessed()
    {
        lastaccessdate = DateTime.Now;
    }

}

