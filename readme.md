# Matchmaking service
## Prerequisites
- Docker (I used 4.1.1, but it should also work on 3.x.x)
- Python 3.x

## Setup
1. Run setup.py using python
    -
    This script will start 2x service containers, 1x engine container, 1x redis container and 1x nginx container
2. After completion of the setup, you can use the matchmaking.rest + rest client vscode plugin to call the endpoints, or any http client that you prefer.
    -
    - ENDPOINTS:

    POST http://localhost:90/v1/matchmaking/join HTTP/1.1
    Content-Type: application/json
    {"ProfileId": GUID, "QoS": INT}

    POST http://localhost:90/v1/matchmaking/leave HTTP/1.1
    Content-type: application/json
    {"ProfileId": GUID}
    
    GET http://localhost:90/v1/matchmaking/GUID/session HTTP/1.1

3. In order to run it from the solution, you will need to have a redis db available on "localhost:6379"

## Architecture
The Matchmaking system is composed out of the service and the engine.
The service pushes users into a matchmaking queue and the engine pops batches of players from the queue in order to match them into sessions.
The setup for this service has an nginx set up as a load balancer that proxies requests to 2 service instances.


 
