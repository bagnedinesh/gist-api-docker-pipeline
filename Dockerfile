FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Set the working directory inside the image
WORKDIR /app
# Expose the API port.
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Copy repository files into the build image
WORKDIR /src
COPY . ./
# Publish the GistApi project to a folder for the runtime image
WORKDIR /src/src/GistApi
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
# Set working directory for the final runtime image
WORKDIR /app
# Copy the published files from the build image
COPY --from=build /app/publish ./
# Listen on the API port on all container interfaces.
ENV ASPNETCORE_URLS=http://+:8080
# Start the application by running the published assembly
ENTRYPOINT ["dotnet", "GistApi.dll"]
