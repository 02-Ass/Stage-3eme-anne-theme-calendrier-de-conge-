# Многоступенчатая сборка: сначала собираем в полном SDK-образе,
# затем копируем только результат публикации в лёгкий runtime-образ
# (без SDK, компиляторов и т.д. — меньше размер, меньше поверхность атаки).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только csproj — чтобы Docker закэшировал restore отдельным
# слоем и не переустанавливал пакеты при каждом изменении кода.
COPY VacationPlanner.Api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Образы ASP.NET Core для .NET 8 по умолчанию слушают порт 8080
# (непривилегированный, контейнер по умолчанию работает не под root).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VacationPlanner.Api.dll"]
