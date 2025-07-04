﻿using AutoOS.Views.Installer.Actions;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;

namespace AutoOS.Views.Installer.Stages;

public static class RegistryStage
{
    public static async Task Run()
    {
        bool? Desktop = PreparingStage.Desktop;
        bool? SSD = PreparingStage.SSD;
        bool? Rename = PreparingStage.Rename;

        InstallPage.Status.Text = "Configuring Registry...";

        string previousTitle = string.Empty;
        int stagePercentage = 10;

        string edgeVersion = "";

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // general privacy
            ("Disabling website access to language list", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\International\User Profile"" /v ""HttpAcceptLanguageOptOut"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling app launch tracking", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""Start_TrackProgs"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling account notifications in the settings app", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications"" /v EnableAccountNotifications /t REG_DWORD /d 0 /f"), null),
            ("Disabling advertising ids", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling advertising ids", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo"" /v Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling advertising ids", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo"" /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f"), null),
            ("Disabling suggested content in the settings app", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling suggested content in the settings app", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353694Enabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling suggested content in the settings app", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"" /v SubscribedContent-353696Enabled /t REG_DWORD /d 0 /f"), null),

            // disable find my device
            ("Disabling find my device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\FindMyDevice"" /v ""AllowFindMyDevice"" /t REG_DWORD /d 0 /f"), null),

            // disable speech recognition
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy"" /v ""HasAccepted"" /t REG_DWORD /d 0 /f"), null),

            // disable inking and typing personalization
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\InputPersonalization"" /v ""RestrictImplicitInkCollection"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\InputPersonalization"" /v ""RestrictImplicitTextCollection"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\InputPersonalization"" /v RestrictImplicitInkCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization"" /v RestrictImplicitInkCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\InputPersonalization"" /v RestrictImplicitTextCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization"" /v RestrictImplicitTextCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\InputPersonalization"" /v AllowInputPersonalization /t REG_DWORD /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\InputPersonalization"" /v AllowInputPersonalization /t REG_DWORD /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\InputPersonalization\TrainedDataStore"" /v ""HarvestContacts"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Personalization\Settings"" /v ""AcceptedPrivacyPolicy"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\AllowTelemetry"" /t DefaultValue /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\InkingAndTypingPersonalization"" /t DefaultValue /d 0 /f"), null),
            ("Disabling speech recognition", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Input\TIPC"" /t Enabled /d 0 /f"), null),

            // disable diagnostics and feedback
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\DataCollection"" /v ""AllowTelemetry"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v AllowTelemetry /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v MaxTelemetryAllowed /t REG_DWORD /d 1 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\DataCollection"" /v MaxTelemetryAllowed /t REG_DWORD /d 1 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\System\AllowTelemetry"" /v value /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\TextInput"" /v AllowLinguisticDataCollection /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Privacy"" /v ""TailoredExperiencesWithDiagnosticDataEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CPSS\DevicePolicy\TailoredExperiencesWithDiagnosticDataEnabled"" /v DefaultValue /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack\EventTranscriptKey"" /v EnableEventTranscript /t REG_DWORD /d 0 /f"), null),
            ("Disabling diagnostics and feedback", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Siuf\Rules"" /v ""NumberOfSIUFInPeriod"" /t REG_DWORD /d 0 /f"), null),

            // disable activity history
            ("Disabling activity history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v EnableActivityFeed /t REG_DWORD /d 0 /f"), null),
            ("Disabling activity history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v PublishUserActivities /t REG_DWORD /d 0 /f"), null),
            ("Disabling activity history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v UploadUserActivities /t REG_DWORD /d 0 /f"), null),

            // disable app access to location
            ("Disabling app access to location", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to location", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to location", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\activity"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to location", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\activity"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to voice activation
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationOnLockScreenEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling app access to voice activation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Speech_OneCore\Settings\VoiceActivation\UserPreferenceForAllApps"" /v ""AgentActivationLastUsed"" /t REG_DWORD /d 0 /f"), null),

            // disable app access to notifications
            ("Disabling app access to notifications", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to notifications", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userNotificationListener"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to account info
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /v ""LetAppsAccessAccountInfo"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to account info", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userAccountInformation"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to contacts
            ("Disabling app access to contacts", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to contacts", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\contacts"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to calendar
            ("Disabling app access to calendar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to calendar", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appointments"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to phone calls
            ("Disabling app access to phone calls", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to phone calls", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCall"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to call history
            ("Disabling app access to call history", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to call history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\phoneCallHistory"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to email
            ("Disabling app access to email", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to email", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\email"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to tasks
            ("Disabling app access to tasks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to tasks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\userDataTasks"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to messaging
            ("Disabling app access to messaging", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to messaging", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\chat"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to radios
            ("Disabling app access to radios", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to radios", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\radios"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to other devices
            ("Disabling app access to other devices", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to other devices", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\bluetoothSync"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to app diagnostics
            ("Disabling app access to app diagnostics", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to app diagnostics", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\appDiagnostics"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to documents
            ("Disabling app access to documents", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to documents", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\documentsLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to download
            ("Disabling app access to downloads", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to downloads", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\downloadsFolder"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to music
            ("Disabling app access to music", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to music", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\musicLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to pictures
            ("Disabling app access to pictures", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to pictures", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\picturesLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to videos
            ("Disabling app access to videos", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to videos", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\videosLibrary"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable app access to the file system
            ("Disabling app access to the file system", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),
            ("Disabling app access to the file system", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\broadFileSystemAccess"" /v ""Value"" /t REG_SZ /d ""Deny"" /f"), null),

            // disable automatic driver installation
            ("Disabling automatic driver installation", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f"), null),
            ("Disabling automatic driver installation", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UpdatePolicy\PolicyState"" /v ExcludeWUDrivers /t REG_DWORD /d 1 /f"), null),

            // disable sign-in and lock last interactive user after a restart
            ("Disabling the automatic sign-in and locking of the last interactive user after a restart", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v DisableAutomaticRestartSignOn /t REG_DWORD /d 1 /f"), null),

            // disable maintenance
            ("Disabling automatic maintenance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance"" /v MaintenanceDisabled /t REG_DWORD /d 1 /f"), null),
            ("Disabling automatic maintenance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\ScheduledDiagnostics"" /v EnabledExecution /t REG_DWORD /d 0 /f"), null),

            // disable windows error reporting
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v AutoApproveOSDumps /t REG_DWORD /d 0 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v LoggingDisabled /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v AutoApproveOSDumps /t REG_DWORD /d 0 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v DontSendAdditionalData /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v DontShowUI /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting\Consent"" /v 0 /t REG_SZ /d """" /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\PCHealth\ErrorReporting"" /v DoReport /t REG_DWORD /d 0 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting\Consent"" /v DefaultConsent /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting\Consent"" /v DefaultOverrideBehavior /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f"), null),
            ("Disabling Windows Error Reporting", async () => await  ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting"" /v DontSendAdditionalData /t REG_DWORD /d 1 /f"), null),

            // disable fault tolerant heap
            ("Disabling Fault Tolerant Heap (FTH)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\FTH"" /v Enabled /t REG_DWORD /d 0 /f"), null),

            // disable customer experience improvement program
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SQMClient\Windows"" /v CEIPEnable /t REG_DWORD /d 0 /f"), null),
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VSCommon\15.0\SQM"" /v OptIn /t REG_DWORD /d 0 /f"), null),
            ("Disabling Customer Experience Improvement Program (CEIP)", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Messenger\Client"" /v CEIP /t REG_DWORD /d 2 /f"), null),

            // disable messages to cloud services
            ("Disabling messages to cloud services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Messaging"" /v AllowMessageSync /t REG_DWORD /d 0 /f"), null),

            // set desktop mode default over tablet mode
            ("Setting desktop mode as default over tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v SignInMode /t REG_DWORD /d 1 /f"), null),

            // disable tablet mode
            ("Disabling tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v TabletMode /t REG_DWORD /d 0 /f"), null),

            // disable tablet mode prompts and always switch
            ("Disabling tablet mode prompts and always switch", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell"" /v ConvertibleSlateModePromptPreference /t REG_DWORD /d 2 /f"), null),

            // enable taskbar in tablet mode
            ("Enabling taskbar when in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAppsVisibleInTabletMode /t REG_DWORD /d 1 /f"), null),

            // disable automatic hiding of the taskbar in tablet mode
            ("Disabling automatic hiding of the taskbar in tablet mode", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v TaskbarAutoHideInTabletMode /t REG_DWORD /d 0 /f"), null),

            // disable program compatibility assistant
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Windows\AppCompat"" /v DisablePCA /t REG_DWORD /d 1 /f"), null),
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableEngine /t REG_DWORD /d 1 /f"), null),
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v AITEnable /t REG_DWORD /d 0 /f"), null),
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableUAR /t REG_DWORD /d 1 /f"), null),
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v DisableInventory /t REG_DWORD /d 1 /f"), null),
            ("Disabling the program compatibility assistant", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\AppCompat"" /v SbEnable /t REG_DWORD /d 1 /f"), null),

            // disable game bar
            ("Disabling GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"" /v ""AppCaptureEnabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling GameBar", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\System\GameConfigStore"" /v ""GameDVR_Enabled"" /t REG_DWORD /d 0 /f"), null),

            // disable game bar presence writer
            ("Disabling the GameBar Presence Writer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter"" /v ActivationType /t REG_DWORD /d 0 /f"), null),

            // configure presentation modes
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_HonorUserFSEBehaviorMode"" /t REG_DWORD /d 1 /f"), null),
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_DXGIHonorFSEWindowsCompatible"" /t REG_DWORD /d 1 /f"), null),
            ("Configuring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_CURRENT_USER\SYSTEM\GameConfigStore"" /v ""GameDVR_FSEBehavior"" /t REG_DWORD /d 2 /f"), null),
            ("Confiugring presentation modes", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Dwm"" /v ""OverlayTestMode"" /t REG_DWORD /d 5 /f"), null),

            // disable remote assistance
            ("Disabling remote assistance", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Remote Assistance"" /v fAllowToGetHelp /t REG_DWORD /d 0 /f"), null),

            // disable search indexing
            ("Disabling search indexing", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WSearch"" /v Start /t REG_DWORD /d 4 /f"), null),

            // disable automatic folder type discovery
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg delete ""HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU"" /f"), null),
            ("Disabling automatic folder type discovery", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"" /v FolderType /t REG_SZ /d ""NotSpecified"" /f"), null),

            // disable jpeg wallpaper compression
            ("Disabling jpeg wallpaper compression", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v JPEGImportQuality /t REG_DWORD /d 100 /f"), null),

            // disable tracking recent files
            ("Disabling tracking of recent files", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v Start_TrackDocs /t REG_DWORD /d 0 /f"), null),

            // clear recent files on exit
            ("Clear recent files on exit", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v ClearRecentDocsOnExit /t REG_DWORD /d 1 /f"), null),

            // disable recent office files
            ("Disabling recent office files", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v DisableGraphRecentItems /t REG_DWORD /d 1 /f"), null),

            // disable shortcut text creation
            ("Disabling shortcut text creation", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v link /t REG_BINARY /d 00000000 /f"), null),

            // disable shortcut tracking
            ("Disabling shortcut tracking", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v LinkResolveIgnoreLinkInfo /t REG_DWORD /d 1 /f"), null),
            ("Disabling shortcut tracking", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoResolveTrack /t REG_DWORD /d 1 /f"), null),

            // show more details in copy dialog
            ("Show more details in copy dialog", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager"" /v EnthusiastMode /t REG_DWORD /d 1 /f"), null),

            // disable show extracted files when complete
            ("Disable showing extracted files when complete", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\ExtractionWizard"" /v ShowFiles /t REG_DWORD /d 0 /f"), null),

            // hide gallery from file explorer
            ("Hiding gallery from file explorer", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /f"), null),

            // disable autoplay
            ("Disabling autoplay", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Explorer"" /v NoAutoplayfornonVolume /t REG_DWORD /d 1 /f"), null),
            ("Disabling autoplay", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers"" /v DisableAutoplay /t REG_DWORD /d 1 /f"), null),

            // disable autorun from disks
            ("Disabling autorun from disks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoAutorun /t REG_DWORD /d 1 /f"), null),
            ("Disabling autorun from disks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),
            ("Disabling autorun from disks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"), null),

            // reduce mouse hover time on tooltips
            ("Disabling mouse hover time on tooltips", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseHoverTime /t REG_SZ /d 30 /f"), null),

            // remove delay when opening start menu
            ("Removing delay when opening the start menu", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v MenuShowDelay /t REG_SZ /d 0 /f"), null),

            // disable save your work prompt
            ("Disabling save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v AutoEndTasks /t REG_SZ /d 1 /f"), null),
            ("Disabling save your work prompt", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v AutoEndTasks /t REG_SZ /d 1 /f"), null),

            // reduce delay to end tasks on shutdown screen
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v HungAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v HungAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f"), null),
            ("Reducing delay to end tasks on shutdown screen", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f"), null),

            // reduce delay for low-level hooks
            ("Reducing delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Desktop"" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f"), null),
            ("Reducing delay for low-level hooks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_USERS\.DEFAULT\Control Panel\Desktop"" /v LowLevelHooksTimeout /t REG_SZ /d 1000 /f"), null),

            // reduce delay to end services on shutdown
            ("Reducing delay to end services on shutdown", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control"" /v WaitToKillServiceTimeout /t REG_SZ /d 1500 /f"), null),

            // disable telemetry
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx POWERSHELL_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOTNET_TRY_CLI_TELEMETRY_OPTOUT 1   "), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx CLOUDSDK_CORE_DISABLE_PROMPTS 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOTNET_CLI_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx DOCKER_CLI_TELEMETRY_OPTOUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx VS_TELEMETRY_OPT_OUT 1"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"setx npm_config_loglevel silent   "), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"" /v POWERSHELL_TELEMETRY_OPTOUT /t REG_SZ /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DiagTrack"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitDiagnosticLogCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitDumpCollection /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v LimitEnhancedDiagnosticDataWindowsAnalytics /t REG_DWORD /d 0 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DoNotShowFeedbackNotifications /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowCommercialDataPipeline /t REG_DWORD /d 0 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowDeviceNameInTelemetry /t REG_DWORD /d 0 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableEnterpriseAuthProxy /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v MicrosoftEdgeDataOptIn /t REG_DWORD /d 0 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableTelemetryOptInChangeNotification /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v DisableTelemetryOptInSettingsUx /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection"" /v AllowBuildPreview /t REG_DWORD /d 0 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows Defender\Reporting"" /v ""DisableGenericRePorts"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v AllowOnlineTips /t REG_DWORD /d 0 /f"), null),

            // disable wifi telemetry
            ("Disabling WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WcmSvc\wifinetworkmanager\config"" /v AutoConnectAllowedOEM /t REG_DWORD /d 0 /f"), null),
            ("Disabling WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowWiFiHotSpotReporting"" /v value /t REG_DWORD /d 0 /f"), null),
            ("Disabling WiFi telemetry", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\WiFi\AllowAutoConnectToWiFiSenseHotspots"" /v value /t REG_DWORD /d 0 /f"), null),

            // make microsoft account optional for microsoft store
            ("Making Microsoft Account optional for Microsoft Store", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System"" /v MSAOptional /t REG_DWORD /d 1 /f"), null),

            // optimize microsoft edge settings
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v UserFeedbackAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillCreditCardEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeCollectionsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowMicrosoftRewards /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeShoppingAssistantEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PersonalizationReportingEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ShowRecommendationsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WebWidgetAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MathSolverEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v InAppSupportEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v StartupBoostEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageSearchBox /t REG_SZ /d ""redirect"" /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v TyposquattingCheckerEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SiteSafetyServicesEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AlternateErrorPagesEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutofillAddressEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ConfigureDoNotTrack /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PaymentMethodQueryEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PromotionalTabsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v VisualSearchEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AutoImportAtFirstRun /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockExternalExtensions /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v FamilySafetySettingsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordGeneratorEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordManagerEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v PasswordMonitorAllowed /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v ResolveNavigationErrorsUseWebService /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v EdgeFollowEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AllowGamesMenu /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v BlockThirdPartyCookies /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultNotificationsSetting /t REG_DWORD /d 2 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v DefaultGeolocationSetting /t REG_DWORD /d 2 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AccessibilityImageLabelsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v QuickSearchShowMiniMenu /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v HubsSidebarEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v MicrosoftEdgeInsiderPromotionEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v WindowOcclusionEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SpotlightExperiencesAndRecommendationsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageAppLauncherEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v NewTabPageContentEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v AddressBarMicrosoftSearchInBingProviderEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge"" /v SearchInSidebarEnabled /t REG_DWORD /d 0 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableMFUTracking /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\EdgeUI"" /v DisableHelpSticker /t REG_DWORD /d 1 /f"), null),
            ("Optimizing Microsoft Edge settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\EdgeUpdate"" /v CreateDesktopShortcutDefault /t REG_DWORD /d 0 /f"), null),

            // restrict internet communication
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\InternetManagement"" /v RestrictCommunication /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoPublishingWizard /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoWebServices /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform"" /v NoGenTicket /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoOnlinePrintsWizard /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoInternetOpenWith /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows NT\Printers"" /v DisableHTTPPrinting /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows NT\Printers"" /v DisableWebPnPDownload /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\HandwritingErrorReports"" /v PreventHandwritingErrorReports /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Windows\TabletPC"" /v PreventHandwritingDataSharing /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Assistance\Client\1.0"" /v NoOnlineAssist /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Assistance\Client\1.0"" /v NoExplicitFeedback /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\Assistance\Client\1.0"" /v NoImplicitFeedback /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\WindowsMovieMaker"" /v WebHelp /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\WindowsMovieMaker"" /v CodecDownload /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Policies\Microsoft\WindowsMovieMaker"" /v WebPublish /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\InternetManagement"" /v RestrictCommunication /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoPublishingWizard /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoWebServices /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows NT\CurrentVersion\Software Protection Platform"" /v NoGenTicket /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoOnlinePrintsWizard /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\PCHealth\HelpSvc"" /v Headlines /t REG_DWORD /d 0 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\PCHealth\HelpSvc"" /v MicrosoftKBSearch /t REG_DWORD /d 0 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\PCHealth\ErrorReporting"" /v DoReport /t REG_DWORD /d 0 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Windows Error Reporting"" /v Disabled /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoInternetOpenWith /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Internet Connection Wizard"" /v ExitOnMSICW /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\EventViewer"" /v MicrosoftEventVwrDisableLinks /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\SystemCertificates\AuthRoot"" /v DisableRootAutoUpdate /t REG_DWORD /d 0 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Registration Wizard Control"" /v NoRegistration /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\SearchCompanion"" /v DisableContentFileUpdates /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows NT\Printers"" /v DisableHTTPPrinting /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows NT\Printers"" /v DisableWebPnPDownload /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\HandwritingErrorReports"" /v PreventHandwritingErrorReports /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\TabletPC"" /v PreventHandwritingDataSharing /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\WindowsMovieMaker"" /v WebHelp /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\WindowsMovieMaker"" /v CodecDownload /t REG_DWORD /d 1 /f"), null),
            ("Restricting internet communication", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\WindowsMovieMaker"" /v WebPublish /t REG_DWORD /d 1 /f"), null),

            // disable typing accesibility settings
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableSpellchecking /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableSpellchecking /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnablePredictionSpaceInsertion /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableDoubleTapSpace /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbTextPrediction /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v EnableHwkbAutocorrection /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\TabletTip\1.7"" /v MultilingualEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling typing accessibility settings", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\TabletTip\1.7"" /v MultilingualEnabled /t REG_DWORD /d 0 /f"), null),

            // disabling sticky keys
            ("Disabling sticky keys", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Accessibility\StickyKeys"" /v Flags /t REG_SZ /d ""506"" /f"), null),

            // disable typing insights
            ("Disabling typing insights", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\input\Settings"" /v InsightsEnabled /t REG_DWORD /d 0 /f"), null),

            // increase keyboard repeat rate
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v InitialKeyboardIndicators /t REG_SZ /d ""0"" /f"), null),
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v KeyboardDelay /t REG_SZ /d ""0"" /f"), null),
            ("Increasing keyboard repeat rate", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Keyboard"" /v KeyboardSpeed /t REG_SZ /d ""31"" /f"), null),

            // disable mouse acceleration
            ("Disabling mouse acceleration", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseSpeed /t REG_SZ /d 0 /f"), null),
            ("Disabling mouse acceleration", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseThreshold1 /t REG_SZ /d 0 /f"), null),
            ("Disabling mouse acceleration", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Control Panel\Mouse"" /v MouseThreshold2 /t REG_SZ /d 0 /f"), null),

            // disabling clipboard history
            ("Disabling clipboard history", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Clipboard"" /v EnableClipboardHistory /t REG_DWORD /d 0 /f"), null),
            ("Disabling clipboard history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowClipboardHistory /t REG_DWORD /d 1 /f"), null),
            ("Disabling clipboard history", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\System"" /v AllowCrossDeviceClipboard /t REG_DWORD /d 1 /f"), null),

            // disable settings sync
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableApplicationSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableApplicationSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWebBrowserSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWebBrowserSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableDesktopThemeSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableDesktopThemeSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableSyncOnPaidNetwork /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWindowsSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableWindowsSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableCredentialsSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableCredentialsSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisablePersonalizationSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisablePersonalizationSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableStartLayoutSettingSync /t REG_DWORD /d 2 /f"), null),
            ("Disabling settings sync", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\SettingSync"" /v DisableStartLayoutSettingSyncUserOverride /t REG_DWORD /d 1 /f"), null),

            // disable disk space checks
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("CurrentUser", @"reg add ""HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v NoLowDiskSpaceChecks /t REG_DWORD /d 1 /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Session Manager"" /v BootExecute /t REG_MULTI_SZ /d ""autocheck autochk /k:C*"" /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Dfrg\BootOptimizeFunction"" /v Enable /t REG_SZ /d ""N"" /f"), null),
            ("Disabling disk space checks", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OptimalLayout"" /v EnableAutoLayout /t REG_DWORD /d 0 /f"), null),

            // disable hibernation
            ("Disabling hibernation", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power"" /v HibernateEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling hibernation", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v HiberbootEnabled /t REG_DWORD /d 0 /f"), null),
            ("Disabling hibernation", async () => await ProcessActions.RunNsudo("CurrentUser", @"powercfg /h off"), null),

            // disable cpu power saving features
            ("Disabling CPU power saving features", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Power"" /v ""CoalescingTimerInterval"" /t REG_DWORD /d 0 /f"), null),

            // reserve 10% of CPU resources to low-priority tasks instead of 20%
            ("Reserving 10% of CPU resources to low-priority tasks instead of 20%", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"" /v SystemResponsiveness /t REG_DWORD /d 10 /f"), null),

            // allocate cpu resources primarily to programs
            ("Allocating CPU resources primarily to programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f"), null),

            // disabling background apps
            ("Disabling background apps", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"" /v LetAppsRunInBackground /t REG_DWORD /d 2 /f"), null),

            // optimizing priorities
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\SearchIndexer.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 5 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\ctfmon.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 5 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\fontdrvhost.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\winlogon.exe\PerfOptions"" /v ""CpuPriorityClass"" /t REG_DWORD /d 2 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\winlogon.exe\PerfOptions"" /v ""IoPriority"" /t REG_DWORD /d 2 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\lsass.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\lsass.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sihost.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sppsvc.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\sppsvc.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\svchost.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\WmiPrvSE.exe\PerfOptions"" /v CpuPriorityClass /t REG_DWORD /d 1 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\WmiPrvSE.exe\PerfOptions"" /v IoPriority /t REG_DWORD /d 0 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""CpuPriorityClass"" /t REG_DWORD /d 3 /f"), null),
            ("Optimizing priorities", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions"" /v ""IoPriority"" /t REG_DWORD /d 3 /f"), null),

            // disable autorun entries
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot"" /v ""AlternateShell"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot\AutorunsDisabled"" /v ""AlternateShell"" /t REG_SZ /d ""cmd.exe"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunCustom(async () => edgeVersion = await Task.Run(() => FileVersionInfo.GetVersionInfo(Environment.ExpandEnvironmentVariables(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")).ProductVersion)), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v """" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Localized Name"" /t REG_SZ /d ""Microsoft Edge"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", $@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{{9459C573-B17A-45AE-9F64-1857B5D58CEE}}"" /v ""StubPath"" /t REG_SZ /d ""\""C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\{edgeVersion}\\Installer\\setup.exe\"" --configure-user-settings --verbose-logging --system-level --msedge --channel=stable"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""Version"" /t REG_SZ /d ""43,0,0,0"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{9459C573-B17A-45AE-9F64-1857B5D58CEE}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\System32\Rundll32.exe C:\Windows\System32\mscories.dll,Install"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""ComponentID"" /t REG_SZ /d ""DOTNETFRAMEWORKS"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""DontAsk"" /t REG_DWORD /d 2 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""Enabled"" /t REG_DWORD /d 0 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""IsInstalled"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\AutorunsDisabled\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /v ""StubPath"" /t REG_SZ /d ""C:\Windows\SysWOW64\Rundll32.exe C:\Windows\SysWOW64\mscories.dll,Install"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\{89B4C1CD-B018-4511-B0A1-5476DBF70820}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v """" /t REG_SZ /d ""IEToEdge BHO"" /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\AutorunsDisabled\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /v ""NoExplorer"" /t REG_DWORD /d 1 /f"), null),
            ("Disabling Autorun entries", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg delete ""HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}"" /f"), null),

            ("Disabling Autorun entries", async () => await ProcessActions.RunPowerShell(@"Get-ScheduledTask | Where-Object {$_.TaskName -like 'UnlockStartLayout*'} | Unregister-ScheduledTask -Confirm:$false"), null),

            // disable potentially unwanted windows programs
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\AggregatorHost.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BCILauncher.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BGAUpsell.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\BingChatInstaller.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\CompatTelRunner.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\DeviceCensus.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FeatureLoader.exe"" /v Debugger /t REG_SZ /d ""%windir%\System32\taskkill.exe"" /f"), null),
            ("Disabling potentially unwanted windows programs", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c ren C:\Windows\HelpPane.exe HelpPane.exee"), null),

            // disable unnecessary services
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\dam"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DPS"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\diagnosticshub.standardcollector.service"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FontCache3.0.0.0"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"cmd /c reg query ""HKLM\SYSTEM\CurrentControlSet\Services\GpuEnergyDrv"" && reg add ""HKLM\SYSTEM\CurrentControlSet\Services\GpuEnergyDrv"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OneSyncSvc"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\PcaSvc"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\tcpipreg"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiServiceHost"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WdiSystemHost"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Wecsvc"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\WerSvc"" /v Start /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\wisvc"" /v Start /t REG_DWORD /d 4 /f"), null),

            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\SysMain"" /v ""Start"" /t REG_DWORD /d 4 /f"), () => SSD == true),

            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdate"" /v ""Start"" /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\edgeupdatem"" /v ""Start"" /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\FontCache3.0.0.0"" /v ""Start"" /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\GameInputSvc"" /v ""Start"" /t REG_DWORD /d 4 /f"), null),
            ("Disabling unnecessary services", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\System\CurrentControlSet\Services\MicrosoftEdgeElevationService"" /v ""Start"" /t REG_DWORD /d 4 /f"), null),

            // rename device
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation"" /v Model /t REG_SZ /d ""AutoOS"" /f"), () => Rename == true),
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /v RegisteredOrganization /t REG_SZ /d ""AutoOS"" /f"), null),
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /v EditionSubManufacturer /t REG_SZ /d ""tinodin"" /f"), null),
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /v EditionSubstring /t REG_SZ /d ""AutoOS"" /f"), null),
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /v EditionSubVersion /t REG_SZ /d ""AutoOS"" /f"), null),
            ("Renaming device", async () => await ProcessActions.RunNsudo("TrustedInstaller", @"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /v RegisteredOrganization /t REG_SZ /d ""AutoOS"" /f"), null),
        };

        var filteredActions = actions.Where(a => a.Condition == null || a.Condition.Invoke()).ToList();
        int groupedTitleCount = 0;

        List<Func<Task>> currentGroup = new();

        for (int i = 0; i < filteredActions.Count; i++)
        {
            if (i == 0 || filteredActions[i].Title != filteredActions[i - 1].Title)
            {
                groupedTitleCount++;
            }
        }

        double incrementPerTitle = groupedTitleCount > 0 ? stagePercentage / (double)groupedTitleCount : 0;

        foreach (var (title, action, condition) in filteredActions)
        {
            if (previousTitle != string.Empty && previousTitle != title && currentGroup.Count > 0)
            {
                foreach (var groupedAction in currentGroup)
                {
                    try
                    {
                        await groupedAction();
                    }
                    catch (Exception ex)
                    {
                        InstallPage.Info.Title += ": " + ex.Message;
                        InstallPage.Info.Severity = InfoBarSeverity.Error;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                        InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                        InstallPage.ResumeButton.Visibility = Visibility.Visible;

                        var tcs = new TaskCompletionSource<bool>();

                        InstallPage.ResumeButton.Click += (sender, e) =>
                        {
                            tcs.TrySetResult(true);
                            InstallPage.Info.Severity = InfoBarSeverity.Informational;
                            InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                            InstallPage.ProgressRingControl.Foreground = null;
                            InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                            InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                        };

                        await tcs.Task;
                    }
                }

                InstallPage.Progress.Value += incrementPerTitle;
                await Task.Delay(150);
                currentGroup.Clear();
            }

            InstallPage.Info.Title = title + "...";
            currentGroup.Add(action);
            previousTitle = title;
        }

        if (currentGroup.Count > 0)
        {
            foreach (var groupedAction in currentGroup)
            {
                try
                {
                    await groupedAction();
                }
                catch (Exception ex)
                {
                    InstallPage.Info.Title += ": " + ex.Message;
                    InstallPage.Info.Severity = InfoBarSeverity.Error;
                    InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                    InstallPage.ProgressRingControl.Visibility = Visibility.Collapsed;
                    InstallPage.ResumeButton.Visibility = Visibility.Visible;

                    var tcs = new TaskCompletionSource<bool>();

                    InstallPage.ResumeButton.Click += (sender, e) =>
                    {
                        tcs.TrySetResult(true);
                        InstallPage.Info.Severity = InfoBarSeverity.Informational;
                        InstallPage.Progress.Foreground = (Brush)Application.Current.Resources["AccentForegroundBrush"];
                        InstallPage.ProgressRingControl.Foreground = null;
                        InstallPage.ProgressRingControl.Visibility = Visibility.Visible;
                        InstallPage.ResumeButton.Visibility = Visibility.Collapsed;
                    };

                    await tcs.Task;
                }
            }

            InstallPage.Progress.Value += incrementPerTitle;
        }
    }
}