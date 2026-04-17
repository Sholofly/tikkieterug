using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TikkieTerug.Api.Data;
using TikkieTerug.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=tikkieterug.db"));
builder.Services.AddHttpClient();

var app = builder.Build();

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TikkieTerug API");
    });
}

app.UseHttpsRedirection();

// Serve Vue frontend static files (in Docker, dist is copied to wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// POST /clubs/import — sync clubs from voetbalnederland.nl, enriched with first-team afdeling + speeldag
app.MapPost("/clubs/import", async (AppDbContext db, IHttpClientFactory httpFactory) =>
{
    var client = httpFactory.CreateClient();

    // Fetch CL.js + klassen.js + team_lijstabc A-Z all in parallel
    var clTask = client.GetStringAsync("https://voetbalnederland.nl/js/CL.js?20230922");
    var compTask = client.GetStringAsync("https://voetbalnederland.nl/js/klassen.js");

    var letters = Enumerable.Range('A', 26).Select(c => (char)c).ToArray();
    var letterTasks = letters.Select(letter =>
        client.PostAsync(
            "https://voetbalnederland.nl/SVC_Teams.asmx/team_lijstabc",
            new StringContent($"{{\"letter\":\"{letter}\"}}", Encoding.UTF8, "application/json"))
    ).ToArray();

    await Task.WhenAll(new Task[] { clTask, compTask }.Concat(letterTasks).ToArray());

    var clJs = clTask.Result;
    var compJs = compTask.Result;

    // 1. Parse clubs: CL[856] = new club(856, 'VSCO 61', 52.464, 5.898, 1);
    var clubRegex = new Regex(
        @"CL\[(\d+)\]\s*=\s*new\s+club\(\s*\d+\s*,\s*'([^']*)'\s*,\s*([0-9.]+)\s*,\s*([0-9.]+)\s*,\s*([01])\s*\)",
        RegexOptions.Compiled);

    var clubs = new Dictionary<int, Club>();
    foreach (Match match in clubRegex.Matches(clJs))
    {
        var id = int.Parse(match.Groups[1].Value);
        clubs[id] = new Club
        {
            Id = id,
            Name = match.Groups[2].Value.Trim(),
            Latitude = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
            Longitude = double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
            IsActive = match.Groups[5].Value == "1"
        };
    }

    // 2. Parse klassen.js: COMP[0] = new comp(202501001,'EreD',0,0);
    //    Build Dictionary<competitionId, afdelingIndex>
    var compRegex = new Regex(
        @"COMP\[(\d+)\]\s*=\s*new\s+comp\((\d+),'[^']*',(\d+),(\d+)\)",
        RegexOptions.Compiled);

    var afdelingByCompetition = new Dictionary<int, int>(); // competitionId → afdelingIndex
    foreach (Match match in compRegex.Matches(compJs))
    {
        var klasseId = int.Parse(match.Groups[2].Value);
        var afdelingIndex = int.Parse(match.Groups[3].Value);
        afdelingByCompetition.TryAdd(klasseId, afdelingIndex);
    }

    // 3. Parse team_lijstabc responses → all teams per club
    //    Row format: clubId;teamNr;speeldag;seizoen;competitionId;competitionName
    //    A club can have multiple entries (Zaterdag, Dames, Zondag etc.)
    var teamEntries = new List<(int ClubId, int TeamNr, int CompetitionId, string SpeeldagCode, string Speeldag)>();
    foreach (var letterTask in letterTasks)
    {
        var responseJson = await letterTask.Result.Content.ReadAsStringAsync();
        var d = JsonDocument.Parse(responseJson).RootElement.GetProperty("d").GetString();
        if (string.IsNullOrEmpty(d)) continue;

        foreach (var row in d.Split('#', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = row.Split(';');
            if (fields.Length < 5) continue;
            if (!int.TryParse(fields[0], out var clubId)) continue;
            if (!int.TryParse(fields[1], out var teamNr)) continue;
            if (!int.TryParse(fields[4], out var competitionId)) continue;

            var speeldagCode = fields[2];
            var speeldag = speeldagCode switch
            {
                "ZA" => "Zaterdag",
                "ZO" => "Zondag",
                "DA" => "Dames",
                _ => speeldagCode
            };

            teamEntries.Add((clubId, teamNr, competitionId, speeldagCode, speeldag));
        }
    }

    // 4. Group by clubId, first entry enriches base club, rest become derived entries
    var derivedTeams = new Dictionary<int, Club>(); // keyed by derived ID to deduplicate
    var enrichedBaseClubs = new HashSet<int>(); // track which clubs already got their base enrichment
    foreach (var entry in teamEntries)
    {
        if (!clubs.TryGetValue(entry.ClubId, out var parentClub)) continue;

        if (!enrichedBaseClubs.Contains(entry.ClubId))
        {
            // First entry for this club → enrich the base club
            enrichedBaseClubs.Add(entry.ClubId);
            parentClub.CompetitionId = entry.CompetitionId;
            parentClub.Speeldag = entry.Speeldag;
            parentClub.TeamNumber = entry.TeamNr;
            if (afdelingByCompetition.TryGetValue(entry.CompetitionId, out var afdelingIndex))
                parentClub.AfdelingId = afdelingIndex;
        }
        else
        {
            // Additional entry → create derived team (keyed by competitionId to deduplicate)
            var derivedId = entry.ClubId + entry.CompetitionId * 100;
            derivedTeams.TryAdd(derivedId, new Club
            {
                Id = derivedId,
                Name = $"{parentClub.Name} ({entry.Speeldag})",
                Latitude = parentClub.Latitude,
                Longitude = parentClub.Longitude,
                IsActive = parentClub.IsActive,
                CompetitionId = entry.CompetitionId,
                Speeldag = entry.Speeldag,
                ParentClubId = entry.ClubId,
                TeamNumber = entry.TeamNr,
                AfdelingId = afdelingByCompetition.TryGetValue(entry.CompetitionId, out var ai) ? ai : null
            });
        }
    }

    // 5. Upsert: remove existing, then add all (base clubs + derived teams)
    if (await db.Clubs.AnyAsync())
    {
        db.Clubs.RemoveRange(db.Clubs);
        await db.SaveChangesAsync();
    }

    var allEntries = clubs.Values.Concat(derivedTeams.Values).ToList();
    db.Clubs.AddRange(allEntries);
    await db.SaveChangesAsync();

    var enriched = allEntries.Count(c => c.CompetitionId.HasValue);
    return Results.Ok(new { imported = allEntries.Count, baseClubs = clubs.Count, derivedTeams = derivedTeams.Count, enriched });
})
.WithName("ImportClubs")
.WithDescription("Sync clubs vanuit voetbalnederland.nl, verrijkt met afdeling en speeldag van het eerste elftal");

// GET /clubs?search=vsco&active=true
app.MapGet("/clubs", async (AppDbContext db, IHttpClientFactory httpFactory, string? search, bool? active) =>
{
    var query = db.Clubs.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(c => EF.Functions.Like(c.Name, $"%{search}%"));

    if (active.HasValue)
        query = query.Where(c => c.IsActive == active.Value);

    var clubs = await query.OrderBy(c => c.Name).Take(50).ToListAsync();

    // Fetch competition names for unique competitionIds in parallel
    var client = httpFactory.CreateClient();
    var uniqueCompIds = clubs.Where(c => c.CompetitionId.HasValue)
        .Select(c => c.CompetitionId!.Value).Distinct().ToList();

    var compNames = new Dictionary<int, string>();
    if (uniqueCompIds.Count > 0)
    {
        var tasks = uniqueCompIds.Select(async compId =>
        {
            try
            {
                var resp = await client.PostAsync(
                    "https://voetbalnederland.nl/SVC_Klasse.asmx/klasse_naam",
                    new StringContent($"{{\"a\":\"{compId}\"}}", Encoding.UTF8, "application/json"));
                var json = await resp.Content.ReadAsStringAsync();
                var name = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();
                return (compId, name: name ?? "");
            }
            catch { return (compId, name: ""); }
        });
        foreach (var result in await Task.WhenAll(tasks))
            compNames[result.compId] = result.name;
    }

    return clubs.Select(c => new
    {
        c.Id,
        c.Name,
        c.Latitude,
        c.Longitude,
        c.IsActive,
        c.AfdelingId,
        c.Speeldag,
        c.CompetitionId,
        competitionName = c.CompetitionId.HasValue ? compNames.GetValueOrDefault(c.CompetitionId.Value) : (string?)null,
        logo = $"https://voetbalnederland.nl/l/{c.ParentClubId ?? c.Id}.gif"
    });
})
.WithName("SearchClubs")
.WithDescription("Zoek clubs op naam en/of actief status");

// GET /clubs/{id}
app.MapGet("/clubs/{id:int}", async (AppDbContext db, IHttpClientFactory httpFactory, int id) =>
{
    var club = await db.FindAsync<Club>(id);
    if (club is null) return Results.NotFound();

    string? competitionName = null;
    if (club.CompetitionId.HasValue)
    {
        try
        {
            var client = httpFactory.CreateClient();
            var resp = await client.PostAsync(
                "https://voetbalnederland.nl/SVC_Klasse.asmx/klasse_naam",
                new StringContent($"{{\"a\":\"{club.CompetitionId.Value}\"}}", Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            competitionName = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();
        }
        catch { }
    }

    return Results.Ok(new
    {
        club.Id,
        club.Name,
        club.Latitude,
        club.Longitude,
        club.IsActive,
        club.AfdelingId,
        club.Speeldag,
        club.CompetitionId,
        competitionName,
        logo = $"https://voetbalnederland.nl/l/{club.ParentClubId ?? club.Id}.gif"
    });
})
.WithName("GetClub");

// GET /competitions/{id}/uitslagen — all match results in a competition, grouped by date
app.MapGet("/competitions/{id:int}/uitslagen", async (AppDbContext db, IHttpClientFactory httpFactory, int id) =>
{
    var client = httpFactory.CreateClient();
    var response = await client.PostAsync(
        "https://voetbalnederland.nl/SVC_Klasse.asmx/klasse_uitslagen1",
        new StringContent($"{{\"a\":\"{id}\"}}", Encoding.UTF8, "application/json"));

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();

    if (string.IsNullOrEmpty(data)) return Results.Ok(Array.Empty<object>());

    var rows = data.Split('#', StringSplitOptions.RemoveEmptyEntries);
    var today = DateOnly.FromDateTime(DateTime.Today);

    // Collect all club IDs for name lookup
    var clubIds = rows.SelectMany(r =>
    {
        var f = r.Split(';');
        return new[] { int.Parse(f[0]), int.Parse(f[1]) };
    }).Distinct().ToArray();

    var clubNames = await db.Clubs
        .Where(c => clubIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id, c => c.Name);

    // Field mapping from gameline.js:
    // [0]=homeId [1]=awayId [2]=homeScore [3]=awayScore [4]=gespeeld [5]=dayOffset
    // [6]=status [7]=homeRed [8]=awayRed [9]=hour [10]=minute [11]=homeReport
    // [12]=awayReport [13]=compId [14]=soort [15]=cixVoor [16]=cixTegen [17]=matchId
    // Status: 0=niet gestart, 1=bezig, 2=rust, 3=afgelopen, 4=gestaakt, 5=afgelast
    // Note: if gespeeld==1, status is forced to 3 (afgelopen)
    var grouped = rows
        .Select(r =>
        {
            var f = r.Split(';');
            var homeId = int.Parse(f[0]);
            var awayId = int.Parse(f[1]);
            var dayOffset = int.Parse(f[5]);
            var matchDate = today.AddDays(dayOffset);
            var gespeeld = int.Parse(f[4]);
            var statusCode = gespeeld == 1 ? 3 : int.Parse(f[6]);
            var status = statusCode switch
            {
                0 => "scheduled",
                1 => "live",
                2 => "halftime",
                3 => "ended",
                4 => "suspended",
                5 => "cancelled",
                _ => $"unknown ({statusCode})"
            };
            return new
            {
                date = matchDate.ToString("yyyy-MM-dd"),
                homeClubId = homeId,
                homeClub = clubNames.GetValueOrDefault(homeId, "Onbekend"),
                homeLogo = $"https://voetbalnederland.nl/l/{homeId}.gif",
                awayClubId = awayId,
                awayClub = clubNames.GetValueOrDefault(awayId, "Onbekend"),
                awayLogo = $"https://voetbalnederland.nl/l/{awayId}.gif",
                homeScore = int.Parse(f[2]),
                awayScore = int.Parse(f[3]),
                status,
                time = $"{f[9]}:{f[10].PadLeft(2, '0')}",
                matchId = int.Parse(f[17])
            };
        })
        .GroupBy(m => m.date)
        .OrderByDescending(g => g.Key)
        .Select(g => new
        {
            date = g.Key,
            matches = g.Select(m => new
            {
                m.homeClubId,
                m.homeClub,
                homeLogo = $"https://voetbalnederland.nl/l/{m.homeClubId}.gif",
                m.awayClubId,
                m.awayClub,
                awayLogo = $"https://voetbalnederland.nl/l/{m.awayClubId}.gif",
                m.homeScore,
                m.awayScore,
                m.status,
                m.time,
                m.matchId
            })
        });

    return Results.Ok(grouped);
})
.WithName("GetCompetitionUitslagen")
.WithDescription("Alle uitslagen in een competitie, gegroepeerd op datum");

// GET /competitions/{id}/naam — competition name
app.MapGet("/competitions/{id:int}/naam", async (IHttpClientFactory httpFactory, int id) =>
{
    var client = httpFactory.CreateClient();
    var response = await client.PostAsync(
        "https://voetbalnederland.nl/SVC_Klasse.asmx/klasse_naam",
        new StringContent($"{{\"a\":\"{id}\"}}", Encoding.UTF8, "application/json"));

    var json = await response.Content.ReadAsStringAsync();
    var name = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();

    return string.IsNullOrEmpty(name) ? Results.NotFound() : Results.Ok(new { id, name });
})
.WithName("GetCompetitionName")
.WithDescription("Naam van een competitie");

// GET /competitions/{id}/stand — current standings of a competition, including period title won
app.MapGet("/competitions/{id:int}/stand", async (IHttpClientFactory httpFactory, int id) =>
{
    var client = httpFactory.CreateClient();
    var response = await client.PostAsync(
        "https://voetbalnederland.nl/SVC_Ranglijst.asmx/ranglijst_klasse",
        new StringContent($"{{\"k\":\"{id}\"}}", Encoding.UTF8, "application/json"));

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();

    if (string.IsNullOrEmpty(data)) return Results.Ok(Array.Empty<object>());

    var rows = data.Split('#', StringSplitOptions.RemoveEmptyEntries);

    // Field mapping:
    // [0]=position [1]=logo (skip) [2]=teamName [3]=? (skip)
    // [4]=played [5]=won [6]=drawn [7]=lost [8]=points
    // [9]=goalsFor [10]=goalsAgainst [11]=penaltyPoints (empty string → 0)
    // [12]=periodWon (0=none, after the @@ double separator)
    // [13]-[16]=skip [17]=clubId [18]=speeldag (skip) [19]=season (skip)
    var standings = rows
        .Select(r =>
        {
            var f = r.Split('@');
            var goalsFor = int.Parse(f[9]);
            var goalsAgainst = int.Parse(f[10]);
            var penaltyPoints = int.TryParse(f[11], out var pp) ? pp : 0;
            var periodWon = int.TryParse(f[12], out var pw) ? pw : 0;
            return new
            {
                position = int.Parse(f[0]),
                clubId = int.Parse(f[17]),
                club = f[2],
                logo = $"https://voetbalnederland.nl/l/{int.Parse(f[17])}.gif",
                played = int.Parse(f[4]),
                won = int.Parse(f[5]),
                drawn = int.Parse(f[6]),
                lost = int.Parse(f[7]),
                goalsFor,
                goalsAgainst,
                goalDifference = goalsFor - goalsAgainst,
                points = int.Parse(f[8]),
                penaltyPoints,
                periodWon
            };
        })
        .OrderBy(s => s.position);

    return Results.Ok(standings);
})
.WithName("GetCompetitionStand")
.WithDescription("Huidige stand van een competitie, inclusief gewonnen periodetitel");

// GET /competitions/{id}/periodestand — standings per period
app.MapGet("/competitions/{id:int}/periodestand", async (IHttpClientFactory httpFactory, int id) =>
{
    var client = httpFactory.CreateClient();
    var response = await client.PostAsync(
        "https://voetbalnederland.nl/SVC_Ranglijst.asmx/periodestand",
        new StringContent($"{{\"k\":\"{id}\"}}", Encoding.UTF8, "application/json"));

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonDocument.Parse(json).RootElement.GetProperty("d").GetString();

    if (string.IsNullOrEmpty(data)) return Results.Ok(Array.Empty<object>());

    var rows = data.Split('#', StringSplitOptions.RemoveEmptyEntries);

    // periodestand fields: [0]=position [1]=logo [2]=team [3]=clubId
    // [4]=played [5]=won [6]=drawn [7]=lost [8]=points [9]=goalsFor [10]=goalsAgainst [11]=period
    var periods = rows
        .Select(r =>
        {
            var f = r.Split('@');
            return new
            {
                position = int.Parse(f[0]),
                clubId = int.Parse(f[3]),
                club = f[2],
                logo = $"https://voetbalnederland.nl/l/{int.Parse(f[3])}.gif",
                played = int.Parse(f[4]),
                won = int.Parse(f[5]),
                drawn = int.Parse(f[6]),
                lost = int.Parse(f[7]),
                points = int.Parse(f[8]),
                goalsFor = int.Parse(f[9]),
                goalsAgainst = int.Parse(f[10]),
                period = int.Parse(f[11])
            };
        })
        .GroupBy(p => p.period)
        .OrderBy(g => g.Key)
        .Select(g => new
        {
            period = g.Key,
            standings = g.OrderBy(p => p.position).Select(p => new
            {
                p.position,
                p.clubId,
                p.club,
                p.logo,
                p.played,
                p.won,
                p.drawn,
                p.lost,
                p.points,
                p.goalsFor,
                p.goalsAgainst
            })
        });

    return Results.Ok(periods);
})
.WithName("GetCompetitionPeriodestand")
.WithDescription("Stand per periode van een competitie");

// GET /matches/{id} — match details with scorers
app.MapGet("/matches/{id:long}", async (AppDbContext db, IHttpClientFactory httpFactory, long id) =>
{
    var client = httpFactory.CreateClient();

    // Fetch match data and scorers in parallel
    var matchTask = client.PostAsync(
        "https://voetbalnederland.nl/SVC_Uitslagen.asmx/wedstrijd_data",
        new StringContent($"{{\"w\":\"{id}\",\"s\":\"1\"}}", Encoding.UTF8, "application/json"));
    var scrTask = client.PostAsync(
        "https://voetbalnederland.nl/SVC_Verslagen.asmx/scr",
        new StringContent($"{{\"s\":\"1\",\"w\":\"{id}\"}}", Encoding.UTF8, "application/json"));

    await Task.WhenAll(matchTask, scrTask);

    // Parse match data
    // Fields: [0]=homeId [1]=awayId [2]=homeScore [3]=awayScore [4]=gespeeld(True/False)
    // [5]=dayOffset [6]=status [7]=homeRed [8]=awayRed [9]=hour [10]=minute
    // [11]=homeReport [12]=awayReport [13]=compId [14]=soort [15]=cixVoor [16]=cixTegen [17]=matchId
    var matchJson = await matchTask.Result.Content.ReadAsStringAsync();
    var matchData = JsonDocument.Parse(matchJson).RootElement.GetProperty("d").GetString();

    if (string.IsNullOrEmpty(matchData)) return Results.NotFound();

    var f = matchData.TrimEnd('#').Split(';');
    var homeId = int.Parse(f[0]);
    var awayId = int.Parse(f[1]);
    var today = DateOnly.FromDateTime(DateTime.Today);
    var dayOffset = int.Parse(f[5]);
    var gespeeld = f[4] == "True" || f[4] == "1";
    var statusCode = gespeeld ? 3 : int.Parse(f[6]);
    var status = statusCode switch
    {
        0 => "scheduled",
        1 => "live",
        2 => "halftime",
        3 => "ended",
        4 => "suspended",
        5 => "cancelled",
        _ => $"unknown ({statusCode})"
    };

    // Enrich club names from DB
    var clubIds = new[] { homeId, awayId };
    var clubNames = await db.Clubs
        .Where(c => clubIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id, c => c.Name);

    // Parse scorers
    // Format: [runningScore];[minute];[playerId];[playerName];[side]#...@motm;[motmPlayerId]#
    // side: 0 = home, 5 = away
    var scorers = new List<object>();
    int? manOfTheMatchId = null;

    var scrJson = await scrTask.Result.Content.ReadAsStringAsync();
    var scrData = JsonDocument.Parse(scrJson).RootElement.GetProperty("d").GetString();

    if (!string.IsNullOrEmpty(scrData))
    {
        var parts = scrData.Split('@');
        var goalsSection = parts[0];

        // Parse man of the match from "motm;[playerId]#"
        if (parts.Length > 1)
        {
            var motmPart = parts[1].TrimEnd('#').Replace("motm;", "");
            if (int.TryParse(motmPart, out var motmId) && motmId > 0)
                manOfTheMatchId = motmId;
        }

        // Parse individual goals
        foreach (var entry in goalsSection.Split('#', StringSplitOptions.RemoveEmptyEntries))
        {
            var g = entry.Split(';');
            if (g.Length < 5) continue;
            var side = g[4] == "0" ? "home" : "away";
            scorers.Add(new
            {
                minute = int.Parse(g[1]),
                playerId = int.Parse(g[2]),
                player = g[3].Trim(),
                side,
                teamScore = int.Parse(g[0])
            });
        }

        // Sort chronologically
        scorers = scorers
            .OrderBy(s => ((dynamic)s).minute)
            .ThenBy(s => ((dynamic)s).teamScore)
            .ToList();
    }

    var result = new
    {
        matchId = id,
        date = today.AddDays(dayOffset).ToString("yyyy-MM-dd"),
        time = $"{f[9]}:{f[10].PadLeft(2, '0')}",
        status,
        competitionId = int.Parse(f[13]),
        homeClubId = homeId,
        homeClub = clubNames.GetValueOrDefault(homeId, "Onbekend"),
        homeLogo = $"https://voetbalnederland.nl/l/{homeId}.gif",
        awayClubId = awayId,
        awayClub = clubNames.GetValueOrDefault(awayId, "Onbekend"),
        awayLogo = $"https://voetbalnederland.nl/l/{awayId}.gif",
        homeScore = int.Parse(f[2]),
        awayScore = int.Parse(f[3]),
        homeRedCards = int.Parse(f[7]),
        awayRedCards = int.Parse(f[8]),
        manOfTheMatchId,
        goals = scorers
    };

    return Results.Ok(result);
})
.WithName("GetMatch")
.WithDescription("Wedstrijddetails inclusief doelpuntenmakers");

// SPA fallback: serve index.html for unmatched routes (Vue Router handles client-side routing)
app.MapFallbackToFile("index.html");

app.Run();
