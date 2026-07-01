# Project Brief: Flight + Accommodation Deal Finder

## Goal

A system that automatically finds unusually cheap bundle deals (flight + accommodation) for a predefined set of origins and destinations, filters them with an algorithm, and for each qualifying deal generates marketing text and a visual, then emails the owner (Frano, franogugic@icloud.com) for review.

This brief describes the **whole product**, but the first build phase (for the Architect/Developer/Reviewer team) is exclusively: **scraper + filter algorithm + database**. The Text/Design agents come in a later phase, once the scraper is built and working.

## Watchlist (fixed, not broad scanning)

**Origins (9):** Zagreb, Split, Zadar, Belgrade, Sarajevo, Vienna, Skopje, Pristina, Dubrovnik

**Destinations (24):** Paris (FR), Rome (IT), London (UK), Barcelona (ES), Amsterdam (NL), Prague (CZ), Vienna (AT), Florence (IT), Venice (IT), Madrid (ES), Berlin (DE), Budapest (HU), Lisbon (PT), Santorini (GR), Athens (GR), Milan (IT), Munich (DE), Reykjavik (IS), Copenhagen (DK), Dublin (IE), Stockholm (SE), Valletta (MT), Valencia (ES), Nice (FR), Zurich (CH)

Up to **216 origin-destination combinations** in total.

## Data sources

- **Flights:** Kiwi.com Tequila API (flexible date-range search, good low-cost-carrier coverage). Amadeus Self-Service API as a possible backup/supplement later.
- **Accommodation:** MVP uses a RapidAPI marketplace "Booking.com data" wrapper (aggregator, pay-per-call). Note: verify current terms of use at implementation time, since these offerings change. Later, if the project grows, consider an official Booking.com Affiliate/Partner API integration.

## Search logic (search space)

For each origin-destination combination, on every run the scraper:
- Scans the **entire flexible window**: departure anywhere from **1 to 3 months** from the current date, combined with trip durations of **3-8 days**.
- Of all prices found (flight + accommodation bundle), takes only the **cheapest** combination as "today's price" for that destination.

## Filter algorithm ("good deal")

- For each destination, compute the **median** of all prices found **within that same scan** (not across historical days — no need for long-term price history storage).
- A deal that is **≥30% below the scan's median** = **good deal**.
- A deal that is **≥60% below the median** = **potential error fare** (higher urgency, different tone in later communication).
- No cold-start problem — works from day one.

## Database

- **Postgres**, with **LISTEN/NOTIFY** — when the scraper writes a new qualifying deal, an event fires (no polling needed by downstream consumers).
- The deals table should store at least: origin, destination, departure date, duration, price, scan median, % below median, status (`new` / later processed by Text/Design agents), timestamp.

## Future phases (out of scope for this build, context only)

- **Text agent** (always-on, listens on Postgres NOTIFY) generates marketing copy for each qualifying deal.
- **Design agent** (always-on) fills an HTML/CSS template with the deal's data (destination image, price, dates) and code renders it to an image (e.g. headless-browser screenshot). No external image-generation API needed.
- Once both are ready, an email is sent to Frano for review. **Automated posting (e.g. social media) is planned for later, not now — do not build that functionality in this phase.**
- If the Architect encounters an ambiguity requiring a human decision (not something it can guess), the plan is to escalate it via a small web form to Frano — that form doesn't exist yet, so for now ambiguities should be raised directly in conversation.

## Implementation notes

- 216 combinations × recurring scraping has real API-call-volume and cost implications with Kiwi Tequila — check whether it supports flexible month-range search in a single call.
- Booking.com data via a RapidAPI wrapper needs a terms-of-use check before implementation.
