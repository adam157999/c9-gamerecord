# Game controller and job name mappings
$games = @{
    "Live22" = "live22Job"
    "habanero" = "habaneroJob"
    "ibc" = "ibcJob"
    "ka" = "kaJob"
    "lionking" = "lionkingJob"
    "ppslotjob" = "ppslotJob"
    "pragmaticlive" = "pragmaticliveJob"
    "relaxjob" = "relaxJob"
    "sexy" = "sexyJob"
    "evolution" = "evoJob"
    "bg" = "bgJob"
    "ag" = "agJob"
    "king855" = "king855Job"
    "spadegaming" = "spadegamingJob"
}

# Base URL pattern
$baseUrl = "https://c9live-gamerecord-job.azurewebsites.net"

foreach ($controller in $games.Keys) {
    $jobName = $games[$controller]
    $taskName = "c9live-$controller"
    $url = "$baseUrl/$controller/$jobName"
    $script = "(New-Object Net.WebClient).DownloadString('$url')"

    Write-Host "Creating task: $taskName" -ForegroundColor Cyan

    # Define action
    $action = New-ScheduledTaskAction -Execute "powershell.exe" `
        -Argument "-ExecutionPolicy Unrestricted -Command `"$script`""

    # Define trigger: repeat every 1 minute (simpler approach)
    $trigger = New-ScheduledTaskTrigger -Once -At 3:24PM -RepetitionInterval (New-TimeSpan -Minutes 1) -RepetitionDuration (New-TimeSpan -Days 1)

    # Define settings (simplified - remove problematic parameters)
    $settings = New-ScheduledTaskSettingsSet `
        -StartWhenAvailable:$true `
        -ExecutionTimeLimit (New-TimeSpan -Days 3) `
        -MultipleInstances "IgnoreNew" `
        -Hidden:$false `
        -RunOnlyIfNetworkAvailable:$false

    # Define principal to run as SYSTEM
    $principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest

    # Register the task
    Register-ScheduledTask -TaskName $taskName `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -Principal $principal `
        -Force
}