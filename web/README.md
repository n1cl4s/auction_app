# Veteranauktionen – hemsida till auktions-API:et

Ett tillägg utöver kursuppgiften. Kräver Node.js. Inga paket behöver installeras.

## Starta

1. Starta API:et i repots rot med `dotnet run` (adress `http://localhost:5000`).
2. Kör `node web/server.js` från repots rot.
3. Öppna <http://localhost:5173>.

Har API:et en annan adress sätts den i miljövariabeln `API_URL`.

`server.js` visar sidan och skickar `/api` vidare till API:et. Därför behövs varken CORS
eller ändringar av sessionskakan. Den är gjord för lokal utveckling, inte för driftsättning.

## Begränsningar i API:et som syns på sidan

- Startpris och köp nu-pris skickas inte i svaren, så sidan kan inte visa dem.
- Auktionslistan saknar pris, så sidan hämtar varje auktion för sig.
- Det går inte att köpa direkt, och API:et har inga kategorier.
- Sökning, sortering och ”Mina auktioner” görs i webbläsaren, inte i API:et.
- Bilder skickas inte. Sidan väljer en bild ur `Database/sample-images` utifrån auktionens namn.
- Budgivare visas bara med användar-id.
