FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
USER root
RUN apk add --no-cache curl tzdata icu-libs krb5-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
USER $APP_UID
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Page.Ui.SvelteRender/Page.Ui.SvelteRender.csproj", "Page.Ui.SvelteRender/"]
COPY ["Page.Ui.Application/Page.Ui.Application.csproj", "Page.Ui.Application/"]
COPY ["Page.Ui.Domain/Page.Ui.Domain.csproj", "Page.Ui.Domain/"]
COPY ["Page.Ui.Infrastructure/Page.Ui.Infrastructure.csproj", "Page.Ui.Infrastructure/"]
RUN dotnet restore "Page.Ui.SvelteRender/Page.Ui.SvelteRender.csproj"
COPY . .
WORKDIR "/src/Page.Ui.SvelteRender"
RUN dotnet build "Page.Ui.SvelteRender.csproj" -c $BUILD_CONFIGURATION -o /app/build
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Page.Ui.SvelteRender.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER root
WORKDIR /app
RUN chown -R $APP_UID:$APP_UID /app
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Page.Ui.SvelteRender.dll"]
