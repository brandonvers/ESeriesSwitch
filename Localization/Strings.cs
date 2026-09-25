namespace ESeriesSwitch.Localization
{
    /// <summary>All UI texts in English and Hungarian. Keys missing from Hungarian fall back to English.</summary>
    static class Strings
    {
        public static readonly Dictionary<string, string> English = new()
        {
            // Header
            ["Subtitle"] = "Switch between the legacy E-series coding tools (INPA, WinKFP, NCS Expert, Tool32) and BMW's official ISTA+ downloaded from AOS.",

            // Status card
            ["CurrentlyActive"] = "CURRENTLY ACTIVE",
            ["Loading"] = "Loading…",
            ["ModeIsta"] = "Factory ISTA+ mode",
            ["ModeIstaDesc"] = "EDIABAS is not in the system environment variables, ISTA+ can be used.",
            ["ModeESeries"] = "E-series mode",
            ["ModeESeriesDesc"] = "INPA / WinKFP / NCS Expert / Tool32 can be used. ISTA+ will not work properly in this mode.",
            ["ModeMixed"] = "Partial state",
            ["ModeMixedDesc"] = "Only one of the two settings is present. Choose a mode below to fix it.",
            ["NotSet"] = "✗  not set",
            ["PathContains"] = "✓  contains: {0}",
            ["PathMissing"] = "✗  does not contain the EDIABAS folder",

            // EDIABAS interface card
            ["InterfaceHeader"] = "EDIABAS INTERFACE",
            ["InterfaceIcomTitle"] = "ICOM",
            ["InterfaceOfflineTitle"] = "Offline (no interface)",
            ["InterfaceOtherTitle"] = "Other interface",
            ["InterfaceUnreadable"] = "Cannot be read",
            ["InterfaceHintIcom"] = "INPA / Tool32 look for the ICOM on startup. If it is not connected, you get a “NET-0009: TIMEOUT” error. Switch to Offline in that case.",
            ["InterfaceHintOffline"] = "INPA / Tool32 start without an error message, but cannot communicate with the car. Switch to ICOM for diagnostics.",
            ["InterfaceHintOther"] = "EDIABAS.INI is set to neither ICOM nor NUL.",
            ["ButtonIcom"] = "ICOM",
            ["ButtonOffline"] = "Offline",

            // Buttons
            ["SwitchToESeries"] = "Switch to E-series tools  (INPA / WinKFP / EDIABAS)",
            ["SwitchToIsta"] = "Switch to factory ISTA+",
            ["RebootRecommended"] = "A restart is recommended for the change to take full effect.",
            ["RestartNow"] = "Restart now",
            ["Refresh"] = "Refresh",
            ["OpenBackups"] = "Backups folder",
            ["About"] = "About",

            // Warnings
            ["WarnConfigDirMissingFolder"] = "{0} points to a folder that does not exist: {1}",
            ["WarnFolderMissing"] = "The EDIABAS folder was not found on this computer ({0}).",
            ["WarnUserVar"] = "{0} is also set among the user variables. The app does not change it; consider removing it manually.",
            ["WarnUserPath"] = "The user PATH also contains the EDIABAS folder. The app does not change it; consider removing it manually.",

            // Messages
            ["ErrReadEnv"] = "Could not read the environment variables:\n\n{0}",
            ["ErrEnvKeyRead"] = "The system environment variables registry key cannot be read.",
            ["ErrEnvKeyWrite"] = "The system environment variables registry key cannot be written.",
            ["ErrNoPermission"] = "No permission to modify the system environment variables.\nRun the app as administrator.",
            ["ErrSwitchFailed"] = "The switch failed:\n\n{0}",
            ["SwitchedESeries"] = "E-series mode (INPA / WinKFP / EDIABAS)",
            ["SwitchedIsta"] = "factory ISTA+ mode",
            ["IcomNote"] = "EDIABAS is set to ICOM: without a connected ICOM, INPA shows a “NET-0009: TIMEOUT” error. If no ICOM is connected, switch to Offline below.\n\n",
            ["SwitchSuccess"] = "Switched successfully: {0}.\n\n{1}Newly started programs already see the new setting, but a restart is recommended for reliable operation (especially because of the ISTA+ services).\n\nRestart the computer now?",
            ["ConfirmRestart"] = "Restart the computer now? Save your open work first!",
            ["ErrRestart"] = "Could not start the restart:\n\n{0}",
            ["ErrIniWrite"] = "Could not modify EDIABAS.INI:\n\n{0}",
            ["ErrInterfaceLineMissing"] = "No 'Interface' line was found in the [Configuration] section of EDIABAS.INI.",
            ["AboutText"] =
                "E-Series ⇄ ISTA+ Switch  v{0}\n\n" +
                "Free, open-source tool (MIT License).\n{1}\n\n" +
                "Not affiliated with, endorsed or supported by BMW AG. BMW, MINI, ISTA, INPA, WinKFP, NCS Expert and EDIABAS " +
                "are trademarks of their respective owners.\n\n" +
                "Use at your own risk. The software is provided “as is”, without warranty of any kind. The author accepts no " +
                "liability for any damage to vehicles, control units, computers or data.\n\n" +
                "Open the GitHub page?",
        };

        public static readonly Dictionary<string, string> Hungarian = new()
        {
            // Header
            ["Subtitle"] = "Váltás a régi E-szériás kódoló toolok (INPA, WinKFP, NCS Expert, Tool32) és a gyári, AOS-ból letöltött ISTA+ között.",

            // Status card
            ["CurrentlyActive"] = "JELENLEG AKTÍV",
            ["Loading"] = "Betöltés…",
            ["ModeIsta"] = "Gyári ISTA+ mód",
            ["ModeIstaDesc"] = "Az EDIABAS nincs a rendszer környezeti változói között, az ISTA+ használható.",
            ["ModeESeries"] = "E-szériás mód",
            ["ModeESeriesDesc"] = "INPA / WinKFP / NCS Expert / Tool32 használható. Az ISTA+ ilyenkor nem fog rendesen működni.",
            ["ModeMixed"] = "Részleges állapot",
            ["ModeMixedDesc"] = "Csak az egyik beállítás van jelen. Válassz lent egy módot a rendbetételhez.",
            ["NotSet"] = "✗  nincs beállítva",
            ["PathContains"] = "✓  tartalmazza: {0}",
            ["PathMissing"] = "✗  nem tartalmazza az EDIABAS mappát",

            // EDIABAS interface card
            ["InterfaceHeader"] = "EDIABAS INTERFÉSZ",
            ["InterfaceIcomTitle"] = "ICOM",
            ["InterfaceOfflineTitle"] = "Offline (nincs interfész)",
            ["InterfaceOtherTitle"] = "Egyéb interfész",
            ["InterfaceUnreadable"] = "Nem olvasható",
            ["InterfaceHintIcom"] = "Az INPA / Tool32 indításkor az ICOM-ot keresi. Ha nincs csatlakoztatva, „NET-0009: TIMEOUT” hibát kapsz. Ilyenkor válts Offline-ra.",
            ["InterfaceHintOffline"] = "Az INPA / Tool32 hibaüzenet nélkül indul, de az autóval nem tud kommunikálni. Diagnosztikához válts ICOM-ra.",
            ["InterfaceHintOther"] = "Az EDIABAS.INI-ben nem ICOM és nem NUL van beállítva.",
            ["ButtonIcom"] = "ICOM",
            ["ButtonOffline"] = "Offline",

            // Buttons
            ["SwitchToESeries"] = "Váltás E-szériás toolokra  (INPA / WinKFP / EDIABAS)",
            ["SwitchToIsta"] = "Váltás gyári ISTA+ -ra",
            ["RebootRecommended"] = "A váltás teljes érvényesüléséhez újraindítás ajánlott.",
            ["RestartNow"] = "Újraindítás most",
            ["Refresh"] = "Frissítés",
            ["OpenBackups"] = "Mentések mappája",
            ["About"] = "Névjegy",

            // Warnings
            ["WarnConfigDirMissingFolder"] = "Az {0} egy nem létező mappára mutat: {1}",
            ["WarnFolderMissing"] = "Az EDIABAS mappa nem található ezen a gépen ({0}).",
            ["WarnUserVar"] = "A felhasználói változók között is van {0}. Ezt az app nem módosítja, érdemes kézzel törölni.",
            ["WarnUserPath"] = "A felhasználói PATH is tartalmazza az EDIABAS mappát. Ezt az app nem módosítja, érdemes kézzel törölni.",

            // Messages
            ["ErrReadEnv"] = "Nem sikerült beolvasni a környezeti változókat:\n\n{0}",
            ["ErrEnvKeyRead"] = "A rendszer környezeti változóinak registry kulcsa nem olvasható.",
            ["ErrEnvKeyWrite"] = "A rendszer környezeti változóinak registry kulcsa nem írható.",
            ["ErrNoPermission"] = "Nincs jogosultság a rendszer környezeti változóinak módosításához.\nIndítsd az appot rendszergazdaként.",
            ["ErrSwitchFailed"] = "A váltás nem sikerült:\n\n{0}",
            ["SwitchedESeries"] = "E-szériás mód (INPA / WinKFP / EDIABAS)",
            ["SwitchedIsta"] = "gyári ISTA+ mód",
            ["IcomNote"] = "Az EDIABAS ICOM-ra van állítva: csatlakoztatott ICOM nélkül az INPA „NET-0009: TIMEOUT” hibát ad. Ha nincs ICOM a gépen, válts lent Offline-ra.\n\n",
            ["SwitchSuccess"] = "Sikeres váltás: {0}.\n\n{1}Az újonnan indított programok már az új beállítást látják, de a biztos működéshez (főleg az ISTA+ szolgáltatásai miatt) újraindítás ajánlott.\n\nÚjraindítod most a gépet?",
            ["ConfirmRestart"] = "Biztosan újraindítod a gépet? Előtte mentsd el a nyitott munkáidat!",
            ["ErrRestart"] = "Nem sikerült elindítani az újraindítást:\n\n{0}",
            ["ErrIniWrite"] = "Az EDIABAS.INI módosítása nem sikerült:\n\n{0}",
            ["ErrInterfaceLineMissing"] = "Az EDIABAS.INI [Configuration] részében nem található 'Interface' sor.",
            ["AboutText"] =
                "E-Series ⇄ ISTA+ Switch  v{0}\n\n" +
                "Ingyenes, nyílt forráskódú program (MIT licenc).\n{1}\n\n" +
                "Nem kapcsolódik a BMW AG-hez, és a BMW AG nem támogatja vagy hagyja jóvá. A BMW, MINI, ISTA, INPA, WinKFP, " +
                "NCS Expert és EDIABAS nevek a jogtulajdonosaik védjegyei.\n\n" +
                "Használata saját felelősségre történik. A program „ahogy van” alapon, mindennemű garancia nélkül érhető el. " +
                "A szerző semmilyen felelősséget nem vállal a járművekben, vezérlőegységekben, számítógépekben vagy adatokban " +
                "keletkező károkért.\n\n" +
                "Megnyitod a GitHub oldalt?",
        };
    }
}
