# Build stage
FROM oven/bun:latest AS builder
WORKDIR /app

# Install dependencies (separate layer for better caching)
COPY package.json bun.lock* ./
RUN bun install --frozen-lockfile

# Copy source code and build
COPY . .
RUN bun run build

# Runtime stage
FROM nginx:alpine

# Copy built assets from builder
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
