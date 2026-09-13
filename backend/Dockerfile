FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY . .

WORKDIR /app/src/App

RUN dotnet restore App.csproj

EXPOSE 8080

ENTRYPOINT ["dotnet", "run", "--urls", "http://0.0.0.0:8080"]