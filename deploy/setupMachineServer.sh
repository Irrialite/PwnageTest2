#!/bin/bash
curl -O https://bootstrap.pypa.io/get-pip.py
python2.7 get-pip.py
pip install awscli
pip install --upgrade awscli
apt-get -y update
apt-get -y install ruby2.0
apt-get -y install wget
wget https://aws-codedeploy-us-east-1.s3.amazonaws.com/latest/install
chmod +x ./install
./install auto
/opt/codedeploy-agent/bin/install auto

sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
apt-key adv --keyserver apt-mo.trafficmanager.net --recv-keys 417A0893
apt-get -y update
apt-get -y install dotnet-dev-1.0.0-preview1-002702

# nginx stuff
apt-get -y install nginx
cat <<EOM >"/etc/nginx/sites-available/default"
server {
	listen 80;
	
	server_name localhost;
	location / {
		proxy_pass http://localhost:5004;
		proxy_http_version 1.1;
		proxy_set_header Upgrade \$http_upgrade;
		proxy_set_header Connection keep-alive;
		proxy_set_header Host \$host;
		proxy_cache_bypass \$http_upgrade;
	}
}
EOM
nginx -s reload