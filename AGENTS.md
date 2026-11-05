# Repository intent

Research and, later, define a Windows desktop experience for managing Linux containers provided by WSL's `wslc` feature (WSL containers / WSLC). The current purpose is to gather durable technical and product knowledge so future planning can be based on evidence rather than repeated web searches.

## Working guidance

- Treat this as research and product discovery until the user explicitly asks to plan or implement. Do not assume feature scope, architecture, or a product direction.
- Capture the user's opinions and requirements as they are stated; do not invent them or replace them with generic container-manager priorities.
- Distinguish WSL itself, WSL distributions, WSL containers (`wslc`), container runtimes, and desktop clients. Do not assume WSLc implements a Docker daemon/API or native Compose support unless current primary sources establish it.
- Prefer current Microsoft documentation, release notes, API references, and source code for technical claims. Use project repositories for the projects' own capabilities and design claims. Date time-sensitive findings and label uncertainty or inference.
- Existing desktop tools exist; compare their actual scope and maturity before describing a gap or proposing a new one. Do not treat a feature list as proof of quality or user fit.
- Preserve user-stated tool preferences (including the Portainer UX reference) as context, not as a requirement to clone a product or its architecture. Distinguish an in-process app service layer, a local host bridge for a web UI, and a remote management service; do not select one before the user asks.
- Keep research notes concise, linked to sources, and organized under `research/`. Update stale findings when later decisions depend on them.
- GitHub write operations are an approval checkpoint, not a hard limit: ask and get the user's OK before mutating the remote (e.g. `push`/`force-push`, branch or tag deletion, pull requests, releases, or any other change that alters it), then proceed once approved. The user will often approve. Read-only actions (`fetch`, `ls-remote`, `status`, `diff`, `log`) need no approval.
