import time
import uuid
import random
from locust import HttpUser, task, between


class MatchmakingUser(HttpUser):
    wait_time = between(1, 5)
    guid = ""

    @task
    def matchmaking_join(self):
        self.client.post("http://localhost:90/v1/matchmaking/join",
                         json={"QoS": 10, "ProfileId": f"{MatchmakingUser.guid}"})

    @task
    def matchmaking_get_session(self):
        self.client.get(f"http://localhost:90/v1/matchmaking/{MatchmakingUser.guid}/session")

    @task
    def matchmaking_leave_session(self):
        self.client.post("http://localhost:90/v1/matchmaking/leave",
                         json={"ProfileId": f"{MatchmakingUser.guid}"})

    def on_start(self):
        MatchmakingUser.guid = str(uuid.uuid4())
