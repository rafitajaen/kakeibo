FROM oven/bun:1.3

WORKDIR /app

# Copy package.json first for layer caching
COPY package.json bun.lock* ./
RUN bun install --frozen-lockfile

# Copy source code
COPY . .

# Expose port
EXPOSE 3050

# Health check
HEALTHCHECK --interval=10s --timeout=5s --start-period=5s --retries=3 \
  CMD wget --spider -q http://localhost:3050/health || exit 1

# Start server
CMD ["bun", "run", "index.ts"]
