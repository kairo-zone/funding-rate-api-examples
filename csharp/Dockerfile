FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY csharp/ ./csharp/
RUN dotnet publish csharp/src/Kairo.Funding.Examples/Kairo.Funding.Examples.csproj \
    -c Release -o /out --nologo

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "kairo-funding.dll"]
CMD ["01"]
