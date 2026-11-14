# Programming POE Part 3

## 1. Project Overview

RaceDay is a race event management system. It supports race organisers
who create and manage events, and participants who browse events, enrol
in race categories, and view their results.

Part 3 adds an **ASP.NET Core MVC web application** (`RaceDay.Mvc`) on
top of the Part 2 REST API (`RaceDay.Api`). The MVC site is the user
interface. It never touches the database. Every piece of data is
requested from the API over HTTP. The solution also includes automated
tests, a Dockerfile for the MVC site, and a GitHub Actions workflow that
builds and tests every push.

## 2. Main Features

-   Register and log in as an **Organiser** or **Participant**.
-   Log out and manage an authenticated session.
-   Browse upcoming events and filter them by location and event type.
-   View event details and the categories available.
-   Participants can enrol in an event category, view their enrolments
    and view their own race results.
-   Participants can update their profile and **upload a profile
    picture**.
-   Organisers can create, edit and delete their events, and **upload an
    event banner image**.
-   Organisers can add categories to their events.
-   Organisers can view enrolments for their events and capture race
    results.
-   Role-based access: Participant pages and Organiser pages are
    restricted to the matching role.
-   Automated tests for the API behaviour and access permissions.

## 3. Technologies Used

-   C#
-   ASP.NET Core MVC (.NET 8)
-   ASP.NET Core Web API (.NET 8)
-   Entity Framework Core 8
-   Microsoft SQL Server / LocalDB
-   Swagger / OpenAPI
-   xUnit and ASP.NET Core integration testing
-   Docker
-   GitHub Actions
-   Visual Studio
-   GitHub

## 4. Project Structure

``` text
RaceDay-Part3/
├── RaceDay.Api/
│   ├── Controllers/       # API endpoints (auth, users, events, categories,
│   │                      # enrolments, results)
│   ├── Data/              # Entity Framework database context
│   ├── Dtos/              # Request and response objects
│   ├── Models/            # Database entity models
│   ├── Security/          # Password hashing, session authorisation, helpers
│   ├── Storage/           # UploadPaths: where uploaded images are stored
│   ├── wwwroot/           # Static files
│   ├── Program.cs         # Application setup and middleware
│   ├── appsettings.json   # Connection string and settings
│   └── RaceDay.Api.csproj
├── RaceDay.Mvc/
│   ├── Controllers/       # Account, Home, Participant and Organiser controllers
│   ├── Models/            # View models and API data models
│   ├── Services/          # RaceDayApiClient (all calls to the API)
│   ├── Views/             # Razor views
│   ├── wwwroot/           # CSS and static files
│   ├── Dockerfile
│   ├── Program.cs
│   └── appsettings.json   # Api:BaseUrl setting
├── RaceDay.Tests/
│   ├── AuthTests.cs
│   ├── EnrolmentTests.cs
│   ├── EventTests.cs
│   ├── ResultTests.cs
│   ├── RoleEnforcementTests.cs
│   └── TestSupport.cs
├── .github/workflows/build-and-test.yml
├── start-raceday.bat      # Starts the API and MVC site together (Windows)
├── RaceDay.sln
└── README.md
```

## 5. Requirements

Before running the project, make sure you have:

1.  Visual Studio 2022 or a compatible IDE with the **.NET 8 SDK**
    installed.
2.  Microsoft SQL Server LocalDB installed, or access to a SQL Server
    instance.
3.  NuGet package restore enabled.
4.  (Optional) Docker Desktop, to run the MVC site in a container.

## 6. Database Configuration

The API uses Entity Framework Core with SQL Server. The connection
string is in `RaceDay.Api/appsettings.json` and by default uses LocalDB
with a database named `RaceDayDb`. If you use a different SQL Server
instance, change the `ConnectionStrings:RaceDayDb` setting.

On startup the API checks the database and creates it from the Entity
Framework model if needed. The event types **Run**, **Walk** and
**Cycle** are created as initial data.

### Organiser registration code

Organiser registration checks the `Registration:OrganiserCode` setting
in `RaceDay.Api/appsettings.json`. Configure this for your own
environment and do not publish real secrets in a public repository.

## 7. How to Run the Application

The MVC site needs the API to be running, so **start the API first, then
the MVC site**.

### Option A: one click (Windows)

Double-click `start-raceday.bat`. It opens a window for the API, then a
window for the MVC site, then opens your browser. Leave both windows
open while you use the site.

### Option B: from a terminal

Open two terminals in the folder containing `RaceDay.sln`.

Terminal 1, the API:

``` bash
dotnet run --project RaceDay.Api --launch-profile http
```

Terminal 2, the MVC site:

``` bash
dotnet run --project RaceDay.Mvc
```

### Option C: Visual Studio

1.  Right-click the solution and choose **Configure Startup Projects**.
2.  Select **Multiple startup projects** and set both `RaceDay.Api` and
    `RaceDay.Mvc` to **Start**.
3.  Run with **Ctrl+F5** (Start Without Debugging).

### Addresses

| Application  | Address                                        |
|--------------|------------------------------------------------|
| API          | `http://localhost:5080` (Swagger at `/swagger`) |
| MVC site     | `http://localhost:62579`                       |

The MVC site finds the API through the `Api:BaseUrl` setting in
`RaceDay.Mvc/appsettings.json`, which is `http://localhost:5080/` by
default. Change it if your API runs on a different address.

### Using the site

1.  Open the MVC site and click **Register**.
2.  Create a Participant account, or an Organiser account using the
    organiser registration code.
3.  Log in. Participants are taken to the events list and Organisers to
    their events dashboard.

## 8. Image Uploads

Profile pictures and event banners are uploaded through the MVC site,
which forwards them to the API as multipart form data.

| Image            | Endpoint                          | Limit | Allowed types      |
|------------------|-----------------------------------|-------|--------------------|
| Profile picture  | `PUT /api/users/me/profile-picture` | 2 MB  | jpg, png, gif, webp |
| Event banner     | `POST /api/events/{id}/banner`    | 5 MB  | jpg, png, webp     |

Only the owning organiser can upload a banner for their event, and a
user can only upload their own profile picture.

### Where images are stored

Images are stored on the API host's local disk, **outside the project
folder**, in:

`%LocalAppData%\RaceDay\uploads\profile-pictures`
`%LocalAppData%\RaceDay\uploads\event-banners`

They are outside the project folder on purpose. When an app writes
files inside its own project directory, Visual Studio Hot Reload and
`dotnet watch` detect a file change and can restart or stop the running
application, which made uploads look like crashes. The location is
managed by the `UploadPaths` class and can be changed with the
`Uploads:Root` setting in `RaceDay.Api/appsettings.json`.

The API serves the images back as static files at
`/profile-pictures/{userId}.ext` and `/event-banners/{eventId}.ext`.

**Note:** This project uses local disk storage and does not use Azure
Blob Storage. Local storage is free and works without an Azure account,
but when running in Docker it needs a persistent volume so images are
not lost.

## 9. Authentication and Security

-   Passwords are hashed before being stored; the plain-text password is
    never returned by the API.
-   Login creates a server-side session that stores the user's ID and
    role. Logout clears it.
-   The MVC site keeps the user's role in its own session and
    redirects users away from pages for the wrong role.
-   The API independently checks the session and role on every
    protected endpoint, so access is enforced even if the MVC site is
    bypassed.
-   Organiser registration uses a configurable registration code.
-   Uploads are checked for size, file extension and content type.

Sessions are held in memory, so restarting the API logs everyone out.
For deployment beyond local development, review the connection string,
registration code, HTTPS and session settings.

## 10. Running the Automated Tests

The `RaceDay.Tests` project uses xUnit and ASP.NET Core integration
testing against an in-memory database, so the tests do not need SQL
Server.

In Visual Studio: build the solution, open **Test > Test Explorer** and
select **Run All Tests**.

Or from a terminal in the folder containing `RaceDay.sln`:

``` bash
dotnet test
```

The tests cover authentication, event operations, categories,
enrolments, results and role-based access restrictions.

## 11. Docker

A Dockerfile is provided for the MVC site. The API must be running
separately. Set `Api__BaseUrl` to an address the container can reach.
`localhost` inside a container means the container itself, so with
Docker Desktop use `host.docker.internal`.

``` bash
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5080/ raceday-mvc
```

Then open `http://localhost:8080`.

## 12. GitHub Actions (CI)

The workflow in `.github/workflows/build-and-test.yml` runs on every
push and pull request. It checks out the code, installs .NET 8, restores
packages, builds the solution in Release mode and runs all the tests. A
green tick on a commit means the build and every test passed.

## 13. Part 3 Deliverables

-   ASP.NET Core MVC front end that uses the REST API for all data.
-   Participant and Organiser pages with role-based access.
-   Profile picture and event banner uploads.
-   ASP.NET Core REST API with EF Core and SQL Server.
-   Automated xUnit tests.
-   Dockerfile for the MVC site.
-   GitHub Actions build-and-test workflow.


## CI/CD WORKFLOW:
<img width="1518" height="440" alt="image" src="https://github.com/user-attachments/assets/5f38ba75-940a-4d00-bf7f-3ef881f7391c" />



## 14. Author

**Mmabatho Vilakazi**\
**RaceDay --- Programming POE Part 3**
