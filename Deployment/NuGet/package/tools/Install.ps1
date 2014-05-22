param($installPath, $toolsPath, $package, $project) 

# Save any outstanding changes
$dte.ExecuteCommand("File.SaveAll", "");
$project.Save();

# Get the current MSBuild project. 
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.LoadProject($project.FullName) | Select-Object -First 1

# Add and remove a dummy item to force the project to reload.  Works around issue where including .targets files in the package don't seem 
# to trigger this.  Note also that the APIs for marking the project dirty also don't seem to have this effect.
$item = $msbuild.AddItemFast("Reference", "DummyItem")
$msbuild.RemoveItems($item)

# Save and unload the project
$msbuild.Save()
[Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.UnloadProject($msbuild);
