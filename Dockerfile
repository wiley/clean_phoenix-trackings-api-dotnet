FROM mcr.microsoft.com/dotnet/aspnet:6.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ADD --chmod=644 https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem /cert/global-bundle.pem

WORKDIR /cert/

RUN cat global-bundle.pem|awk 'split_after==1{n++;split_after=0} /-----END CERTIFICATE-----/ {split_after=1} {print > "cert" n ""}' ;\
    for CERT in /cert/cert*; do mv $CERT /usr/local/share/ca-certificates/aws-rds-ca-$(basename $CERT).crt; done ;\
    update-ca-certificates

FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS build
WORKDIR /src

ARG ART_USER
ARG ART_PASS
ARG ART_URL

RUN dotnet nuget add source --name crossknowledge/phoenix $ART_URL --username $ART_USER --password $ART_PASS --store-password-in-clear-text
RUN mkdir Trackings.API
COPY ./Trackings.API/Trackings.API.csproj ./Trackings.API/
RUN mkdir Trackings.Domain
COPY ./Trackings.Domain/Trackings.Domain.csproj ./Trackings.Domain/
RUN mkdir Trackings.Infrastructure
COPY ./Trackings.Infrastructure/Trackings.Infrastructure.csproj ./Trackings.Infrastructure/
RUN mkdir Trackings.Infrastructure.Interface
COPY ./Trackings.Infrastructure.Interface/Trackings.Infrastructure.Interface.csproj ./Trackings.Infrastructure.Interface/
RUN mkdir Trackings.Services
COPY ./Trackings.Services/Trackings.Services.csproj ./Trackings.Services/
COPY . .
RUN dotnet restore "Trackings.API/Trackings.API.csproj"
WORKDIR "/src/Trackings.API"
RUN dotnet build "Trackings.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Trackings.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Trackings.API.dll"]
