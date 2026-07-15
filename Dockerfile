# ===================================
# Stage 1: Build Stage
# Uses full SDK image to restore, build, and publish
# ===================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files first for better cache usage
COPY ["ResumeAnalyzer.slnx", "./"]
COPY ["ResumeAnalyzer/ResumeAnalyzer.csproj", "ResumeAnalyzer/"]
COPY ["ResumeAnalyzer.Tests/ResumeAnalyzer.Tests.csproj", "ResumeAnalyzer.Tests/"]

# Restore both projects
RUN dotnet restore "ResumeAnalyzer.slnx"

# Copy remaining source code
COPY . .

# Build in Release mode for optimizations
#RUN dotnet build "ResumeAnalyzer/ResumeAnalyzer.csproj" -c Release -o /app/build --no-restore

# Run unit tests
# If tests fail, Docker build will stop here
RUN dotnet build "ResumeAnalyzer.Tests/ResumeAnalyzer.Tests.csproj" -c Release --no-restore
RUN dotnet test "ResumeAnalyzer.Tests/ResumeAnalyzer.Tests.csproj" -c Release --no-build

# ===================================
# Stage 2: Publish Stage
# Creates self-contained, trimmed deployment
# ===================================
FROM build AS publish
RUN dotnet publish "ResumeAnalyzer/ResumeAnalyzer.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ===================================
# Stage 3: Runtime Stage (Final Image)
# Uses the secure, highly-compatible debian-slim runtime
# ===================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-slim AS final
WORKDIR /app

# Run as non-root user for security
RUN addgroup -g 1000 appgroup && \
    adduser -u 1000 -G appgroup -D appuser
USER appuser

# Copy published files from publish stage
COPY --from=publish  /app/publish .

# Expose port (informational only)
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ResumeAnalyzer.dll"]
