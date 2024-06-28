FROM cgsama/dotnet-sdk-7-runtime-3-1:latest
WORKDIR /src
COPY . ./
RUN dotnet build -f netcoreapp3.1
RUN dotnet publish -f netcoreapp3.1 -o /app
WORKDIR /app
COPY ./system.json ./system.json
COPY ./less3.db ./less3.db
COPY ./start.sh ./start.sh

ENTRYPOINT ["bash", "start.sh"]