
FROM ubuntu:focal
ENV TZ=America/Toronto
RUN apt-get update
RUN apt-get install -y wget
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y dotnet-sdk-7.0 dotnet-runtime-7.0
WORKDIR /app
COPY . ./
#dotnet build -f netcoreapp2.2
#dotnet publish -f netcoreapp2.2