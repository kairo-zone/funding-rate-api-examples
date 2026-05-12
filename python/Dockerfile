# kairo.zone Funding API - Python examples
#
# Build:
#   docker build -t kairo-funding-py -f python/Dockerfile python
# Run a specific example:
#   docker run --rm -e KAIRO_FUNDING_API_KEY=xxx kairo-funding-py \
#       python examples/04_spread_scanner.py
# Default invocation runs example 01:
#   docker run --rm -e KAIRO_FUNDING_API_KEY=xxx kairo-funding-py

FROM python:3.14-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PIP_NO_CACHE_DIR=1

WORKDIR /app

# Install dependencies first for better layer caching.
COPY pyproject.toml README.md ./
COPY kairo_funding ./kairo_funding
COPY examples ./examples

RUN pip install --no-cache-dir .

CMD ["python", "examples/01_quickstart.py"]
