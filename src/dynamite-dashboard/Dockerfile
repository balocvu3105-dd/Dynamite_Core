# src/dynamite-dashboard/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Build
COPY . .
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL
ARG VITE_DISCORD_CLIENT_ID
ENV VITE_DISCORD_CLIENT_ID=$VITE_DISCORD_CLIENT_ID
ARG VITE_DISCORD_REDIRECT_URI=http://localhost:5173/auth/callback
ENV VITE_DISCORD_REDIRECT_URI=$VITE_DISCORD_REDIRECT_URI
RUN npm run build

# Serve với Nginx
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80