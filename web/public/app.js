// Hemsidan visar bara data och skickar anrop. Alla auktionsregler avgörs av API:et.
const view = document.getElementById("view");
const nav = document.getElementById("nav");
let me = null;
let renderId = 0;
let listFilter = { query: "", sort: "end" };

// API:et skickar inga bilder, så de matchas mot auktionens namn.
const images = [
  ["mercedes", "mercedes-benz-w128.png"], ["desoto", "desoto-firesweep.jpg"], ["buick", "buick-special.jpg"],
  ["pontiac", "pontiac-streamliner.jpg"], ["cadillac", "cadillac-series-62.jpg"], ["dodge", "dodge.jpg"],
  ["plymouth", "plymouth-fury.jpg"], ["chevrolet", "chevrolet-impala-sport.jpg"],
  ["thunderbird", "ford-thunderbird.jpg"], ["morris", "morris-minor.jpg"], ["ferrari", "ferrari-812-superfast.png"],
];

const translations = {
  "Email and password are required.": "Fyll i e-post och lösenord.",
  "A valid email and password (1–128 characters) are required.": "Ange en giltig e-postadress och ett lösenord (högst 128 tecken).",
  "Email is already registered.": "E-postadressen är redan registrerad.",
  "Name (1–200 characters) and a future endTime are required.": "Ange ett namn (1–200 tecken) och en sluttid i framtiden.",
  "Starting price must be at least 1000 kr and whole kronor.": "Startpriset måste vara minst 1 000 kr i hela kronor.",
  "First bid must be at least the start price.": "Första budet måste vara minst startpriset.",
  "You cannot bid on your own auction.": "Du kan inte buda på din egen auktion.",
  "Auction has ended": "Auktionen är avslutad.",
  "Amount must be in whole kronor": "Budet måste vara i hela kronor.",
  "Amount must be greater than zero.": "Budet måste vara större än noll.",
  "Bid must be at least 1000 kr higher than the highest bid.": "Budet måste vara minst 1 000 kr högre än högsta budet.",
};

// Bygger element utan innerHTML, så att text från API:et aldrig tolkas som HTML.
function el(tag, props = {}, ...children) {
  const node = Object.assign(document.createElement(tag), props);
  node.append(...children.filter((child) => child !== null && child !== false));
  return node;
}

async function api(method, url, body) {
  let response;
  try {
    response = await fetch(url, {
      method,
      headers: body ? { "content-type": "application/json" } : {},
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw Object.assign(new Error("Kunde inte nå servern."), { status: 0 });
  }
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const fallback = {
      401: "Du behöver logga in.",
      403: "Du har inte behörighet att göra det.",
      404: "Hittades inte.",
      502: "Auktions-API:et svarar inte. Kontrollera att det är igång.",
    }[response.status] || `Något gick fel (statuskod ${response.status}).`;
    throw Object.assign(new Error(translations[data?.message] || data?.message === "API unreachable" && fallback || data?.message || fallback), { status: response.status });
  }
  return data;
}

const kronor = (amount) => `${Number(amount).toLocaleString("sv-SE")} kr`;
const dateTime = (value) => new Date(value).toLocaleString("sv-SE", { dateStyle: "medium", timeStyle: "short" });

function timeLeft(endTime) {
  const seconds = Math.floor((new Date(endTime) - Date.now()) / 1000);
  if (seconds <= 0) return "Avslutad";
  const days = Math.floor(seconds / 86400);
  const hours = Math.floor((seconds % 86400) / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  if (days > 0) return `${days} d ${hours} h`;
  if (hours > 0) return `${hours} h ${minutes} min`;
  return `${minutes} min ${seconds % 60} s`;
}
const isUrgent = (endTime) => new Date(endTime) - Date.now() < 86400000;
const countdown = (endTime) => {
  const node = el("span", { className: isUrgent(endTime) ? "value urgent" : "value", textContent: timeLeft(endTime) });
  node.dataset.end = endTime;
  return node;
};

const imageFor = (name) => images.find(([key]) => name.toLowerCase().includes(key))?.[1];

function photo(name) {
  const file = imageFor(name);
  return file
    ? el("img", { className: "photo", src: `/images/${file}`, alt: name, loading: "lazy" })
    : el("div", { className: "photo", textContent: "Bild saknas" });
}

let toastTimer;
function toast(message) {
  const box = document.getElementById("toast");
  box.textContent = message;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (box.textContent = ""), 4000);
}

function field(text, props) {
  return el("label", {}, text, el("input", { required: true, ...props }));
}

// Gemensam formulärhantering: spärrar dubbelklick och visar API:ets felmeddelande.
function form(fields, buttonText, onSubmit) {
  const error = el("p", { className: "error", role: "alert" });
  const button = el("button", { type: "submit", textContent: buttonText });
  const node = el("form", {}, ...fields, error, button);
  node.addEventListener("submit", async (event) => {
    event.preventDefault();
    error.textContent = "";
    button.disabled = true;
    try {
      await onSubmit(Object.fromEntries(new FormData(node)), event.submitter);
    } catch (problem) {
      error.textContent = problem.message;
      if (problem.status === 401 && me) { me = null; drawNav(); }
    } finally {
      button.disabled = false;
    }
  });
  return node;
}

function drawNav() {
  if (!me) {
    nav.replaceChildren(el("a", { href: "#/logga-in", textContent: "Logga in" }), el("a", { href: "#/registrera", textContent: "Skapa konto" }));
    return;
  }
  const logout = el("button", { type: "button", textContent: "Logga ut", title: `Inloggad som ${me.email}` });
  logout.addEventListener("click", async () => {
    try {
      await api("DELETE", "/api/auth/login");
      me = null;
      drawNav();
      toast("Du är utloggad.");
      render();
    } catch (problem) {
      toast(problem.message);
    }
  });
  nav.replaceChildren(el("a", { href: "#/mina", textContent: "Mina auktioner" }), logout);
}

// Listan saknar pris, så varje auktion hämtas även för sig.
async function loadAuctions() {
  const auctions = await api("GET", "/api/auctions");
  return Promise.all(auctions.map((a) => api("GET", `/api/auctions/${a.id}`)));
}

const soonestFirst = (a, b) => new Date(a.endTime) - new Date(b.endTime);
const latestFirst = (a, b) => new Date(b.endTime) - new Date(a.endTime);
const priceText = (a) => (a.currentPrice === null ? "Inga bud" : kronor(a.currentPrice));
const priceLabel = (a) => (a.ended ? "Slutbud" : "Högsta bud");

function stat(label, value) {
  return el("div", {}, el("span", { className: "label", textContent: label }),
    typeof value === "string" ? el("span", { className: "value", textContent: value }) : value);
}

const card = (a) => el("li", { className: "card" }, el("a", { href: `#/auktion/${a.id}` },
  el("div", { className: "frame" }, photo(a.name)),
  el("h3", { textContent: a.name }),
  el("div", { className: "row" },
    stat(priceLabel(a), priceText(a)),
    a.ended ? stat("Slutade", dateTime(a.endTime)) : stat("Tid kvar", countdown(a.endTime)))));
const cards = (items) => el("ul", { className: "grid" }, ...items.map(card));

function hero(a) {
  const open = el("a", { className: "button", href: `#/auktion/${a.id}`, textContent: "Visa och buda" });
  return el("section", { className: "hero" },
    el("img", { src: `/images/${imageFor(a.name)}`, alt: a.name }),
    el("div", { className: "wrap" },
      el("h1", { textContent: a.name }),
      el("div", { className: "bar" },
        stat("Högsta bud", priceText(a)),
        stat("Antal bud", String(a.bids.length)),
        stat("Tid kvar", countdown(a.endTime)),
        open)));
}

// De tre auktioner som slutar först. En skarp bild får den stora rutan om någon av dem har en.
function endingSoon(items) {
  const big = items.find((a) => imageFor(a.name)?.endsWith(".png")) ?? items[0];
  const ordered = [big, ...items.filter((a) => a !== big)];
  return el("ul", { className: "feature" }, ...ordered.map((a) => el("li", {}, el("a", { href: `#/auktion/${a.id}` },
    photo(a.name),
    el("div", { className: "chip" }, countdown(a.endTime)),
    el("div", { className: "caption" },
      el("h3", { textContent: a.name }),
      stat(priceLabel(a), priceText(a)))))));
}

// Sökning och sortering görs bara i webbläsaren. API:et har inte de funktionerna.
function searchableList(title, active) {
  const compare = {
    end: soonestFirst,
    high: (a, b) => (b.currentPrice ?? -1) - (a.currentPrice ?? -1),
    bids: (a, b) => b.bids.length - a.bids.length,
  };
  const search = el("input", { type: "search", placeholder: "Sök bil", value: listFilter.query, ariaLabel: "Sök bland pågående auktioner" });
  const results = el("div");
  const tabs = [["end", "Slutar först"], ["high", "Högsta bud"], ["bids", "Flest bud"]].map(([key, text]) => {
    const button = el("button", { type: "button", className: "tab", textContent: text });
    button.dataset.sort = key;
    button.addEventListener("click", () => { listFilter.sort = key; showResults(); });
    return button;
  });
  const showResults = () => {
    listFilter.query = search.value;
    for (const tab of tabs) tab.setAttribute("aria-pressed", String(tab.dataset.sort === listFilter.sort));
    const query = search.value.trim().toLowerCase();
    const shown = active.filter((a) => a.name.toLowerCase().includes(query)).sort(compare[listFilter.sort]);
    results.replaceChildren(shown.length ? cards(shown)
      : el("p", { className: "muted", textContent: query ? "Ingen pågående auktion matchar sökningen." : "Det finns inga pågående auktioner just nu." }));
  };
  search.addEventListener("input", showResults);
  showResults();
  return [
    el("div", { className: "list-head" }, el("h2", { textContent: title }),
      el("div", { className: "tools" }, el("div", { className: "tabs", role: "group", ariaLabel: "Sortera" }, ...tabs), search)),
    results,
  ];
}

async function listPage() {
  const active = (await loadAuctions()).filter((a) => !a.ended).sort(soonestFirst);
  // Bara de två stora bilderna är skarpa nog att visas i helbredd.
  const featured = active.find((a) => imageFor(a.name)?.endsWith(".png"));
  const soon = active.filter((a) => a !== featured).slice(0, 3);
  return {
    hero: featured ? hero(featured) : null,
    content: [
      featured ? null : el("h1", { textContent: "Auktioner" }),
      soon.length === 3 ? el("h2", { textContent: "Slutar snart" }) : null,
      soon.length === 3 ? endingSoon(soon) : null,
      ...searchableList("Alla pågående auktioner", active),
    ],
  };
}

async function endedPage() {
  const ended = (await loadAuctions()).filter((a) => a.ended).sort(latestFirst);
  return [
    el("h1", { textContent: "Avslutade auktioner" }),
    ended.length ? cards(ended) : el("p", { className: "muted", textContent: "Ingen auktion är avslutad än." }),
  ];
}

// API:et har inget anrop för egna auktioner, så urvalet görs i webbläsaren.
async function minePage() {
  if (!me) return [el("h1", { textContent: "Mina auktioner" }), el("p", {}, el("a", { href: "#/logga-in", textContent: "Logga in" }), " för att se dina auktioner.")];
  const mine = (await loadAuctions()).filter((a) => a.sellerId === me.id);
  const active = mine.filter((a) => !a.ended).sort(soonestFirst);
  const ended = mine.filter((a) => a.ended).sort(latestFirst);
  return [
    el("h1", { textContent: "Mina auktioner" }),
    el("p", { className: "muted", textContent: `Inloggad som ${me.email}` }),
    mine.length ? null : el("p", {}, "Du har inga auktioner än. ", el("a", { href: "#/ny", textContent: "Sälj din bil" })),
    active.length ? el("h2", { textContent: "Pågående" }) : null,
    active.length ? cards(active) : null,
    ended.length ? el("h2", { textContent: "Avslutade" }) : null,
    ended.length ? cards(ended) : null,
  ];
}

function bidArea(auction) {
  if (auction.ended) {
    const result = auction.winnerId === null ? "Auktionen fick inga bud."
      : me && auction.winnerId === me.id ? "Du vann auktionen!" : `Vinnare: användare ${auction.winnerId}.`;
    return el("p", { textContent: result });
  }
  if (!me) return el("p", {}, el("a", { href: "#/logga-in", textContent: "Logga in" }), " för att lägga bud.");
  if (me.id === auction.sellerId) return el("p", { textContent: "Det här är din egen auktion, så du kan inte buda på den." });
  const hint = auction.currentPrice === null
    ? "Första budet måste vara minst startpriset. API:et visar ännu inte startpriset."
    : `Lägsta nästa bud är ${kronor(auction.currentPrice + 1000)}.`;
  const amountField = field("Ditt bud (kr)", { name: "amount", type: "number", inputMode: "numeric", step: "any" });
  // Snabbvalen fyller bara i fältet. API:et avgör om budet godkänns.
  const quick = auction.currentPrice === null ? null : el("div", { className: "quick" }, ...[1000, 5000, 10000].map((step) => {
    const button = el("button", { type: "button", className: "secondary", textContent: `+${kronor(step)}` });
    button.addEventListener("click", () => (amountField.querySelector("input").value = auction.currentPrice + step));
    return button;
  }));
  return form(
    [amountField, quick, el("span", { className: "muted", textContent: hint })].filter(Boolean),
    "Lägg bud",
    async ({ amount }) => {
      await api("POST", `/api/auctions/${auction.id}/bids`, { amount: Number(amount) });
      toast("Ditt bud är registrerat.");
      await render();
    });
}

async function detailPage(id) {
  const auction = await api("GET", `/api/auctions/${id}`);
  const refresh = el("button", { type: "button", className: "secondary", textContent: "Uppdatera buden" });
  refresh.addEventListener("click", () => render());
  const rows = auction.bids.map((bid) => el("tr", {},
    el("td", { textContent: kronor(bid.amount) }),
    el("td", { textContent: me && bid.buyerId === me.id ? "Du" : `Användare ${bid.buyerId}` }),
    el("td", { textContent: dateTime(bid.time) })));
  return [
    el("p", {}, el("a", { href: "#/", textContent: "← Alla auktioner" })),
    el("h1", { textContent: auction.name }),
    el("div", { className: "detail" },
      el("div", {}, photo(auction.name)),
      el("div", { className: "panel" },
        el("span", { className: "label", textContent: auction.ended ? "Slutbud" : "Högsta bud" }),
        el("p", { className: "price", textContent: auction.currentPrice === null ? "Inga bud" : kronor(auction.currentPrice) }),
        el("div", { className: "timing" }, el("span", { className: "label", textContent: `Sluttid: ${dateTime(auction.endTime)}` }),
          auction.ended ? el("span", { className: "value", textContent: "Avslutad" }) : countdown(auction.endTime)),
        bidArea(auction))),
    el("h2", { textContent: "Budhistorik" }),
    rows.length
      ? el("table", {}, el("thead", {}, el("tr", {}, ...["Bud", "Budgivare", "Tid"].map((t) => el("th", { scope: "col", textContent: t })))), el("tbody", {}, ...rows))
      : el("p", { textContent: "Inga bud har lagts än." }),
    el("p", {}, refresh),
  ];
}

async function signIn(email, password) {
  me = (await api("POST", "/api/auth/login", { email, password })).user;
  drawNav();
  toast("Du är inloggad.");
  location.hash = "#/";
}

function authPage(isNew) {
  const fields = [
    field("E-post", { name: "email", type: "email", autocomplete: "email" }),
    field("Lösenord", { name: "password", type: "password", autocomplete: isNew ? "new-password" : "current-password" }),
  ];
  return [
    el("div", { className: "narrow" },
      el("h1", { textContent: isNew ? "Skapa konto" : "Logga in" }),
      form(fields, isNew ? "Skapa konto" : "Logga in", async ({ email, password }) => {
        if (isNew) await api("POST", "/api/users", { email, password });
        try {
          await signIn(email, password);
        } catch (problem) {
          // 401 vid inloggning betyder fel uppgifter, inte att man behöver logga in.
          if (problem.status === 401) throw new Error("Fel e-post eller lösenord.");
          throw problem;
        }
      }),
      el("p", {}, el("a", isNew ? { href: "#/logga-in", textContent: "Har du redan ett konto? Logga in" } : { href: "#/registrera", textContent: "Inget konto? Skapa ett" }))),
  ];
}

function createPage() {
  if (!me) return [el("h1", { textContent: "Skapa auktion" }), el("p", {}, el("a", { href: "#/logga-in", textContent: "Logga in" }), " för att skapa en auktion.")];
  return [
    el("div", { className: "narrow" },
      el("h1", { textContent: "Sälj din bil" }),
      form([
        field("Namn på bilen", { name: "name", maxLength: 200 }),
        field("Startpris (kr, minst 1 000)", { name: "startingPrice", type: "number", inputMode: "numeric", step: "any" }),
        field("Sluttid", { name: "endTime", type: "datetime-local" }),
      ], "Skapa auktion", async ({ name, startingPrice, endTime }) => {
        const created = await api("POST", "/api/auctions", {
          name, startingPrice: Number(startingPrice), endTime: new Date(endTime).toISOString(),
        });
        toast("Auktionen är skapad.");
        location.hash = `#/auktion/${created.id}`;
      })),
  ];
}

async function render() {
  const current = ++renderId;
  const route = location.hash.slice(1) || "/";
  const detail = route.match(/^\/auktion\/(\d+)$/);
  if (!view.children.length) view.replaceChildren(el("p", { textContent: "Hämtar…" }));
  let content;
  try {
    content = detail ? await detailPage(detail[1])
      : route === "/logga-in" ? authPage(false)
      : route === "/registrera" ? authPage(true)
      : route === "/ny" ? createPage()
      : route === "/" ? await listPage()
      : route === "/avslutade" ? await endedPage()
      : route === "/mina" ? await minePage()
      : [el("h1", { textContent: "Sidan finns inte" }), el("a", { href: "#/", textContent: "Till auktionerna" })];
  } catch (problem) {
    const retry = el("button", { type: "button", textContent: "Försök igen" });
    retry.addEventListener("click", () => render());
    content = [el("h1", { textContent: "Det gick inte att hämta sidan" }), el("p", { className: "error", textContent: problem.message }), retry];
  }
  // Ett långsamt svar får inte skriva över en nyare sida.
  if (current !== renderId) return;
  const page = Array.isArray(content) ? { content } : content;
  view.replaceChildren(...[page.hero, el("div", { className: "wrap" }, ...page.content.filter(Boolean))].filter(Boolean));
  document.body.classList.toggle("has-hero", Boolean(page.hero));
  for (const link of document.querySelectorAll(".top nav a")) {
    if (link.hash === (location.hash || "#/")) link.setAttribute("aria-current", "page");
    else link.removeAttribute("aria-current");
  }
}

setInterval(() => {
  let justEnded = false;
  for (const node of view.querySelectorAll("[data-end]")) {
    const text = timeLeft(node.dataset.end);
    if (text === "Avslutad" && node.textContent !== text) justEnded = true;
    node.textContent = text;
    node.classList.toggle("urgent", isUrgent(node.dataset.end));
  }
  if (justEnded) render();
}, 1000);

window.addEventListener("hashchange", () => {
  view.replaceChildren();
  document.body.classList.remove("has-hero");
  scrollTo(0, 0);
  render().then(() => view.focus());
});

(async () => {
  try {
    me = (await api("GET", "/api/auth/login")).user;
  } catch {
    me = null;
  }
  drawNav();
  render();
})();
