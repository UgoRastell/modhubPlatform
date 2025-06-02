# ---------- build Blazor ----------
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /src
    COPY . .
    RUN dotnet publish -c Release -o /app/dist
    
    # ---------- runtime : Nginx ----------
    FROM nginx:alpine AS final
    WORKDIR /usr/share/nginx/html
    COPY --from=build /app/dist/wwwroot .          
    # Fallback SPA (toutes les routes → index.html)
    RUN printf 'server {\n  listen 80;\n  root /usr/share/nginx/html;\n  index index.html;\n  location / { try_files $uri $uri/ /index.html; }\n}\n' \
          > /etc/nginx/conf.d/default.conf
    
    EXPOSE 80
    CMD ["nginx", "-g", "daemon off;"]
    