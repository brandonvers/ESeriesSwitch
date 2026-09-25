<img src="Assets/icon_256.png" width="96" align="right" alt="">

# E-Series ⇄ ISTA+ Switch

Kis Windows-app, amely egy kattintással vált a régi E-szériás BMW kódoló toolok
(INPA, WinKFP, NCS Expert, Tool32) és a gyári AOS-ból letöltött ISTA+ között.

A régi toolokhoz a rendszer környezeti változói között szerepelnie kell az EDIABAS-nak,
az ISTA+ viszont csak akkor működik rendesen, ha nincs ott. Az app ezt állítja át.

## Mit állít

| Mód | `ediabas_config_dir` | `Path` |
|---|---|---|
| E-szériás toolok | `c:\ec-apps\ediabas\bin` | tartalmazza: `c:\ec-apps\ediabas\bin` |
| Gyári ISTA+ | nincs | nem tartalmazza |

- Rendszerszintű (HKLM) változókat módosít, ezért rendszergazdaként fut.
- Minden váltás előtt mentést készít ide: `C:\ProgramData\ESeriesSwitch\Backups\`
- A PATH többi bejegyzéséhez és a típusához (`REG_EXPAND_SZ`) nem nyúl.
- Váltás után újraindítást ajánl fel.

## Fordítás

- Visual Studio 2026, .NET 10, WPF
- Futtatás: F5 (a VS-t rendszergazdaként kell indítani, vagy engedélyezni az újraindítását)
- Egyetlen exe készítése: jobb klikk a projekten → **Publish…** → **FolderProfile** → **Publish**
  → `bin\publish\ESeriesSwitch.exe`
