from jinja2 import Environment, FileSystemLoader, select_autoescape
import os
import sys
import multiprocessing

UPSTREAM_PORT = 9000


def generate_nginx_conf(servers):
    env = Environment(
        loader=FileSystemLoader("nginx"),
        autoescape=select_autoescape()
    )

    template = env.get_template("nginx.conf.tpl")

    f = open("nginx/nginx.conf", "w")
    f.write(template.render(cpu_count=multiprocessing.cpu_count(), servers=servers))
    f.close()


def build_matchmaking_service_image():
    stream = os.popen("cd src/Matchmaking && docker build -t matchmaking_service .")
    output = stream.read();
    output


def build_matchmaking_engine_image():
    stream = os.popen("cd src/MatchmakingEngine && docker build -t matchmaking_engine .")
    output = stream.read();
    output


def build_nginx_image():
    stream = os.popen("cd nginx && docker build -t nginx .")
    output = stream.read();
    output


def create_docker_network():
    stream = os.popen("docker network create --driver bridge app-tier")
    output = stream.read();
    output


def create_service_container(index):
    stream = os.popen(
        f"docker run -d --name upstream{index} -t -i --network app-tier --rm matchmaking_service")
    output = stream.read();
    output


def create_engine_container(index):
    stream = os.popen(f"docker run -d --name engine{index} -t -i --network app-tier --rm matchmaking_engine")
    output = stream.read();
    output


def create_nginx_container():
    stream = os.popen(f"docker run -d --name nginx -t -i --network app-tier --rm -p 90:8000 nginx")
    output = stream.read();
    output


def create_redis_container():
    stream = os.popen(f"docker run -d --name redis -e ALLOW_EMPTY_PASSWORD=yes --network app-tier bitnami/redis:latest")
    output = stream.read();
    output


def main():
    args = sys.argv[1:]
    for arg in args:
        print(arg)

    engine_count = args[0];
    service_count = args[1];

    upstream_list = []
    for i in range(0, service_count):
        upstream_list.append(f"upstream{i}:90")

    generate_nginx_conf(upstream_list)
    create_docker_network()

    build_matchmaking_service_image()
    build_matchmaking_engine_image()

    create_redis_container()
    for i in range(0, service_count):
        create_service_container(i)

    for i in range(0, engine_count):
        create_engine_container(i)

    build_nginx_image()
    create_nginx_container()


if __name__ == "__main__":
    main()
