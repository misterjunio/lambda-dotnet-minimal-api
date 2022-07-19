FROM public.ecr.aws/lambda/dotnet:6 AS base

# Setup build image
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim as build
WORKDIR /
COPY ./TodoApi.sln ./
COPY ./TodoApi/TodoApi.csproj ./TodoApi/
COPY ./TodoApi.Tests/TodoApi.Tests.csproj ./TodoApi.Tests/

# Restore
RUN dotnet restore -v minimal

# Bust Docker Cache
ARG CACHE_BUST

# Copy remaining source and test code for building
COPY . /

# Build
RUN dotnet build --no-restore -c Release -v minimal

# Test
RUN dotnet test --no-restore --no-build -c Release -v minimal

# Publish
RUN dotnet publish ./TodoApi/TodoApi.csproj --no-build -c Release -v minimal -o /app

# Setup runtime image
FROM base AS runtime
WORKDIR /var/task

# Copy built app from the build image
COPY --from=build /app .

# .NET 6 Minimal API is an executable assembly
CMD [ "TodoApi" ]
