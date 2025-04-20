# ���������� ����������� ����� .NET SDK ��� ������
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# �������� ����� ������� � ��������������� �����������
COPY *.csproj .
RUN dotnet restore

# �������� ��� ����� � �������� ����������
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ���������� ����� .NET Runtime ��� �������
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# ������������� ����������� �����������
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# ��������� ����������� Microsoft Edge
RUN wget -q https://packages.microsoft.com/keys/microsoft.asc -O- | apt-key add - \
    && echo "deb [arch=amd64] https://packages.microsoft.com/repos/edge stable main" > /etc/apt/sources.list.d/microsoft-edge.list

# ������������� Microsoft Edge Stable
RUN apt-get update && apt-get install -y microsoft-edge-stable \
    && rm -rf /var/lib/apt/lists/*

# ������������� msedgedriver (������ ������ ��������������� ������ Edge)
# �������� ������ Edge
RUN EDGE_VERSION=$(microsoft-edge --version | awk '{print $3}') && \
    # ��������� ��������������� msedgedriver
    wget -q "https://msedgedriver.azureedge.net/${EDGE_VERSION}/edgedriver_linux64.zip" -O edgedriver.zip && \
    unzip edgedriver.zip -d /usr/local/bin && \
    chmod +x /usr/local/bin/msedgedriver && \
    rm edgedriver.zip

# �������� ��������� ���������� �� ������ build
COPY --from=build /app/publish .

# ��������� ����� �����
ENTRYPOINT ["dotnet", "Scraper.dll"]