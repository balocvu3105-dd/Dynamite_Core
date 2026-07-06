# src/dynamite-dashboard/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Build args
ARG VITE_API_URL=http://localhost:5000
ARG VITE_DISCORD_CLIENT_ID
ARG VITE_DISCORD_REDIRECT_URI=http://localhost:5173/auth/callback

# Copy source
COPY . .

# Ghi đè .env.production bằng build args để đảm bảo đúng values
RUN echo "VITE_API_URL=${VITE_API_URL}" > .env.production \
 && echo "VITE_DISCORD_CLIENT_ID=${VITE_DISCORD_CLIENT_ID}" >> .env.production \
 && echo "VITE_DISCORD_REDIRECT_URI=${VITE_DISCORD_REDIRECT_URI}" >> .env.production \
 && cat .env.production

RUN npm run build

# Serve với Nginx
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80