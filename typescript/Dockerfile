FROM node:20-alpine
WORKDIR /app

# Build context is the typescript/ directory.
COPY package.json tsconfig.json ./
RUN npm install --no-audit --no-fund

COPY src ./src

ENV NODE_ENV=production
CMD ["npx", "tsx", "src/examples/01_quickstart.ts"]
