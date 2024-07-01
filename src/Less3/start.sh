mkdir -p /data/less3disk && echo ok
mkdir -p /data/temp && echo ok
mkdir -p /data/logs && echo ok
cp /data/less3.conf /etc/nginx/conf.d/less3.conf && echo ok
cp -n /etc/nginx/conf.d/less3.conf /data/less3.conf && echo ok
cp /data/system.json /app/system.json && echo ok
cp -n /app/system.json /data/system.json && echo ok
systemctl restart nginx
cp -n /app/less3.db /data/less3.db && echo ok
systemctl start nginx && echo ok
export ENV LANG=en_US.UTF-8 && ok
dotnet Less3.dll && echo ok