# TikkieTerug

Mobiele webapp/PWA voor amateurvoetbal: volg clubs en competities, bekijk programma's, uitslagen, standen, topscorers en wedstrijddetails.

De applicatie bestaat uit een Vue/Vite-frontend en een ASP.NET Core API. De API haalt actuele gegevens op bij Voetbal Nederland/Voetbal Oost en bewaart clubgegevens en logo's lokaal in SQLite en een schijfcache.

## Stack

- Frontend: Vue 3, Vite en Pinia
- Backend: ASP.NET Core Minimal API (.NET 10)
- Opslag: SQLite met Entity Framework Core
- Deployment: Docker Compose

## Lokaal ontwikkelen

Vereisten: .NET SDK 10 en Bun of Node.js.

Start de API in één terminal:

```bash
cd src/TikkieTerug.Api
dotnet run --launch-profile http
```

De API draait op `http://localhost:5244`.

Start de frontend in een tweede terminal:

```bash
cd src/TikkieTerug.Web
bun run dev -- --host 127.0.0.1
```

Open daarna `http://127.0.0.1:5173`. Vite proxyt `/api` en `/logos` naar de lokale API.

### Lokale data vullen

De API importeert clubs automatisch kort na het starten en daarna dagelijks. Handmatig importeren kan met:

```bash
curl -X POST http://localhost:5244/clubs/import
```

## Builds controleren

```bash
dotnet build TikkieTerug.slnx --no-restore

cd src/TikkieTerug.Web
bun run build
```

## Handmatig deployen op de NAS

De productie-installatie staat op de NAS in:

```text
/volume1/docker/tikkieterug
```

De container publiceert poort `8014`. SQLite-data en de logo-cache staan in het Docker-volume `app-data` en blijven behouden bij een gewone herbouw.

### Normale deployment

Gebruik na een push naar GitHub op de NAS:

```bash
cd /volume1/docker/tikkieterug

git fetch origin
git reset --hard origin/master
git log --oneline -1

docker-compose down
docker-compose build --no-cache
docker-compose up -d --force-recreate
```

Controleer de container en logs:

```bash
docker-compose ps
docker-compose logs --tail=50 api
```

De verwachte status is `Up` en de mapping is `8014:8080`.

### Na deployment controleren

```bash
curl http://localhost:8014/config.json
curl "http://localhost:8014/clubs?search=VSCO&active=true"
curl -I http://localhost:8014/logos/856
```

De clubzoekopdracht hoort `"logo":"/logos/<id>"` terug te geven. De logoroute hoort `200 OK` en `Content-Type: image/gif` terug te geven.

### Clubs opnieuw importeren

Voer dit uit wanneer een nieuw seizoen is gestart of clubs een oude competitie/stand tonen:

```bash
curl -X POST http://localhost:8014/clubs/import
```

De import vervangt de volledige lokale clubtabel met actuele gegevens. Wacht tot het commando een JSON-resultaat geeft voordat je controleert.

### Volledig schone installatie

Gebruik dit alleen wanneer ook SQLite-data en de persistent gecachete logo's verwijderd mogen worden:

```bash
cd /volume1/docker/tikkieterug
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d --force-recreate
```

> `docker-compose down -v` verwijdert het volume `app-data` en dus de database en logo-cache.

## Configuratie

`config.json` wordt in productie read-only gemount naar de frontend. Bij dezelfde frontend/API-host blijft de waarde leeg:

```json
{
  "apiUrl": ""
}
```

## PWA-icoon vernieuwen

Bij een icoonwijziging moeten naast de SVG's ook de PNG's in `src/TikkieTerug.Web/public/` worden vernieuwd:

```text
icon-192.png
icon-512.png
icon-maskable-192.png
icon-maskable-512.png
apple-touch-icon.png
```

Verhoog ook `CACHE_NAME` in `src/TikkieTerug.Web/public/sw.js`. Na deployment moeten gebruikers met een geïnstalleerde Android-PWA de app mogelijk verwijderen en opnieuw installeren voordat het homescreenicoon verandert.
