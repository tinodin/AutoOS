using AutoOS.Views.Installer.Actions;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.Views.Installer.Stages;

public static class TimeDateRegionStage
{
    private static string countryCode = null;
    public static async Task Run()
    {
        InstallPage.Status.Text = "Time, Date and Region...";

        string previousTitle = string.Empty;
        int stagePercentage = 2;

        using (HttpClient client = new HttpClient())
        {
            string response = client.GetStringAsync("https://get.geojs.io/v1/ip/geo.json").Result;
            JObject jsonResponse = JObject.Parse(response);

            countryCode = jsonResponse["country_code"]?.ToString();
        }

        var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>
        {
            // set time zone automatically
            ($"Setting time zone to {GetWindowsTimeZone(countryCode)}", async () => await ProcessActions.RunNsudo("CurrentUser", $@"powershell -Command ""Set-TimeZone -Id '{GetWindowsTimeZone(countryCode)}'"""), null),

            // set keyboard layout automatically
            ($"Setting keyboard layout to {GetKeyboardLayout(countryCode)}", async () => await ProcessActions.RunNsudo("CurrentUser", $@"powershell -Command ""$langList = New-WinUserLanguageList en-US; $langList[0].InputMethodTips.Clear(); $langList[0].InputMethodTips.Add('{GetKeyboardLayout(countryCode)}'); Set-WinUserLanguageList $langList -Force"""), null),

            // set regional format automatically
            ($"Setting regional format to en-{countryCode}", async () => await ProcessActions.RunNsudo("CurrentUser", $@"powershell -Command ""Set-Culture en-{countryCode}"""), null),

            // sync time
            ("Syncing time", async () => await ProcessActions.RunNsudo("CurrentUser", "net start w32time"), null),
            ("Syncing time", async () => await ProcessActions.RunNsudo("CurrentUser", "w32tm /resync"), null),
            ("Syncing time", async () => await ProcessActions.RunNsudo("CurrentUser", "net stop w32time"), null),

            // apply changes to the whole system
            ("Applying changes to the whole system", async () => await ProcessActions.RunNsudo("CurrentUser", @"powershell -Command ""Copy-UserInternationalSettingsToSystem -WelcomeScreen $true -NewUser $true"""), null),
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

    private static string GetWindowsTimeZone(string countryCode)
    {
        return countryCode switch
        {
            "AF" => "Afghanistan Standard Time",
            "AL" => "Central European Standard Time",
            "DZ" => "Central European Standard Time",
            "AS" => "UTC-11",
            "AD" => "Central European Standard Time",
            "AO" => "West Africa Standard Time",
            "AG" => "Atlantic Standard Time",
            "AR" => "Argentina Standard Time",
            "AM" => "Armenian Standard Time",
            "AW" => "Atlantic Standard Time",
            "AU" => "AUS Eastern Standard Time",
            "AT" => "Central European Standard Time",
            "AZ" => "Azerbaijan Standard Time",
            "BS" => "Eastern Standard Time",
            "BH" => "Arabian Standard Time",
            "BD" => "Bangladesh Standard Time",
            "BB" => "Atlantic Standard Time",
            "BY" => "Eastern European Standard Time",
            "BE" => "Romance Standard Time",
            "BZ" => "Central Standard Time",
            "BJ" => "West Africa Standard Time",
            "BA" => "Central European Standard Time",
            "BW" => "Central Africa Standard Time",
            "BR" => "Brazil Standard Time",
            "BN" => "Brunei Darussalam Standard Time",
            "BG" => "Eastern European Standard Time",
            "BF" => "West Africa Standard Time",
            "BI" => "Central Africa Standard Time",
            "KH" => "Indochina Time",
            "CM" => "West Africa Standard Time",
            "CA" => "Pacific Standard Time",
            "CV" => "Cape Verde Standard Time",
            "KY" => "Eastern Standard Time",
            "CF" => "West Africa Standard Time",
            "TD" => "West Africa Standard Time",
            "CL" => "Pacific SA Standard Time",
            "CN" => "China Standard Time",
            "CO" => "SA Pacific Standard Time",
            "KM" => "Comoros Standard Time",
            "CD" => "Congo Standard Time",
            "CG" => "West Africa Standard Time",
            "CR" => "Central Standard Time",
            "HR" => "Central European Standard Time",
            "CU" => "Cuba Standard Time",
            "CY" => "Eastern European Standard Time",
            "CZ" => "Central European Standard Time",
            "DK" => "Romance Standard Time",
            "DJ" => "East Africa Standard Time",
            "DM" => "Atlantic Standard Time",
            "DO" => "Atlantic Standard Time",
            "EC" => "Ecuador Time",
            "EG" => "Egypt Standard Time",
            "SV" => "Central Standard Time",
            "GQ" => "West Africa Standard Time",
            "ER" => "East Africa Standard Time",
            "EE" => "Eastern European Standard Time",
            "SZ" => "South Africa Standard Time",
            "ET" => "East Africa Standard Time",
            "FJ" => "Fiji Standard Time",
            "FI" => "FLE Standard Time",
            "FR" => "Romance Standard Time",
            "GA" => "West Africa Standard Time",
            "GM" => "Greenwich Mean Time",
            "GE" => "Georgia Standard Time",
            "DE" => "W. Europe Standard Time",
            "GH" => "Greenwich Mean Time",
            "GR" => "Eastern European Standard Time",
            "GT" => "Central Standard Time",
            "GN" => "Greenwich Mean Time",
            "GW" => "Greenwich Mean Time",
            "GY" => "Guyana Time",
            "HT" => "Haiti Standard Time",
            "HN" => "Central Standard Time",
            "HK" => "China Standard Time",
            "HU" => "Central European Standard Time",
            "IS" => "Greenwich Mean Time",
            "IN" => "India Standard Time",
            "ID" => "W. Indonesia Time",
            "IR" => "Iran Standard Time",
            "IQ" => "Arabian Standard Time",
            "IE" => "GMT Standard Time",
            "IL" => "Israel Standard Time",
            "IT" => "W. Europe Standard Time",
            "JM" => "Jamaica Time",
            "JP" => "Tokyo Standard Time",
            "JE" => "GMT Standard Time",
            "JO" => "Arabian Standard Time",
            "KZ" => "Central Asia Standard Time",
            "KE" => "East Africa Standard Time",
            "KI" => "Gilbert Islands Time",
            "KP" => "North Korea Standard Time",
            "KR" => "Korea Standard Time",
            "KW" => "Arabian Standard Time",
            "KG" => "Kyrgyzstan Standard Time",
            "LA" => "Indochina Time",
            "LV" => "Eastern European Standard Time",
            "LB" => "Middle East Standard Time",
            "LS" => "South Africa Standard Time",
            "LR" => "Greenwich Mean Time",
            "LY" => "Eastern European Standard Time",
            "LT" => "E. Europe Standard Time",
            "LU" => "Romance Standard Time",
            "MO" => "China Standard Time",
            "MK" => "Central European Standard Time",
            "MG" => "East Africa Standard Time",
            "MW" => "Central Africa Time",
            "MY" => "Malaysia Standard Time",
            "MV" => "Maldives Standard Time",
            "ML" => "GMT Standard Time",
            "MT" => "Central European Standard Time",
            "MH" => "UTC+12",
            "MR" => "West Africa Standard Time",
            "MU" => "Mauritius Standard Time",
            "MX" => "Pacific Standard Time",
            "FM" => "UTC+10",
            "MD" => "Eastern European Standard Time",
            "MC" => "Central European Standard Time",
            "MN" => "Mongolia Standard Time",
            "ME" => "Central European Standard Time",
            "MA" => "Morocco Standard Time",
            "MZ" => "Central Africa Time",
            "MM" => "Myanmar Standard Time",
            "NA" => "Namibia Standard Time",
            "NP" => "Nepal Standard Time",
            "NL" => "W. Europe Standard Time",
            "NZ" => "New Zealand Standard Time",
            "NI" => "Central Standard Time",
            "NE" => "West Africa Standard Time",
            "NG" => "West Africa Standard Time",
            "NO" => "W. Europe Standard Time",
            "OM" => "Arabian Standard Time",
            "PK" => "Pakistan Standard Time",
            "PW" => "UTC+9",
            "PA" => "Eastern Standard Time",
            "PG" => "Papua New Guinea Standard Time",
            "PY" => "Paraguay Standard Time",
            "PE" => "Peru Standard Time",
            "PH" => "Philippine Standard Time",
            "PL" => "Central European Standard Time",
            "PT" => "Pacific Standard Time",
            "PR" => "Atlantic Standard Time",
            "QA" => "Arabian Standard Time",
            "RE" => "Reunion Standard Time",
            "RO" => "Eastern European Standard Time",
            "RU" => "Russian Standard Time",
            "RW" => "Central Africa Time",
            "SA" => "Arabian Standard Time",
            "SB" => "Solomon Islands Standard Time",
            "SC" => "Seychelles Standard Time",
            "SD" => "Central Africa Time",
            "SE" => "W. Europe Standard Time",
            "SG" => "Singapore Standard Time",
            "SI" => "Central European Standard Time",
            "SK" => "Central European Standard Time",
            "SL" => "Greenwich Mean Time",
            "SN" => "Greenwich Mean Time",
            "SO" => "East Africa Standard Time",
            "ZA" => "South Africa Standard Time",
            "SS" => "Central Africa Time",
            "ES" => "Romance Standard Time",
            "LK" => "India Standard Time",
            "CH" => "W. Europe Standard Time",
            "TZ" => "East Africa Standard Time",
            "TH" => "Indochina Time",
            "TG" => "West Africa Standard Time",
            "TK" => "UTC+13",
            "TO" => "Tonga Standard Time",
            "TT" => "Atlantic Standard Time",
            "TN" => "Central European Standard Time",
            "TR" => "Turkey Standard Time",
            "TM" => "Turkmenistan Standard Time",
            "TV" => "UTC+12",
            "UG" => "East Africa Time",
            "UA" => "FLE Standard Time",
            "AE" => "Arabian Standard Time",
            "GB" => "GMT Standard Time",
            "US" => "Pacific Standard Time",
            "UY" => "Montevideo Standard Time",
            "UZ" => "Uzbekistan Standard Time",
            "VU" => "Vanuatu Standard Time",
            "VE" => "Venezuela Standard Time",
            "VN" => "SE Asia Standard Time",
            "YE" => "Arabian Standard Time",
            "ZM" => "Central Africa Time",
            "ZW" => "Central Africa Time",
            _ => "UTC"
        };
    }

    private static string GetKeyboardLayout(string countryCode)
    {
        return countryCode switch
        {
            "AF" => "0409:00000401", // Arabic (Saudi Arabia)
            "AM" => "0409:0000045E", // Amharic
            "AR" => "0409:0000040A", // Spanish (Argentina)
            "AS" => "0409:0000044D", // Assamese
            "AT" => "0409:00000407", // German (Austria)
            "AU" => "0409:00000C09", // English (Australia)
            "AZ" => "0409:0000042C", // Azerbaijani (Latin)
            "BE" => "0409:00000813", // Dutch (Belgium)
            "BG" => "0409:00000402", // Bulgarian
            "BN" => "0409:00000445", // Bangla (Bangladesh)
            "BO" => "0409:0000040A", // Spanish (Bolivia)
            "BR" => "0409:00000416", // Portuguese (Brazil)
            "BS" => "0409:0000201A", // Bosnian (Latin)
            "BY" => "0409:00000423", // Belarusian
            "CA" => "0409:00001009", // English (Canada)
            "CH" => "0409:00001007", // German (Switzerland)
            "CL" => "0409:0000040A", // Spanish (Chile)
            "CN" => "0409:00000804", // Chinese (Simplified)
            "CO" => "0409:0000040A", // Spanish (Colombia)
            "CR" => "0409:0000040A", // Spanish (Costa Rica)
            "CS" => "0409:00000405", // Czech
            "CY" => "0409:00000452", // Welsh
            "CZ" => "0409:00000405", // Czech
            "DA" => "0409:00000406", // Danish
            "DE" => "0409:00000407", // German (Germany)
            "DK" => "0409:00000406", // Danish
            "DO" => "0409:0000040A", // Spanish (Dominican Republic)
            "EC" => "0409:0000040A", // Spanish (Ecuador)
            "EE" => "0409:00000425", // Estonian
            "EG" => "0409:00000401", // Arabic (Egypt)
            "ES" => "0409:0000040A", // Spanish (Spain)
            "ET" => "0409:00000425", // Estonian
            "FI" => "0409:0000040B", // Finnish
            "FR" => "0409:0000040C", // French (France)
            "GA" => "0409:00001809", // Irish
            "GB" => "0409:00000809", // English (United Kingdom)
            "GR" => "0409:00000408", // Greek
            "GT" => "0409:0000040A", // Spanish (Guatemala)
            "HK" => "0409:00000C04", // Chinese (Traditional, Hong Kong)
            "HN" => "0409:0000040A", // Spanish (Honduras)
            "HR" => "0409:0000041A", // Croatian
            "HU" => "0409:0000040E", // Hungarian
            "ID" => "0409:00000421", // Indonesian
            "IE" => "0409:00001809", // Irish
            "IL" => "0409:0000040D", // Hebrew
            "IN" => "0409:00000439", // Hindi
            "IQ" => "0409:00000401", // Arabic (Iraq)
            "IS" => "0409:0000040F", // Icelandic
            "IT" => "0409:00000410", // Italian (Italy)
            "JM" => "0409:00000409", // English (Jamaica)
            "JO" => "0409:00000401", // Arabic (Jordan)
            "JP" => "0409:00000411", // Japanese
            "KE" => "0409:00000409", // English (Kenya)
            "KG" => "0409:00000440", // Kyrgyz (Cyrillic)
            "KH" => "0409:00000453", // Khmer
            "KR" => "0409:00000412", // Korean
            "KW" => "0409:00000401", // Arabic (Kuwait)
            "KZ" => "0409:0000043F", // Kazakh
            "LA" => "0409:00000454", // Lao
            "LB" => "0409:00000401", // Arabic (Lebanon)
            "LT" => "0409:00000427", // Lithuanian
            "LU" => "0409:0000040C", // French (Luxembourg)
            "LV" => "0409:00000426", // Latvian
            "LY" => "0409:00000401", // Arabic (Libya)
            "MA" => "0409:00000401", // Arabic (Morocco)
            "MK" => "0409:0000042F", // Macedonian
            "MN" => "0409:00000450", // Mongolian (Cyrillic)
            "MO" => "0409:00001404", // Chinese (Traditional, Macao)
            "MX" => "0409:0000080A", // Spanish (Mexico)
            "MY" => "0409:0000043E", // Malay (Malaysia)
            "NG" => "0409:00000409", // English (Nigeria)
            "NL" => "0409:00000413", // Dutch (Netherlands)
            "NO" => "0409:00000414", // Norwegian (Bokmål)
            "NZ" => "0409:00000409", // English (New Zealand)
            "OM" => "0409:00000401", // Arabic (Oman)
            "PA" => "0409:0000040A", // Spanish (Panama)
            "PE" => "0409:0000040A", // Spanish (Peru)
            "PH" => "0409:0000042D", // Filipino
            "PK" => "0409:00000420", // Urdu
            "PL" => "0409:00000415", // Polish
            "PT" => "0409:00000816", // Portuguese (Portugal)
            "PY" => "0409:0000040A", // Spanish (Paraguay)
            "QA" => "0409:00000401", // Arabic (Qatar)
            "RO" => "0409:00000418", // Romanian (Romania)
            "RS" => "0409:00000C1A", // Serbian (Latin, Serbia)
            "RU" => "0409:00000419", // Russian
            "SA" => "0409:00000401", // Arabic (Saudi Arabia)
            "SE" => "0409:0000041D", // Swedish
            "SG" => "0409:00000409", // English (Singapore)
            "SI" => "0409:00000424", // Slovenian
            "SK" => "0409:0000041B", // Slovak
            "SV" => "0409:0000040A", // Spanish (El Salvador)
            "TH" => "0409:0000041E", // Thai
            "TN" => "0409:00000401", // Arabic (Tunisia)
            "TR" => "0409:0000041F", // Turkish
            "UA" => "0409:00000422", // Ukrainian
            "US" => "0409:00000409", // English (United States)
            "UY" => "0409:0000040A", // Spanish (Uruguay)
            "UZ" => "0409:00000443", // Uzbek (Latin)
            "VE" => "0409:0000040A", // Spanish (Venezuela)
            "VN" => "0409:0000042A", // Vietnamese
            "YE" => "0409:00000401", // Arabic (Yemen)
            "ZA" => "0409:00000409", // English (South Africa)
            "ZW" => "0409:00000409", // English (Zimbabwe)
            _ => null
        };
    }
}