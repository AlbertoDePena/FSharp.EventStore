FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS installer-env

COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.fsproj --configuration Release --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet:3.0
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_V2_COMPATIBILITY_MODE=true \
    DB_CONNECTION_STRING=""

LABEL maintainer "Alberto De Pena <albertodepena89@gmail.com>"

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]