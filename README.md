# KitsuneCommand

<p align="center">
  <strong>Web-based server management for 7 Days to Die</strong><br>
  Monitoring | Management | Map
</p>

---

KitsuneCommand is an open-source mod for 7 Days to Die dedicated servers that provides a RESTful API and a modern web management panel. Built as a clean-room V2 rewrite of [ServerKit](https://github.com/IceCoffee1024/7DaysToDie-ServerKit) with a modern Vue 3 frontend and improved security.

## Features

- **Web Dashboard** — Real-time server stats, player count, FPS, memory, game day/time
- **Player Management** — View online/offline players, inventories, skills, kick/ban
- **GPS Map** — Live map with player markers and region tracking
- **Web Console** — Execute server commands from your browser with real-time log streaming
- **Chat System** — View and search chat history, send messages
- **Points & Store** — In-game economy with sign-in rewards and a configurable shop
- **Teleportation** — Home, city, and friend teleport systems with point costs
- **Auto Backup** — Scheduled server backups with archive management
- **Task Scheduler** — Cron-based automated command execution
- **CD Keys & VIP Gifts** — Promo code and VIP reward systems
- **Colored Chat** — Custom name colors and chat formatting
- **Plugin System** — Extend functionality with custom plugin DLLs
- **13 Languages** — English, German, Spanish, French, Italian, Japanese, Korean, Polish, Portuguese, Russian, Turkish, Chinese (Simplified & Traditional)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | C# / .NET Framework 4.8 / OWIN / ASP.NET Web API 2 |
| Frontend | Vue 3 / TypeScript / Vite / PrimeVue 4 |
| Database | SQLite / Dapper |
| Real-time | WebSocketSharp |
| Auth | OAuth2 with BCrypt password hashing |
| Game Integration | Harmony runtime patching |
| DI | Autofac |

## Requirements

- 7 Days to Die Dedicated Server (V2.5+)
- .NET Framework 4.8 runtime (included with Windows)
- For building from source: .NET SDK or Visual Studio/Rider

## Installation

1. Download the latest release from [Releases](https://github.com/AdaInTheLab/KitsuneCommand/releases)
2. Extract the `KitsuneCommand` folder into your server's `Mods/` directory:
   ```
   7DaysToDieServer/
     Mods/
       KitsuneCommand/
         ModInfo.xml
         KitsuneCommand.dll
         Config/
         wwwroot/
         Plugins/
   ```
3. Start your dedicated server
4. Open `http://your-server-ip:8888` in a browser
5. On first run, check the server console for your auto-generated admin credentials

## Building from Source

### Frontend

```bash
cd frontend
npm install
npm run dev      # Development with hot reload (proxies API to :8888)
npm run build    # Production build (outputs to src/KitsuneCommand/wwwroot/)
```

### Backend

Requires game binary references in `src/KitsuneCommand/7dtd-binaries/`. Copy these from your 7D2D install's `7DaysToDie_Data/Managed/` folder:

- `Assembly-CSharp.dll`
- `Assembly-CSharp-firstpass.dll`
- `LogLibrary.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `0Harmony.dll`

Then build with Visual Studio, Rider, or the .NET CLI:

```bash
dotnet build src/KitsuneCommand/KitsuneCommand.csproj -c Release
```

### Full Build + Package

```powershell
.\tools\build.ps1
# Output: dist/KitsuneCommand/ (ready to copy to Mods/)
```

## Configuration

Settings are stored in `<SaveGameDir>/KitsuneCommand/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `WebUrl` | `http://*:8888` | HTTP server bind address |
| `WebSocketPort` | `8889` | WebSocket server port |
| `DatabasePath` | `KitsuneCommand.db` | SQLite database location |
| `AccessTokenExpireMinutes` | `1440` | Auth token lifetime (24h) |
| `EnableCors` | `false` | Enable for frontend dev with Vite |

## Console Commands

All commands use the `kc-` prefix:

| Command | Description |
|---------|-------------|
| `kc-gi` | Give items to a player |
| `kc-gm` | Send global message |
| `kc-pm` | Send private message |
| `kc-rs` | Restart server |

## Creating Plugins

Reference `KitsuneCommand.Abstractions.dll` and implement `IPlugin`:

```csharp
using KitsuneCommand.Abstractions;

public class MyPlugin : IPlugin
{
    public string Name => "MyPlugin";
    public string Version => "1.0.0";
    public string Author => "You";

    public void Initialize(PluginContext context)
    {
        context.EventBus.Subscribe<ChatMessageEvent>(msg =>
        {
            // React to chat messages
        });
    }

    public void Shutdown() { }
}
```

Place the compiled DLL in the `Plugins/` directory.

## Development Status

- [x] **Phase 1** — Foundation (project structure, auth, database, web server, Vue frontend)
- [ ] **Phase 2** — Core game integration (players, map, console)
- [ ] **Phase 3** — Chat & access control
- [ ] **Phase 4** — Points & game store
- [ ] **Phase 5** — Teleportation system
- [ ] **Phase 6** — Rewards & scheduling
- [ ] **Phase 7** — Customization & utilities
- [ ] **Phase 8** — Plugin system & hardening

## License

[MIT](LICENSE)

## Credits

- Original concept: [ServerKit](https://github.com/IceCoffee1024/7DaysToDie-ServerKit) by IceCoffee1024
- Logo: Created with Coda/Gemini
