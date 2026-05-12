# asciinema demo

Tooling to record a short terminal demo of the kairo.zone Funding API
quickstart. The recording is meant to be embedded at the top of the root
`README.md` so visitors can see real output without cloning anything.

## What's here

| File              | Purpose                                                     |
|-------------------|-------------------------------------------------------------|
| `README.md`       | This file - how to record, publish, and embed the demo.     |
| `record.sh`       | POSIX shell script that drives `asciinema rec` end-to-end.  |
| `demo-script.md`  | Human-readable storyboard with timing notes for each scene. |

No `.cast` file is checked in - see the last section for why.

## How to record

Prerequisites:

- `asciinema` CLI installed (`pipx install asciinema` or your distro's pkg).
- A working Python 3.10+ install with the `kairo_funding` package available
  (`cd python && pip install -e .` from the repo root).
- A valid API key exported as `KAIRO_FUNDING_API_KEY`.

Then, from the repository root:

```bash
export KAIRO_FUNDING_API_KEY=your_key_here
bash asciinema/record.sh
```

The script records into `asciinema/quickstart.cast`. The full take should
run 60-90 seconds; see `demo-script.md` for the per-segment breakdown.

You can preview the result locally before uploading:

```bash
asciinema play asciinema/quickstart.cast
```

## How to publish

```bash
asciinema upload asciinema/quickstart.cast
```

The CLI prints a URL of the form `https://asciinema.org/a/<id>`. The first
upload from a machine prompts you to claim ownership by linking your
asciinema.org account; subsequent uploads are silent.

## How to embed in the root README

Once you have the asciinema URL, replace `<id>` below with the numeric id
and add the snippet near the top of the repository's root `README.md`:

```markdown
[![asciicast](https://asciinema.org/a/<id>.svg)](https://asciinema.org/a/<id>)
```

That renders as a clickable SVG poster that opens the player on
asciinema.org.

### Alternative: self-hosted SVG via agg

If you would rather not depend on the asciinema.org service, the
[`agg`](https://github.com/asciinema/agg) tool renders a `.cast` to an
animated SVG or GIF that you can commit alongside the project:

```bash
agg asciinema/quickstart.cast asciinema/quickstart.svg
```

Then embed it directly:

```markdown
![Funding API quickstart](asciinema/quickstart.svg)
```

This is heavier in the repo but has no third-party runtime dependency.

## Why no .cast file in this commit

A `.cast` is a real recording of a real terminal session and embeds live
funding data, response counts, and any environment leakage that happened
during the take - that is not appropriate to ship from a development
machine. The maintainer regenerates it on a clean run with a published
API key just before tagging a release.
