
# Idea (not for implemented yet): Standalone script editor/demonstrator

A new WinForms project that demonstrates the scripting system. 
Uses CDS.ScriptChat.WinForms.
Presents a script editor, output window, AI chat window and menu.
Uses Krypton for docking.
Remembers and restores docking layout.
Menu provides standard file operations for .csv files including a most-recently-used feature.
Optional command-line args supported to allow a script filename to be specified for editing and automatically running.
Rich output window: idea is the script might produce text, diagrams, charts, etc. We could use markdown and a rendering control, what other options are there? Could we essentially sandbox the output, or use some local folder system so each run of the script produced a new folder with markdown and linkable PNG images etc?

Use cases:
1. Learning C#
2. PowerShell type scripts for admin, disk querying etc.
3. Ad hoc data analysis. E.g. use the AI chat to quickly make a script that reads a file and processes/analyses it.


# Control over .Net version and language version

1. Can we control which .Net version is used, such as .Net Framework 4.8, .Net 8.0, .Net 11 Preview, etc?
2. Can we specify which version of C# to use, such as 'latest' or a specific version?


# Allowing 'using var ...' at the top level?

At the moment we cannot do this. We have to use curly braces, or place the code inside a scoped block such as a method.
I think LinqPad allows this feature - how do they do it?

