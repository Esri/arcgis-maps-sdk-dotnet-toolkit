param($installPath, $toolsPath, $package, $project) 

# Save files
$dte.ExecuteCommand("File.SaveAll", "");
$project.Save();

# Create timer for delayed project file editing
$timer = New-Object Timers.Timer
$timer.Interval = 1000;

# Tick handler for making the project dirty and saving it.  Must be put in a timer to allow saving after NuGet has made changes
$onTick = { 
    $event.MessageData[1].Stop()

    # Get the current MSBuild project
    $msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.LoadProject($event.MessageData[0]) | Select-Object -First 1

    # NuGet makes changes to the project that are unsaved.  Save them before continuing to prevent user seeing a "conflicting changes" prompt
    $msbuild.Save()

    # Add and remove a dummy item to force the project to reload.  Works around issue where including .targets files in the package don't seem 
    # to trigger this.  Note also that the APIs for marking the project dirty also don't seem to have this effect.
    $item = $msbuild.AddItemFast("Reference", "DummyItem")
    $msbuild.RemoveItems($item)

    # Save and unload the project
    $msbuild.Save()
    [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.UnloadProject($msbuild);

    # Unregister event handler       
    $handler = Get-EventSubscriber -SourceIdentifier $event.MessageData[2] -ErrorAction SilentlyContinue
    if ($handler)
    {
        Unregister-Event $event.MessageData[2]
    }
}

# Create GUID to be used as name of tick handler.  Necessary because uninstalling package from multiple projects (i.e. at the solution) level
# will cause a naming conflict otherwise
$handlerGuid = [System.Guid]::NewGuid().ToString()
$parameters = @($project.FullName, $timer, $handlerGuid)

# Register tick event handler
Register-ObjectEvent -InputObject $timer -EventName elapsed -SourceIdentifier $handlerGuid -Action $onTick -MessageData $parameters

# Start timer
$timer.Start()