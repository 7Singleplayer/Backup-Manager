# Description

This is a minimalist CLI tool that can be used to create backups.
How to use it

When the program starts, you will be prompted to enter a path to a configuration file.
You can leave this field empty—the program will automatically generate a new config file.

If anything goes wrong, all relevant information will be written to a log file.
The log file is created either in the directory where the program is running or in your home directory.

After startup, the main menu appears in the terminal and looks like this:

    > Autostart = False
    backups management
    start backups
    Settings
    Exit

The ">" symbol indicates the currently selected option.
You can move the selection using the arrow keys on your keyboard.

Mouse input is currently not supported.
Press Enter to confirm your selection.

## Autostart
If you set Autostart to true, the program will skip the main menu on the next launch and immediately start executing the configured backups.

This only works if at least one backup has already been configured.

## Backup Management
To set up a backup, navigate to Backups Management.
You will see the following options:

    > Add Backup
    Remove Backup
    Edit Backups
    List Backups
    Return to Main Menu

## Backup Configuration

The program copies files from a source path to a destination path.

When the backup is executed is defined by the backup reasons.
You can enable or disable each reason by setting it to 1 (true) or 0 (false).

To edit backup reasons, press Enter to open the detailed configuration view.

### Important:
Numbers must be separated by commas (,).
Example:

    source path: your_source_path_here
    destination path: your_destination_path_here
    keep structure: False
    > backup reasons: 0|0|0
    backup interval hours: 0
    backup interval days: 0
    do zip: False
    do unzip: False
    Confirm and Add Backup
    Cancel
    Enter backup reasons (comma separated):
    Options: 1 = last access date, 2 = backup interval hours, 3 = backup interval days
    1,1,0

Confirm by pressing Enter.

Most other settings are self-explanatory.

## Editing Backups

Editing backups works the same way as adding a new one.
First, select the backup you want to edit by entering its number from the list.

Note: Indexing starts at 0.

##Starting Backups

To start the backup process, select Start Backups from the main menu.

## Settings

Additional global options can be configured in the Settings menu.

### Compression Level

To change the compression level, refer to the official .NET documentation:
https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.compressionlevel?view=net-10.0
This page explains all available compression levels in detail.
