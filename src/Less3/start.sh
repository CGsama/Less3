mkdir -p /data/less3disk && echo ok
mkdir -p /data/temp && echo ok
mkdir -p /data/logs && echo ok
cp -n /app/less3.db /data/less3.db && echo ok
systemctl start nginx && echo ok
export ENV LANG=en_US.UTF-8 && ok
dotnet Less3.dll && echo ok