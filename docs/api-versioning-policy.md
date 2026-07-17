 TMS API Versioning Policy
Purpose
This document defines what changes are safe to ship without a new API
version, and what changes require one. Any engineer proposing a change
to a shipped endpoint should be able to classify it using this page alone.

1. Breaking Changes (require a new version)
A change is "breaking" if an existing client, calling the endpoint exactly
as it does today, would get a different result or fail. This includes:
- Removing a field from a response
- Renaming a field (clients read by name, not position)
- Changing a field's data type (e.g. `int` → `string`)
- Changing a status code for an existing scenario (e.g. 200 → 201)
- Tightening validation on a request (a payload that used to be accepted
  is now rejected)
- Changing the default sort order or default page size
- Changing the URL structure of an existing route

 2. Additive Changes (safe on the current version)
A change is "additive" if no existing client is affected, because nothing
they already depend on has changed. This includes:
- Adding a new optional field to a response
- Adding a brand-new endpoint
- Adding a new optional query parameter with a sane default
- Loosening validation (accepting more than before)
- Adding a new enum value, as long as clients are documented to ignore
  unknown values

"Rule of thumb:" if you can't say with certainty that no client's code
would need to change, treat it as breaking.

 3. Sunset Window
Once a new major version ships, the previous version stays live for a
"minimum of 6 months". This is not negotiable for exceptions — it exists
because rural training centres run on quarterly maintenance cycles and
need a full cycle plus buffer to plan and execute a migration.

The exact sunset date is fixed the day the new version ships and
communicated immediately (see below) — it is never shortened after
the fact.

 4. Communication
From day one of a new version shipping, the previous version's responses
carry three headers on every request:
- `Deprecation: true`
- `Sunset: <RFC 7231 date>`
- `Link: <new-version-url>; rel="successor-version"`

In addition:
- A `CHANGELOG.md` entry is added describing what changed and why
- An email is sent to every team known to hold an API key for the
  deprecated version
- A calendar invite is sent for the shutdown date itself, so the sunset
  isn't a surprise even to teams who missed the email

5. Version Skipping
Clients are never required to upgrade through every intermediate
version. A client on V1 may jump directly to V3 once V3 ships — V2 does
not need to be a stopover. Each version's contract stands on its own;
we do not assume linear migration paths.