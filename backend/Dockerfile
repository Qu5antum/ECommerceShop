FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

EXPOSE 5000
EXPOSE 5001

COPY src/App/App.csproj ./src/App/

RUN dotnet restore ./src/App/App.csproj

COPY . .

WORKDIR /app/src/App

RUN dotnet build App.csproj -c Release

RUN dotnet tool install --global dotnet-ef --version 10.0.*

ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "bin/Release/net10.0/App.dll"]