# Используем официальный образ .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем все файлы и собираем приложение
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Используем образ .NET Runtime для запуска
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Устанавливаем необходимые зависимости
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Добавляем репозиторий Microsoft Edge
RUN wget -q https://packages.microsoft.com/keys/microsoft.asc -O- | apt-key add - \
    && echo "deb [arch=amd64] https://packages.microsoft.com/repos/edge stable main" > /etc/apt/sources.list.d/microsoft-edge.list

# Устанавливаем Microsoft Edge Stable
RUN apt-get update && apt-get install -y microsoft-edge-stable \
    && rm -rf /var/lib/apt/lists/*

# Устанавливаем msedgedriver (версия должна соответствовать версии Edge)
# Получаем версию Edge
RUN EDGE_VERSION=$(microsoft-edge --version | awk '{print $3}') && \
    # Скачиваем соответствующий msedgedriver
    wget -q "https://msedgedriver.azureedge.net/${EDGE_VERSION}/edgedriver_linux64.zip" -O edgedriver.zip && \
    unzip edgedriver.zip -d /usr/local/bin && \
    chmod +x /usr/local/bin/msedgedriver && \
    rm edgedriver.zip

# Копируем собранное приложение из образа build
COPY --from=build /app/publish .

# Указываем точку входа
ENTRYPOINT ["dotnet", "Scraper.dll"]