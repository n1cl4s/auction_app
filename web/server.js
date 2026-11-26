// Visar hemsidan och skickar /api vidare till auktions-API:et.
// Allt får samma adress, så sessionskakan fungerar utan CORS-ändringar i API:et.
const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");

const port = Number(process.env.PORT || 5173);
const api = new URL(process.env.API_URL || "http://localhost:5000");
const root = path.join(__dirname, "public");
const imageRoot = path.join(__dirname, "..", "Database", "sample-images");
const types = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".jpg": "image/jpeg",
  ".png": "image/png",
};

function proxy(req, res) {
  const upstream = http.request(
    {
      hostname: api.hostname,
      port: api.port,
      path: req.url,
      method: req.method,
      headers: { ...req.headers, host: api.host },
      timeout: 8000,
    },
    (reply) => {
      res.writeHead(reply.statusCode, reply.headers);
      reply.pipe(res);
    },
  );
  upstream.on("timeout", () => upstream.destroy(new Error("timeout")));
  upstream.on("error", () => {
    if (res.headersSent) return res.end();
    res.writeHead(502, { "content-type": "application/json" });
    res.end(JSON.stringify({ message: "API unreachable" }));
  });
  req.pipe(upstream);
}

function serveFile(req, res) {
  let name;
  try {
    name = decodeURIComponent(req.url.split("?")[0]);
  } catch {
    res.writeHead(400);
    return res.end();
  }
  // Bilderna finns redan i repot och kopieras därför inte hit.
  const isImage = name.startsWith("/images/");
  const base = isImage ? imageRoot : root;
  const file = path.join(base, isImage ? name.slice("/images/".length) : name === "/" ? "index.html" : name);
  if (!file.startsWith(base + path.sep)) {
    res.writeHead(403);
    return res.end();
  }
  fs.readFile(file, (error, content) => {
    if (error) {
      res.writeHead(404);
      return res.end("Not found");
    }
    res.writeHead(200, { "content-type": types[path.extname(file)] || "application/octet-stream" });
    res.end(content);
  });
}

http
  .createServer((req, res) => (req.url.startsWith("/api/") ? proxy(req, res) : serveFile(req, res)))
  .listen(port, "127.0.0.1", () => console.log(`Hemsida: http://localhost:${port}  (API: ${api.origin})`));
