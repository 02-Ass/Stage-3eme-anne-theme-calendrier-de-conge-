# Стадия 1: собираем статику через Vite.
# ВАЖНО: Vite "запекает" переменные VITE_* в файлы на этапе сборки, а не
# читает их в рантайме — поэтому передаём их как build-time ARG, а не
# через environment у контейнера в docker-compose (это не сработало бы).
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .

ARG VITE_API_BASE_URL=http://localhost:5000/api
ARG VITE_OIDC_AUTHORITY=http://localhost:8080/realms/vacation-planner
ARG VITE_OIDC_CLIENT_ID=vacation-planner-frontend
ARG VITE_AUTH_DISABLED=false
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_OIDC_AUTHORITY=$VITE_OIDC_AUTHORITY \
    VITE_OIDC_CLIENT_ID=$VITE_OIDC_CLIENT_ID \
    VITE_AUTH_DISABLED=$VITE_AUTH_DISABLED

RUN npm run build

# Стадия 2: отдаём собранную статику лёгким nginx, без Node вообще.
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
