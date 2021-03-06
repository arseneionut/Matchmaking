user  nginx;
worker_processes {{cpu_count}};
worker_rlimit_nofile 100000;

error_log  /var/log/nginx_error.log crit;
pid        /var/run/nginx.pid;


events {
    worker_connections {{ cpu_count * 64}};
    use epoll;
}


http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                      '$status $body_bytes_sent "$http_referer" '
                      '"$http_user_agent" "$http_x_forwarded_for"';

    access_log  /var/log/nginx/access.log  main;
    sendfile        on;
    #tcp_nopush     on;
    keepalive_timeout  65;
    #gzip  on;

    # Balanced server instances are defined here.
    upstream server {
    {% for server_domain in servers %}
        server {{server_domain}};
    {% endfor %}
    }

    server {
        listen 8000;

        location / {
            set $upstream http://server;

            proxy_pass $upstream;
            proxy_http_version 1.1;
            proxy_redirect off;
            proxy_pass_request_headers on;
        }
    }
}