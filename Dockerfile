FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/ProjectManagement.Api/ProjectManagement.Api.csproj", "src/ProjectManagement.Api/"]
COPY ["src/ProjectManagement.Application/ProjectManagement.Application.csproj", "src/ProjectManagement.Application/"]
COPY ["src/ProjectManagement.Domain/ProjectManagement.Domain.csproj", "src/ProjectManagement.Domain/"]
COPY ["src/ProjectManagement.Infrastructure/ProjectManagement.Infrastructure.csproj", "src/ProjectManagement.Infrastructure/"]
RUN dotnet restore "src/ProjectManagement.Api/ProjectManagement.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/ProjectManagement.Api"
RUN dotnet build "ProjectManagement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "ProjectManagement.Api.dll"]
