FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx .
COPY UNI-EDU-Backend.Domain/*.csproj UNI-EDU-Backend.Domain/
COPY UNI-EDU-Backend.Application/*.csproj UNI-EDU-Backend.Application/
COPY UNI-EDU-Backend.Infrastructure/*.csproj UNI-EDU-Backend.Infrastructure/
COPY UNI-EDU-Backend.API/*.csproj UNI-EDU-Backend.API/
RUN dotnet restore

COPY . .
RUN dotnet publish UNI-EDU-Backend.API -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UNI-EDU-Backend.API.dll"]
