# Qt Bridge for C# - API Documentation Build

## Prerequisites

Install DocFX as a global .NET tool (one-time):

```
dotnet tool install -g docfx
```

## Quick start

Build and preview locally:

```
docfx docs/docfx.json --serve
```

Then open <http://localhost:8080>.

## Directory layout

```
docs/
  docfx.json        # DocFX configuration
  toc.yml           # Top navigation bar
  index.md          # Landing page
  filterConfig.yml  # Excludes types/namespaces from API reference
  template/
    public/
      main.css      # Visual overrides and landing page styles
  README.md         # This file
  api/              # Intermediate API metadata - gitignored, do not edit
  _site/            # Build output - gitignored, do not edit
```

## Build commands

| Task | Command |
|---|---|
| Build + serve | `docfx docs/docfx.json --serve` |
| Build only | `docfx build docs/docfx.json` |
| Serve existing output | `docfx serve docs/_site --port 8080` |
| Full clean rebuild | see below |

`docfx docs/docfx.json --serve` builds the site and immediately starts a local web server
at <http://localhost:8080>. Use this when you have made changes and want to preview them.

`docfx serve docs/_site --port 8080` only starts the web server over the already-built
`_site/` folder — it does **not** rebuild. Use this when the output is already up to date
and you just want to browse it.

### Full clean rebuild

Required after changing the API source or `filterConfig.yml`.

**Windows (cmd):**
```
rmdir /s /q docs\api docs\_site
docfx metadata docs/docfx.json
docfx build docs/docfx.json
```

**Linux / macOS:**
```
rm -rf docs/api docs/_site
docfx metadata docs/docfx.json
docfx build docs/docfx.json
```

`docfx build` does not remove stale files from `api/` or `_site/`, so a clean is
necessary whenever types are added, removed, or filtered.

## Excluding types from the API reference

Edit [`filterConfig.yml`](filterConfig.yml). Rules are matched top-to-bottom; first
match wins. The file must end with a catch-all `include` rule.

### Exclude a single type

```yaml
- exclude:
    uidRegex: ^My\.Namespace\.MyClass$
```

### Exclude an entire namespace

```yaml
- exclude:
    uidRegex: ^My\.Namespace(\..+)?$
```

The `(\..+)?` part matches both the namespace root (`My.Namespace`) and all its members
(`My.Namespace.SomeClass`, `My.Namespace.SomeClass.Method`, ...).

After editing the filter, run a full clean rebuild (see above).

## Visual customisation

[`template/public/main.css`](template/public/main.css) is merged into the site's
stylesheet on every build. Add CSS rules there to override the default DocFX modern theme.

## Publishing

Copy the contents of `docs/_site/` to your doc server. The folder is self-contained -
no server-side processing is required, any static file host works.
