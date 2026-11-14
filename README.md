# RaceDay Part 3 — MVC Web Application

This implementation adds an ASP.NET Core MVC front end to the supplied Part 2 API. The MVC project makes all data requests through the REST API; it does not access the database. Azure Blob Storage is intentionally not used. Event banners and profile pictures are stored on the API host under `wwwroot`, using free local disk storage. **This is a functional alternative but does not satisfy a marking criterion that explicitly requires Azure Blob Storage.**

## Requirements
- .NET 8 SDK
- SQL Server instance configured for the Part 2 API
- Visual Studio 2022 or VS Code

## 1. Configure and run the API
Open `RaceDay.sln` and configure `RaceDay.Api/appsettings.json` connection string and `Registration:OrganiserCode`. Ensure API runs and Swagger opens. The default MVC API address is `http://localhost:5000/`; change `RaceDay.Mvc/appsettings.json` if your API uses another address (for example `https://localhost:7001/`). For local development, use the exact HTTP/HTTPS URL shown by the API launch profile.

## 2. Run MVC
From the repository root:

```bash
dotnet run --project RaceDay.Mvc
```

Open the URL printed by the MVC console. Register a Participant or Organiser. Organiser registration may require the API organiser code.

## 3. Local image storage
The MVC sends images to the API. The API writes profile images to `RaceDay.Api/wwwroot/profile-pictures` and event banners to `RaceDay.Api/wwwroot/event-banners`. No Azure account or paid storage is required. Keep these folders in persistent storage and back them up. The event banner endpoint is `POST /api/events/{id}/banner` (multipart field `file`); the Part 2 event DTO does not have a banner URL field, so the banner file is stored and directly addressable by `/event-banners/{eventId}.jpg|png|webp` but isn't currently rendered by the event detail view.

## Docker
A Dockerfile is provided for the MVC site. Set `Api__BaseUrl` to an API address reachable from the container. `localhost` inside a container means the container itself, not your host machine. With Docker Desktop on Windows/Mac, use `http://host.docker.internal:5000/` if the API runs on the host. Build and run:

```bash
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5000/ raceday-mvc
```

Open `http://localhost:8080`. The API and MVC should run as separate services. If containerising both, put them on the same Docker network and use the API service name as `Api__BaseUrl`.

## GitHub Actions
The workflow restores and builds both API and MVC. Add meaningful commits as you implement, test, and refine features; do not manufacture commits.

## Important compatibility note
This code targets .NET 8 and follows the routes and JSON contracts in the supplied Part 2 API. Run `dotnet build RaceDay.sln` and test against your database/API configuration. Local file storage requires a persistent volume when deployed in Docker. This starter adds event banner upload to the API; profile-picture upload uses the existing Part 2 endpoint. Review the assessment brief because replacing Azure with local storage may lose the Azure marks.
