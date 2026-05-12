# Demo storyboard

A 60-90 second asciinema take that shows the kairo.zone Funding API in three
short segments. The intent: prove "one GET, real data" without overwhelming
a first-time viewer.

Total target length: ~75 seconds. Keep the typing speed natural - asciinema
records real terminal output, so do not rush.

## Opening (3 seconds)

- `clear` then `echo "kairo.zone Funding API - quickstart"`
- Hold the title card on screen for ~2 seconds before the next command.

## Segment 1: quickstart (15 seconds)

- `cat python/examples/01_quickstart.py | head -20`
  - Shows that the whole client call is ~10 lines of Python. Let it sit ~2s.
- `python python/examples/01_quickstart.py`
  - Prints `version=... count=...` then five rows of (exchange, base, rate,
    next funding time, interval). The viewer should see real exchanges and
    a non-zero count - that is the "this is live" moment.
- Pause ~2s after output so the eye can settle.

## Segment 2: spread scanner (20 seconds)

- Banner line `--- cross-exchange spread ---` to mark the transition.
- `KAIRO_BASE=BTC python python/examples/04_spread_scanner.py`
  - Prints one line per exchange that lists BTC perpetuals, annualized %
    in a readable format, then a `spread = ...` summary line.
- This is the longest output of the three; give it ~2s to land before
  moving on. Resist the urge to trim - the spread summary is the payoff.

## Segment 3: top funding (15 seconds)

- Banner line `--- top 10 funding rates ---`.
- `python python/examples/08_top_funding.py`
  - Prints "TOP 10 POSITIVE" and "BOTTOM 10 NEGATIVE" blocks.
- Pause ~2s.

## Outro (4 seconds)

- `echo` two lines:
  - `docs:    https://kairo.zone/docs`
  - `signup:  https://kairo.zone`
- Hold ~2 seconds, then Ctrl-D to end the recording.

## Pacing notes

- `--idle-time-limit 2` is passed to `asciinema rec`, so any pause longer
  than two seconds will be compressed in playback. That means the `sleep 2`
  calls in `record.sh` are upper bounds, not literal waits for the viewer.
- If a run prints unusually long output (e.g. many exchanges report on BTC),
  re-record at a different hour - the snapshot is live.
- Never paste an API key on screen. Export it in the shell before invoking
  `record.sh`; asciinema records the inner command output only.
