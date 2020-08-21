FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /stage

COPY *.fsproj ./

RUN dotnet restore

COPY . ./

RUN dotnet publish --configuration Release --output ./output --runtime alpine-x64 --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine AS runtime

LABEL maintainer "Alberto De Pena <albertodepena89@gmail.com>"

WORKDIR /Database

COPY --from=build ./stage/output/ ./

ENV DOTNET_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DB_CONNECTION_STRING=""
    
RUN chmod +x ./EventStore.Database

ENTRYPOINT ["./EventStore.Database"]